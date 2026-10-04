using TheDeep.Consequences;
using TheDeep.Player;
using TheDeep.State;
using TheDeep.World;
using UnityEngine;
using UnityEngine.AI;

namespace TheDeep.NPC
{
    // A prototype NPC that walks a route on the baked NavMesh and reacts to the world (D-027, D-028):
    //   Travel  — walks between its stops, waiting a moment at each
    //   Flee    — runs away from a nearby Disturbance (something broke), then returns to its route
    //   Pursue  — if its hostility condition is met (e.g. Concord is Hostile) and the player is near,
    //             it chases them; catching them ejects the player to another area (no damage)
    //   Stagger — a strong hit from a thrown/pushed physics object shoves it back briefly
    //   Fall    — if the structure it stands on breaks, it becomes a physics body and falls
    // Routes change by themselves when NavMesh links are switched on/off (StateGate), because the
    // agent simply walks whatever path the NavMesh currently allows. The decisions live in NpcBrain.
    //
    // Needs a NavMeshAgent (base offset = half its height) and a kinematic Rigidbody + collider,
    // so thrown objects register hits. NPC positions are never saved: on load they start at their
    // scene positions and the world state decides which routes exist.
    [DefaultExecutionOrder(10)] // after StateGate.Start, so the first path uses the correct routes
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    public class NpcAgent : MonoBehaviour
    {
        [Header("Route")]
        [Tooltip("Points to walk between, in order, looping. Leave empty to stand still.")]
        [SerializeField] Transform[] stops = new Transform[0];
        [Tooltip("Which stop to head for first.")]
        [SerializeField] int firstStop;
        [Tooltip("Seconds to wait on arriving at a stop.")]
        [SerializeField] float waitAtStop = 2f;

        [Header("Speeds (m/s)")]
        [SerializeField] float travelSpeed = 2f;
        [SerializeField] float fleeSpeed = 4.5f;
        [SerializeField] float pursueSpeed = 3.6f;

        [Header("Reactions")]
        [SerializeField] NpcSettings settings = new NpcSettings();
        [Tooltip("How far to run from a disturbance, in meters.")]
        [SerializeField] float fleeDistance = 6f;
        [Tooltip("Shove distance per unit of impact strength when staggered, in meters.")]
        [SerializeField] float shovePerStrength = 0.04f;
        [Tooltip("Longest possible shove, in meters.")]
        [SerializeField] float maxShove = 1.5f;
        [Tooltip("Hits slower than this (m/s) are ignored, e.g. walking into a crate. A thrown crate is faster.")]
        [SerializeField] float minImpactSpeed = 2f;

        [Header("Hostility")]
        [Tooltip("When this is met the NPC pursues the player. Leave empty for an NPC that is never hostile.")]
        [SerializeField] StateCondition hostileWhen = new StateCondition();
        [Tooltip("Scene the player is thrown out to when caught.")]
        [SerializeField] string ejectScene = "";
        [Tooltip("Spawn point id in that scene.")]
        [SerializeField] string ejectSpawnId = "";

        [Header("Debug")]
        [Tooltip("Show the NPC's current state (Travel, Flee, ...) as a small label above it.")]
        [SerializeField] bool showStateLabel;

        const float PursuitRepathInterval = 0.25f;

        NavMeshAgent agent;
        Rigidbody body;
        NpcBrain brain;
        Transform player;
        bool hostile;
        int currentStop;
        float waitUntil = -1f;
        float nextPursuitRepath;
        bool warnedOffNavMesh;
        TextMesh stateLabel;

        // Collected between frames from events and collisions, consumed by the next Update.
        bool pendingDisturbed;
        bool pendingLostFooting;
        float pendingImpact;
        Vector3 fleeFrom;
        Vector3 shoveVelocity;
        Vector3 impactFrom;

        // Exposed for the scene-validation tests.
        public Transform[] Stops => stops;
        public string EjectScene => ejectScene;
        public string EjectSpawnId => ejectSpawnId;
        public NpcState State => brain != null ? brain.State : NpcState.Idle;

        Vector3 Feet => transform.position - Vector3.up * agent.baseOffset;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            body = GetComponent<Rigidbody>();
            body.isKinematic = true; // moved by the agent; only becomes a physics body when it falls
            brain = new NpcBrain(settings);
            currentStop = stops.Length > 0 ? Mathf.Clamp(firstStop, 0, stops.Length - 1) : 0;
            if (showStateLabel)
                CreateStateLabel();
        }

