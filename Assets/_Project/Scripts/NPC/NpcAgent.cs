using TheDeep.Combat;
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
    //   Pursue  — if it is hostile (e.g. Concord is Hostile) and the player is near, it chases them;
    //             catching them can eject the player to another area
    //   Stagger — a strong hit from a thrown/pushed physics object shoves it back briefly
    //   Fall    — if the structure it stands on breaks, it becomes a physics body and falls
    // Routes change by themselves when NavMesh links are switched on/off (StateGate), because the
    // agent simply walks whatever path the NavMesh currently allows. The decisions live in NpcBrain.
    //
    // P6 combat (only for NPCs with Max Health above 0, like the Scrapper; P5 NPCs are unaffected):
    //   Alert   — stops and faces the player for a moment after noticing them
    //   Attack  — winds up (leans back), strikes once, recovers; a stagger cancels the wind-up
    //   Dead    — out of health, or knocked/dropped more than Lethal Drop: it topples over
    //   Thrown/pushed objects and the player's melee damage it; a hit near a ledge can knock it off.
    //
    // Needs a NavMeshAgent (base offset = half its height) and a kinematic Rigidbody + collider,
    // so thrown objects register hits. NPC positions and health are never saved: on load they start
    // at their scene positions and the world state decides which routes exist.
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

        [Header("Combat (P6) - leave Max Health at 0 for a non-combat NPC")]
        [Tooltip("0 = can't be hurt or killed (P5 NPCs).")]
        [SerializeField] float maxHealth = 0f;
        [Tooltip("Hostile to the player no matter what the world state is (e.g. a Scrapper).")]
        [SerializeField] bool alwaysHostile;
        [Tooltip("Damage a landed strike deals to the player.")]
        [SerializeField] float attackDamage = 20f;
        [Tooltip("How far a landed strike pushes the player, in meters.")]
        [SerializeField] float playerKnockbackDistance = 2.5f;
        [Tooltip("Over how many seconds that push happens.")]
        [SerializeField] float playerKnockbackDuration = 0.25f;
        [Tooltip("Impacts below this strength (mass x speed) deal no damage (they may still stagger).")]
        [SerializeField] float impactDamageThreshold = 8f;
        [Tooltip("Damage per unit of impact strength above the threshold.")]
        [SerializeField] float impactDamageScale = 1.5f;
        [Tooltip("Most damage one impact can deal.")]
        [SerializeField] float maxImpactDamage = 45f;
        [Tooltip("Knocked or dropped further than this, in meters, it dies.")]
        [SerializeField] float lethalDrop = 1.5f;

        [Header("Debug")]
        [Tooltip("Show the NPC's current state (Travel, Flee, ...) as a small label above it.")]
        [SerializeField] bool showStateLabel;

        const float PursuitRepathInterval = 0.25f;
        const float StrikeReachBonus = 0.4f;   // a strike reaches a little past the range that starts it
        const float StrikeHalfAngle = 60f;     // the player must be within this many degrees in front
        const float MeleeShoveTime = 0.2f;
        const float KnockOffSpeed = 5f;
        const float WindupLean = -15f;         // leans back while winding up
        const float RecoveryLean = 10f;        // ... and forward after the strike

        NavMeshAgent agent;
        Rigidbody body;
        NpcBrain brain;
        Health health;
        Transform player;
        PlayerHealth playerHealth;
        bool hostile;
        int currentStop;
        float waitUntil = -1f;
        float nextPursuitRepath;
        bool warnedOffNavMesh;
        float defaultStoppingDistance;
        float fallStartY;
        Quaternion attackFacing;
        TextMesh label;

        // Collected between frames from events, collisions and melee, consumed by the next Update.
        bool pendingDisturbed;
        bool pendingLostFooting;
        bool pendingDead;
        float pendingImpact;
        float pendingMeleeStrength;
        Vector3 fleeFrom;
        Vector3 impactFrom;
        Vector3 meleeFrom;
        Vector3 shoveVelocity;
        float shoveTime = 0.5f;

        // Exposed for the scene-validation tests.
        public Transform[] Stops => stops;
        public string EjectScene => ejectScene;
        public string EjectSpawnId => ejectSpawnId;
        public bool IsCombatant => maxHealth > 0f;
        public float AttackRange => settings.attackRange;
        public NpcState State => brain != null ? brain.State : NpcState.Idle;

        Vector3 Feet => transform.position - Vector3.up * agent.baseOffset;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            body = GetComponent<Rigidbody>();
            body.isKinematic = true; // moved by the agent; only becomes a physics body when it falls
            brain = new NpcBrain(settings);
            health = maxHealth > 0f ? new Health(maxHealth) : null;
            defaultStoppingDistance = agent.stoppingDistance;
            currentStop = stops.Length > 0 ? Mathf.Clamp(firstStop, 0, stops.Length - 1) : 0;
            if (showStateLabel || health != null)
                CreateLabel();
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
            playerHealth = playerObject != null ? playerObject.GetComponent<PlayerHealth>() : null;
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
            hostile = alwaysHostile || (hostileWhen != null && !hostileWhen.IsEmpty && hostileWhen.IsMet(WorldSession.State));
        }

        void OnDisturbance(Disturbance disturbance)
        {
            if (brain.State == NpcState.Fall || brain.State == NpcState.Dead)
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
            if (other == null || other == body || other.isKinematic || other == PlayerGrabThrow.CurrentlyHeld)
                return;

            float speed = collision.relativeVelocity.magnitude;
            if (speed < minImpactSpeed)
                return;
            float strength = ImpactMath.Strength(other.mass, speed);
            float damage = health != null ? CombatRules.ImpactDamage(strength, impactDamageThreshold, impactDamageScale, maxImpactDamage) : 0f;
            ReceiveHit(strength, damage, other.position, melee: false);
        }

        // Called by PlayerMelee: a hit of the given strength (for stagger/shove) and damage.
        public void ReceiveMeleeHit(float strength, float damage, Vector3 attackerPosition) =>
            ReceiveHit(strength, damage, attackerPosition, melee: true);

        // Every hit goes through here: it may stagger (strength), hurt (damage) and shove (melee).
        void ReceiveHit(float strength, float damage, Vector3 from, bool melee)
        {
            if (brain.State == NpcState.Fall || brain.State == NpcState.Dead)
                return;

            if (strength > pendingImpact)
            {
                pendingImpact = strength;
                impactFrom = from;
            }
            if (melee && strength > pendingMeleeStrength)
            {
                pendingMeleeStrength = strength;
                meleeFrom = from;
            }

            if (health != null && damage > 0f)
            {
                float taken = health.TakeDamage(damage);
                if (taken > 0f)
                    Debug.Log($"{name} takes {taken:0} damage ({health.Current:0}/{health.Max:0} HP left).");
                if (health.IsDead)
                    pendingDead = true;
            }
        }

        void Update()
        {
            if (brain.State == NpcState.Dead)
            {
                UpdateLabel(NpcState.Dead);
                return;
            }
            if (brain.State == NpcState.Fall)
            {
                // A combat NPC that has dropped far enough is dead (P5 NPCs just stay where they land).
                if (health != null && CombatRules.IsLethalDrop(fallStartY, transform.position.y, lethalDrop))
                    Die("dropped to its death");
                UpdateLabel(brain.State);
                return;
            }

            bool playerAlive = playerHealth == null || !playerHealth.IsDead;
            var input = new NpcInputs
            {
                HasRoute = stops.Length > 0,
                Hostile = hostile && playerAlive,
                DistanceToPlayer = player != null ? Vector3.Distance(player.position, transform.position) : float.PositiveInfinity,
                Disturbed = pendingDisturbed,
                LostFooting = pendingLostFooting,
                ImpactStrength = pendingImpact,
                Dead = pendingDead,
            };
            float strength = pendingImpact;
            float meleeStrength = pendingMeleeStrength;
            pendingDisturbed = pendingLostFooting = false;
            pendingImpact = pendingMeleeStrength = 0f;

            NpcState before = brain.State;
            NpcState now = brain.Tick(input, Time.deltaTime);
            // A new hit or a new disturbance restarts the reaction (fresh shove / fresh escape point).
            bool restarted = (now == NpcState.Stagger && strength >= settings.staggerThreshold) ||
                             (now == NpcState.Flee && input.Disturbed);
            if (before == NpcState.Attack && now != NpcState.Attack)
                EndAttackPose();
            if (now != before || restarted)
                EnterState(now, strength);
            if (now == NpcState.Fall || now == NpcState.Dead || brain.State == NpcState.Fall)
                return;

            // A melee hit that didn't stagger still shoves a little (and can push it off a ledge).
            if (meleeStrength > 0f && now != NpcState.Stagger)
                StartShove(meleeFrom, NpcRules.ShoveDistance(meleeStrength, shovePerStrength, maxShove), MeleeShoveTime);
            if (brain.State == NpcState.Fall)
                return;

            if (brain.StrikeNow)
                Strike();

            if (brain.CaughtPlayer && AreaExit.TravelTo(ejectScene, ejectSpawnId))
                Debug.Log($"{name} caught the player and throws them out of the area.");

            UpdateShove();
            UpdateState(now);
            UpdateLabel(now);
        }

        void EnterState(NpcState state, float impactStrength = 0f)
        {
            if (state == NpcState.Fall)
            {
                BeginFall();
                return;
            }
            if (state == NpcState.Dead)
            {
                Debug.Log($"{name} is killed.");
                BecomeCorpse();
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
                    agent.stoppingDistance = defaultStoppingDistance;
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

                case NpcState.Alert:
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    Debug.Log($"{name} spots the player!");
                    break;

                case NpcState.Pursue:
                    agent.speed = pursueSpeed;
                    // An attacker stops at striking distance instead of walking into the player.
                    agent.stoppingDistance = settings.attackRange > 0f ? settings.attackRange * 0.8f : defaultStoppingDistance;
                    agent.isStopped = false;
                    nextPursuitRepath = 0f;
                    Debug.Log($"{name} noticed the player and gives chase.");
                    break;

                case NpcState.Attack:
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    agent.updateRotation = false;
                    attackFacing = FacingPlayer();
                    Debug.Log($"{name} winds up an attack.");
                    break;

                case NpcState.Stagger:
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                    float shove = NpcRules.ShoveDistance(impactStrength, shovePerStrength, maxShove);
                    Debug.Log($"{name} staggers from an impact of strength {impactStrength:0.0} (shoved {shove:0.0} m).");
                    // Spread the shove over the first half of the stagger, decaying to nothing.
                    StartShove(impactFrom, shove, settings.staggerDuration * 0.5f);
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

                case NpcState.Alert:
                    transform.rotation = FacingPlayer();
                    break;

                case NpcState.Pursue:
                    if (player != null && Time.time >= nextPursuitRepath)
                    {
                        nextPursuitRepath = Time.time + PursuitRepathInterval;
                        agent.SetDestination(player.position);
                    }
                    break;

                case NpcState.Attack:
                    // The tell: track the player and lean back during the wind-up, then lunge
                    // forward (committed to that direction) after the strike.
                    if (brain.WindingUp)
                        attackFacing = FacingPlayer();
                    transform.rotation = attackFacing * Quaternion.Euler(brain.WindingUp ? WindupLean : RecoveryLean, 0f, 0f);
                    break;
            }
        }

        // The wind-up is over: the blow lands if the player is still in reach and in front.
        void Strike()
        {
            bool lands = player != null && playerHealth != null && !playerHealth.IsDead &&
                         CombatRules.StrikeConnects(transform.position, attackFacing * Vector3.forward, player.position,
                             settings.attackRange + StrikeReachBonus, StrikeHalfAngle);
            if (lands)
            {
                Debug.Log($"{name} strikes the player!");
                playerHealth.TakeHit(attackDamage, transform.position, playerKnockbackDistance, playerKnockbackDuration);
            }
            else
            {
                Debug.Log($"{name} strikes and misses.");
            }
        }

        void EndAttackPose()
        {
            if (agent.enabled)
                agent.updateRotation = true;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        Quaternion FacingPlayer()
        {
            if (player == null)
                return transform.rotation;
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            return toPlayer.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toPlayer) : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        // Pushes the agent away from 'from' by 'distance' over 'time' seconds, along the NavMesh.
        // A combat NPC pushed over a real drop is knocked off instead.
        void StartShove(Vector3 from, float distance, float time)
        {
            if (distance <= 0f)
                return;
            Vector3 direction = NpcRules.AwayDirection(from, transform.position, -transform.forward);
            if (health != null && KnocksOffLedge(direction, distance))
            {
                KnockOff(direction);
                return;
            }
            shoveTime = Mathf.Max(0.05f, time);
            shoveVelocity = direction * (2f * distance / shoveTime);
        }

        void UpdateShove()
        {
            if (shoveVelocity.sqrMagnitude <= 0.0001f || !agent.enabled || !agent.isOnNavMesh || agent.isOnOffMeshLink)
                return;
            agent.Move(shoveVelocity * Time.deltaTime); // Move keeps it on the NavMesh
            shoveVelocity = Vector3.MoveTowards(shoveVelocity, Vector3.zero,
                shoveVelocity.magnitude * Time.deltaTime / shoveTime + 0.01f);
        }

        // True if the shove would carry it over a real drop: nothing solid in the way, and no ground
        // within Lethal Drop below the spot just past where the shove ends.
        bool KnocksOffLedge(Vector3 direction, float distance)
        {
            float reach = distance + 0.6f;
            if (Physics.Raycast(transform.position, direction, reach, ~0, QueryTriggerInteraction.Ignore))
                return false; // a wall or something solid stops it
            Vector3 probe = Feet + direction * reach + Vector3.up;
            return !Physics.Raycast(probe, Vector3.down, 1f + lethalDrop, ~0, QueryTriggerInteraction.Ignore);
        }

        void KnockOff(Vector3 direction)
        {
            Debug.Log($"{name} is knocked off the ledge!");
            brain.Tick(new NpcInputs { LostFooting = true }, 0f);
            BeginFall();
            body.linearVelocity = direction * KnockOffSpeed + Vector3.up * 1.5f;
        }

        void HeadForCurrentStop()
        {
            if (stops.Length == 0 || stops[currentStop] == null || !agent.isOnNavMesh)
                return;
            agent.isStopped = false;
            agent.SetDestination(stops[currentStop].position);
        }

        // The structure under it broke (or it was knocked off a ledge): hand it over to physics.
        void BeginFall()
        {
            fallStartY = transform.position.y;
            shoveVelocity = Vector3.zero;
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
            UpdateLabel(NpcState.Fall);
        }

        // Killed outside the normal Update flow (e.g. a lethal drop while falling).
        void Die(string cause)
        {
            if (health != null)
                health.Kill();
            brain.Tick(new NpcInputs { Dead = true }, 0f);
            Debug.Log($"{name} {cause}.");
            BecomeCorpse();
        }

        // Out of the fight; it lies there until the area reloads. One that dies on its feet is laid
        // down without physics (so a body can't crash into and break structures); one that was
        // already falling just keeps falling.
        void BecomeCorpse()
        {
            shoveVelocity = Vector3.zero;
            bool wasStanding = body.isKinematic;
            if (agent.enabled)
                agent.enabled = false;
            if (wasStanding)
            {
                float halfHeight = agent.baseOffset;
                transform.SetPositionAndRotation(transform.position - Vector3.up * (halfHeight - 0.5f),
                    Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Quaternion.Euler(-90f, 0f, 0f));
            }

            ConditionalText bark = GetComponent<ConditionalText>();
            if (bark != null)
                bark.enabled = false;
            if (label != null)
                label.transform.position = transform.position + Vector3.up * 0.8f;
            UpdateLabel(NpcState.Dead);
        }

        void CreateLabel()
        {
            var labelObject = new GameObject("StateLabel");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            label = labelObject.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 48;
            label.characterSize = health != null ? 0.03f : 0.02f;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.color = health != null ? new Color(1f, 0.55f, 0.55f, 1f) : new Color(1f, 1f, 1f, 0.6f);
            MeshRenderer labelRenderer = labelObject.GetComponent<MeshRenderer>();
            if (labelRenderer == null)
                labelRenderer = labelObject.AddComponent<MeshRenderer>();
            labelRenderer.sharedMaterial = label.font.material;
        }

        // Health (for combat NPCs), "!" when alert, and the state when the debug label is on.
        void UpdateLabel(NpcState state)
        {
            if (label == null)
                return;
            string text = "";
            if (health != null)
                text = state == NpcState.Dead ? "DEAD" : $"HP {Mathf.CeilToInt(health.Current)}/{health.Max:0}";
            if (state == NpcState.Alert)
                text += " !";
            if (showStateLabel)
                text += (text.Length > 0 ? "\n" : "") + state;
            label.text = text;

            Camera viewer = Camera.main;
            if (viewer != null)
                label.transform.rotation = Quaternion.LookRotation(label.transform.position - viewer.transform.position);
        }
    }
}
