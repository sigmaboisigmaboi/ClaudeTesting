using UnityEngine;

namespace TheDeep.State
{
    // The one shared WorldState for the current Play session.
    // Every component reads and records through WorldSession.State, so two objects can never
    // overwrite each other's saves (each loading its own copy would lose updates).
    // Loaded from disk the first time it's used; survives scene loads (area transitions).
    public static class WorldSession
    {
        static WorldState state;

        public static WorldState State
        {
            get
            {
                if (state == null)
                {
                    state = WorldState.LoadFromDisk();
                    Debug.Log($"World state loaded: {state.FactCount} fact(s), {state.DestroyedCount} destroyed object(s) from {WorldState.SaveFilePath}");
                }
                return state;
            }
        }

        public static void Save() => State.SaveToDisk();

        // Deletes the save file and forgets everything in memory. Objects already in the
        // scene keep their current look until the scene is loaded again (stop and press Play).
        public static void DeleteSave()
        {
            WorldState.DeleteSaveFile();
            state = new WorldState();
            Debug.Log($"Deleted saved world state at {WorldState.SaveFilePath}");
        }

        // Unity calls this at the start of every Play session, so nothing carries over from the
        // previous session even if the Editor is set to skip reloading scripts on Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlaySession()
        {
            state = null;
        }
    }
}
