using TheDeep.State;
using UnityEngine;

namespace TheDeep.Dev
{
    // Prototype helper: shows what the world remembers, and resets it.
    // Right-click this component (⋮ menu) > "Delete Saved World State", then stop and press Play.
    public class WorldStateDebug : MonoBehaviour
    {
        void Start()
        {
            WorldState state = WorldSession.State;
            Debug.Log($"[WorldStateDebug] facts: [{string.Join(", ", state.Facts)}] | destroyed: [{string.Join(", ", state.DestroyedIds)}]");
        }

        [ContextMenu("Delete Saved World State")]
        void DeleteSavedWorldState()
        {
            WorldSession.DeleteSave();
            Debug.Log("[WorldStateDebug] Stop and press Play again to see the intact world.");
        }
    }
}
