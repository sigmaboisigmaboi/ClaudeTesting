using TheDeep.State;
using UnityEngine;

namespace TheDeep.World
{
    // P3.1 persistence spike: pushing the assigned crate onto this pad records a fact.
    // On the next start, if the fact is remembered, the crate is placed on the pad
    // (an authored "after" state, not a saved physics position).
    // Needs a trigger collider on this GameObject.
    public class CrateTargetPad : MonoBehaviour
    {
        const string Fact = "bootstrap.crate_on_pad";

        [Tooltip("The only crate that counts for this pad.")]
        [SerializeField] Rigidbody crate;

        [Tooltip("Where the crate is placed on load, relative to this pad.")]
        [SerializeField] Vector3 crateRestOffset = new Vector3(0f, 0.5f, 0f);

        WorldState worldState;

        void Awake()
        {
            // Shared session state (D-020): never load a private copy, or saves overwrite each other.
            worldState = WorldSession.State;
            Debug.Log($"Loaded world state: {worldState.FactCount} fact(s) from {WorldState.SaveFilePath}");

            if (worldState.Has(Fact))
                PlaceCrateOnPad();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody != crate || worldState.Has(Fact))
                return;

            worldState.Record(Fact);
            Debug.Log($"World state: recorded '{Fact}' and saved to {WorldState.SaveFilePath}");
        }

        void PlaceCrateOnPad()
        {
            Vector3 restPosition = transform.position + crateRestOffset;
            crate.transform.SetPositionAndRotation(restPosition, Quaternion.identity);
            crate.position = restPosition;
            crate.rotation = Quaternion.identity;
        }

        // Testing helper: right-click this component in the Inspector to reset the world.
        [ContextMenu("Delete Saved World State")]
        void DeleteSavedWorldState()
        {
            WorldSession.DeleteSave();
        }
    }
}
