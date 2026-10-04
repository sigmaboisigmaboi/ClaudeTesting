using System.Collections;
using TheDeep.Player;
using TheDeep.State;
using TheDeep.World;
using UnityEngine;

namespace TheDeep.Destruction
{
    // A breakable piece of infrastructure with three authored looks (D-021):
    //   Intact    — the normal object (this GameObject's collider + the Intact visual child)
    //   Fractured — pre-placed piece Rigidbodies, hidden until it breaks
    //   Rubble    — a static after-state shown when the scene loads already destroyed
    // It takes damage from impacts by pushed/thrown physics objects, or collapses when all of
    // its supports are destroyed. Its destruction is recorded in the shared WorldSession and saved.
    //
    // Needs a collider on this GameObject (the intact shape) so collision messages arrive here.
    [RequireComponent(typeof(PersistentId))]
    public class Destructible : MonoBehaviour
    {
        // Max debris pieces simulating at once across all destructibles (D-022).
        const int MaxActiveDebrisPieces = 40;

        [Header("Authored looks")]
        [SerializeField] GameObject intact;
        [SerializeField] GameObject fractured;
        [SerializeField] GameObject rubble;

        [Header("Integrity")]
        [Tooltip("How much damage it takes to break. Not a health system — structures only.")]
        [SerializeField] float maxIntegrity = 25f;
        [Tooltip("Impacts at or below this strength do nothing (filters out bumps and settling).")]
        [SerializeField] float damageThreshold = 5f;

        [Header("Structure")]
        [Tooltip("If set, this collapses when ALL of these are destroyed.")]
        [SerializeField] Destructible[] supports = new Destructible[0];
        [Tooltip("Optional world fact recorded when this is destroyed, e.g. p2span.bridge_destroyed")]
        [SerializeField] string factOnDestroyed = "";

        [Header("Debris")]
        [Tooltip("Outward speed given to pieces when it breaks from an impact, in m/s.")]
        [SerializeField] float breakBurstSpeed = 2f;
        [Tooltip("Pieces freeze in place after this many seconds (sooner if they come to rest).")]
        [SerializeField] float settleTime = 4f;

        static DebrisBudget<Destructible> debrisBudget = new DebrisBudget<Destructible>(MaxActiveDebrisPieces);

        float integrity;
        bool isBroken;
        Collider intactCollider;
        Rigidbody[] pieces;
        bool piecesFrozen;

        public bool IsBroken => isBroken;
        public string PersistentIdValue => GetComponent<PersistentId>().Id;

        // Exposed for the scene-validation tests.
        public GameObject IntactVisual => intact;
        public GameObject FracturedPieces => fractured;
        public GameObject RubbleVisual => rubble;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlaySession()
        {
            debrisBudget = new DebrisBudget<Destructible>(MaxActiveDebrisPieces);
        }

        void Awake()
        {
            integrity = maxIntegrity;
            intactCollider = GetComponent<Collider>();
            pieces = fractured != null ? fractured.GetComponentsInChildren<Rigidbody>(includeInactive: true) : new Rigidbody[0];

            if (WorldSession.State.IsDestroyed(PersistentIdValue))
            {
                ShowDestroyedState();
                return;
            }

            ShowIntactState();
        }

        void Start()
        {
            // Covers a save where every support was destroyed but this one wasn't recorded yet.
            if (!isBroken && SupportsAllDestroyed())
                Break(null);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (isBroken)
                return;

            // Only moving physics objects count — not the floor, walls, or the player's CharacterController.
            Rigidbody other = collision.rigidbody;
            if (other == null)
                return;

            // Carried objects don't deal destruction damage; only pushed or thrown ones do (P2 decision C).
            if (other == PlayerGrabThrow.CurrentlyHeld)
                return;

            float strength = ImpactMath.Strength(other.mass, collision.relativeVelocity.magnitude);
            float damage = DestructionRules.DamageFromImpact(strength, damageThreshold);
            if (damage <= 0f)
                return;

            integrity = DestructionRules.ApplyDamage(integrity, damage);
            Debug.Log($"{name} hit by {other.name}: strength {strength:0.0}, damage {damage:0.0}, integrity {integrity:0.0}/{maxIntegrity:0.0}");

            if (DestructionRules.IsBroken(integrity))
                Break(collision.contactCount > 0 ? collision.GetContact(0).point : (Vector3?)null);
        }