        void OnEnable()
        {
            WorldSession.Changed += OnWorldChanged;
            Disturbances.Raised += OnDisturbance;
        }

        void OnDisable()
        {
            WorldSession.Changed -= OnWorldChanged;
            Disturbances.Raised -= OnDisturbance;
        }

        void Start()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
            RefreshHostility();
            EnterState(brain.State);
        }

        void OnWorldChanged()
        {
            RefreshHostility();
            // Routes may have opened or closed (NavMesh links toggled): ask for a fresh path.
            if (brain.State == NpcState.Travel && waitUntil < 0f)
                HeadForCurrentStop();
        }

        void RefreshHostility()
        {
            hostile = hostileWhen != null && !hostileWhen.IsEmpty && hostileWhen.IsMet(WorldSession.State);
        }

        void OnDisturbance(Disturbance disturbance)
        {
            if (brain.State == NpcState.Fall)
                return;
            if (disturbance.IsUnderfoot(Feet))
            {
                pendingLostFooting = true;
            }
            else if (disturbance.Reaches(transform.position))
            {
                pendingDisturbed = true;
                fleeFrom = disturbance.Position;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            // Only moving physics objects count, and carried objects never do (same rule as Destructible).
            // Other NPCs (kinematic) and slow bumps, like walking into a crate, don't count either.
            Rigidbody other = collision.rigidbody;
            if (other == null || other == body || other.isKinematic || other == PlayerGrabThrow.CurrentlyHeld || brain.State == NpcState.Fall)
                return;

            float speed = collision.relativeVelocity.magnitude;
            if (speed < minImpactSpeed)
                return;
            float strength = ImpactMath.Strength(other.mass, speed);
            if (strength > pendingImpact)
            {
                pendingImpact = strength;
                impactFrom = other.position;
            }
        }

        void Update()
        {
            if (brain.State == NpcState.Fall)
                return;

            var input = new NpcInputs
            {
                HasRoute = stops.Length > 0,
                Hostile = hostile,
                DistanceToPlayer = player != null ? Vector3.Distance(player.position, transform.position) : float.PositiveInfinity,
                Disturbed = pendingDisturbed,
                LostFooting = pendingLostFooting,
                ImpactStrength = pendingImpact,
            };
            float strength = pendingImpact;
            pendingDisturbed = pendingLostFooting = false;
            pendingImpact = 0f;

            NpcState before = brain.State;
            NpcState now = brain.Tick(input, Time.deltaTime);
            // A new hit or a new disturbance restarts the reaction (fresh shove / fresh escape point).
            bool restarted = (now == NpcState.Stagger && strength >= settings.staggerThreshold) ||
                             (now == NpcState.Flee && input.Disturbed);
            if (now != before || restarted)
                EnterState(now, strength);

            if (brain.CaughtPlayer && AreaExit.TravelTo(ejectScene, ejectSpawnId))
                Debug.Log($"{name} caught the player and throws them out of the area.");

            UpdateState(now);
            UpdateStateLabel(now);
        }

        void EnterState(NpcState state, float impactStrength = 0f)
        {
            if (state == NpcState.Fall)
            {
                BeginFall();
                return;
            }
            if (!agent.isOnNavMesh)
            {
                if (!warnedOffNavMesh)
                {
                    warnedOffNavMesh = true;
                    Debug.LogWarning($"{name} is not on a NavMesh, so it can't move. Has the scene's NavMesh been baked?", this);
                }
                return;
            }

            switch (state)
            {
                case NpcState.Idle:
                    agent.isStopped = true;
                    break;

                case NpcState.Travel:
                    agent.speed = travelSpeed;
                    agent.isStopped = false;
                    waitUntil = -1f;
                    HeadForCurrentStop();
                    break;

                case NpcState.Flee:
                    agent.speed = fleeSpeed;
                    agent.isStopped = false;
                    Vector3 away = NpcRules.AwayDirection(fleeFrom, transform.position, -transform.forward);
                    if (NavMesh.SamplePosition(transform.position + away * fleeDistance, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                        agent.SetDestination(hit.position);
                    else
                        agent.isStopped = true;
                    Debug.Log($"{name} flees from the disturbance.");
                    break;

                case NpcState.Pursue:
                    agent.speed = pursueSpeed;
                    agent.isStopped = false;
                    nextPursuitRepath = 0f;
                    Debug.Log($"{name} noticed the player and gives chase.");
                    break;

                case NpcState.Stagger:
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    Vector3 shoveDirection = NpcRules.AwayDirection(impactFrom, transform.position, -transform.forward);
                    float shove = NpcRules.ShoveDistance(impactStrength, shovePerStrength, maxShove);
                    // Spread the shove over the first half of the stagger, decaying to nothing.
                    float shoveTime = Mathf.Max(0.05f, settings.staggerDuration * 0.5f);
                    shoveVelocity = shoveDirection * (2f * shove / shoveTime);
                    Debug.Log($"{name} staggers from an impact of strength {impactStrength:0.0} (shoved {shove:0.0} m).");
                    break;
            }
        }

        void UpdateState(NpcState state)
        {
            if (!agent.isOnNavMesh)
                return;

            switch (state)
            {
                case NpcState.Travel:
                    if (stops.Length == 0 || agent.pathPending || agent.isOnOffMeshLink)
                        return;
                    if (waitUntil >= 0f)
                    {
                        if (Time.time >= waitUntil)
                        {
                            waitUntil = -1f;
                            currentStop = NpcRules.NextStop(currentStop, stops.Length);
                            HeadForCurrentStop();
                        }
                    }
                    else if (agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                    {
                        waitUntil = Time.time + waitAtStop;
                    }
                    break;

                case NpcState.Pursue:
                    if (player != null && Time.time >= nextPursuitRepath)
                    {
                        nextPursuitRepath = Time.time + PursuitRepathInterval;
                        agent.SetDestination(player.position);
                    }
                    break;

                case NpcState.Stagger:
                    if (!agent.isOnOffMeshLink && shoveVelocity.sqrMagnitude > 0.0001f)
                    {
                        agent.Move(shoveVelocity * Time.deltaTime); // Move keeps it on the NavMesh
                        float decay = Mathf.Max(0.05f, settings.staggerDuration * 0.5f);
                        shoveVelocity = Vector3.MoveTowards(shoveVelocity, Vector3.zero,
                            shoveVelocity.magnitude * Time.deltaTime / decay + 0.01f);
                    }
                    break;
            }
        }

        void HeadForCurrentStop()
        {
            if (stops.Length == 0 || stops[currentStop] == null || !agent.isOnNavMesh)
                return;
            agent.isStopped = false;
            agent.SetDestination(stops[currentStop].position);
        }

        // The structure under it broke: hand it over to physics and let it drop.
        void BeginFall()
        {
            agent.enabled = false;
            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.None;
            body.AddForce(Random.insideUnitSphere * 0.5f, ForceMode.VelocityChange);

            // Its bark goes quiet (disabling ConditionalText hides its text).
            ConditionalText bark = GetComponent<ConditionalText>();
            if (bark != null)
                bark.enabled = false;
            Debug.Log($"{name} lost their footing and falls!");
            UpdateStateLabel(NpcState.Fall);
        }

        void CreateStateLabel()
        {
            var labelObject = new GameObject("StateLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            stateLabel = labelObject.AddComponent<TextMesh>();
            stateLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stateLabel.fontSize = 48;
            stateLabel.characterSize = 0.02f;
            stateLabel.anchor = TextAnchor.LowerCenter;
            stateLabel.color = new Color(1f, 1f, 1f, 0.6f);
            MeshRenderer labelRenderer = labelObject.GetComponent<MeshRenderer>();
            if (labelRenderer == null)
                labelRenderer = labelObject.AddComponent<MeshRenderer>();
            labelRenderer.sharedMaterial = stateLabel.font.material;
        }

        void UpdateStateLabel(NpcState state)
        {
            if (stateLabel == null)
                return;
            stateLabel.text = state.ToString();
            Camera viewer = Camera.main;
            if (viewer != null)
                stateLabel.transform.rotation = Quaternion.LookRotation(stateLabel.transform.position - viewer.transform.position);
        }
    }
}
