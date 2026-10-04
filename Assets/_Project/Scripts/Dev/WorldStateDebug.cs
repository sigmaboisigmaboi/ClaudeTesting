using System.Linq;
using TheDeep.Factions;
using TheDeep.State;
using UnityEngine;

namespace TheDeep.Dev
{
    // Prototype helper: shows what the world remembers, resets it, and offers test shortcuts.
    // Right-click this component (⋮ menu) for:
    //   "Delete Saved World State" — then stop and press Play to see the intact world
    //   "Dev: Record Bridge Destroyed" / "Dev: Record Cache Looted" — test consequences without
    //   breaking things by hand (the objects themselves show as rubble on the next scene load)
    public class WorldStateDebug : MonoBehaviour
    {
        void OnEnable() => WorldSession.Changed += LogState;
        void OnDisable() => WorldSession.Changed -= LogState;
        void Start() => LogState();

        void LogState()
        {
            WorldState state = WorldSession.State;
            string reputations = string.Join(", ", new[] { Faction.Concord, Faction.Delvers, Faction.Hollowers }
                .Select(f => $"{f} {state.GetReputation(f)} ({ReputationRules.BandFor(state.GetReputation(f))})"));
            Debug.Log($"[WorldStateDebug] facts: [{string.Join(", ", state.Facts)}] | destroyed: [{string.Join(", ", state.DestroyedIds)}] | reputation: {reputations}");
        }

        [ContextMenu("Delete Saved World State")]
        void DeleteSavedWorldState()
        {
            WorldSession.DeleteSave();
            Debug.Log("[WorldStateDebug] Stop and press Play again to see the intact world.");
        }

        [ContextMenu("Dev: Record Bridge Destroyed")]
        void RecordBridgeDestroyed()
        {
            WorldState state = WorldSession.State;
            state.MarkDestroyed("p2span.post_left");
            state.MarkDestroyed("p2span.post_right");
            state.MarkDestroyed("p2span.bridge_deck");
            state.Set("p2span.bridge_destroyed");
            WorldSession.Commit();
        }

        [ContextMenu("Dev: Record Cache Looted")]
        void RecordCacheLooted()
        {
            WorldSession.State.MarkDestroyed("p2span.wall_alcove");
            WorldSession.Commit();
        }
    }
}