        // Breaks this object: swap to physics pieces, record it, and tell anything it supports.
        // impactPoint is null for a collapse (no hit), which just lets the pieces fall.
        public void Break(Vector3? impactPoint)
        {
            if (isBroken)
                return;
            isBroken = true;

            SetLook(intactOn: false, fracturedOn: true, rubbleOn: false);
            LaunchPieces(impactPoint);

            if (string.IsNullOrEmpty(PersistentIdValue))
            {
                Debug.LogError($"{name} has no PersistentId value, so its destruction can't be saved.", this);
            }
            else
            {
                RecordDestruction();
            }

            if (pieces.Length > 0)
            {
                foreach (Destructible older in debrisBudget.Add(this, pieces.Length))
                    older.FreezePieces();
                StartCoroutine(SettleThenFreeze());
            }

            NotifyDependents();
        }

        void RecordDestruction()
        {
            WorldState state = WorldSession.State;
            state.MarkDestroyed(PersistentIdValue);
            if (!string.IsNullOrEmpty(factOnDestroyed))
                state.Set(factOnDestroyed);
            WorldSession.Save();
            Debug.Log($"{name} destroyed — recorded '{PersistentIdValue}'" +
                      (string.IsNullOrEmpty(factOnDestroyed) ? "" : $" and fact '{factOnDestroyed}'") + " and saved.");
        }

        void ShowIntactState()
        {
            isBroken = false;
            SetLook(intactOn: true, fracturedOn: false, rubbleOn: false);
        }

        void ShowDestroyedState()
        {
            isBroken = true;
            piecesFrozen = true;
            SetLook(intactOn: false, fracturedOn: false, rubbleOn: true);
        }

        void SetLook(bool intactOn, bool fracturedOn, bool rubbleOn)
        {
            if (intactCollider != null)
                intactCollider.enabled = intactOn;
            if (intact != null)
                intact.SetActive(intactOn);
            if (fractured != null)
                fractured.SetActive(fracturedOn);
            if (rubble != null)
                rubble.SetActive(rubbleOn);
        }

        void LaunchPieces(Vector3? impactPoint)
        {
            foreach (Rigidbody piece in pieces)
            {
                piece.isKinematic = false;
                if (impactPoint == null)
                    continue; // collapse: gravity does the work

                Vector3 outward = piece.position - impactPoint.Value;
                outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Vector3.up;
                piece.linearVelocity = outward * breakBurstSpeed;
            }
        }

        IEnumerator SettleThenFreeze()
        {
            float elapsed = 0f;
            const float checkInterval = 0.25f;
            const float minimumSimTime = 1f; // let the pieces actually fall before checking for rest

            while (elapsed < settleTime && !piecesFrozen)
            {
                yield return new WaitForSeconds(checkInterval);
                elapsed += checkInterval;
                if (elapsed >= minimumSimTime && AllPiecesAsleep())
                    break;
            }

            FreezePieces();
        }

        bool AllPiecesAsleep()
        {
            foreach (Rigidbody piece in pieces)
            {
                if (!piece.IsSleeping())
                    return false;
            }
            return true;
        }

        // Pieces stay exactly where they landed but stop simulating (kinematic, colliders kept).
        void FreezePieces()
        {
            if (piecesFrozen)
                return;
            piecesFrozen = true;
            debrisBudget.Remove(this);

            foreach (Rigidbody piece in pieces)
            {
                if (piece == null)
                    continue;
                piece.linearVelocity = Vector3.zero;
                piece.angularVelocity = Vector3.zero;
                piece.isKinematic = true;
            }
        }

        bool SupportsAllDestroyed()
        {
            int standing = 0;
            foreach (Destructible support in supports)
            {
                if (support != null && !support.IsBroken)
                    standing++;
            }
            return DestructionRules.ShouldCollapse(supports.Length, standing);
        }

        void NotifyDependents()
        {
            foreach (Destructible other in FindObjectsByType<Destructible>(FindObjectsSortMode.None))
            {
                if (other != this && !other.IsBroken && other.DependsOn(this) && other.SupportsAllDestroyed())
                {
                    Debug.Log($"{other.name} lost all its supports and collapses.");
                    other.Break(null);
                }
            }
        }

        bool DependsOn(Destructible support) => System.Array.IndexOf(supports, support) >= 0;

        // Testing helper: right-click this component in the Inspector to reset the world.
        [ContextMenu("Delete Saved World State")]
        void DeleteSavedWorldState() => WorldSession.DeleteSave();
    }
}
