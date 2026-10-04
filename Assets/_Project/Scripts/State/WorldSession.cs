using System;
using System.Collections.Generic;
using TheDeep.Consequences;
using UnityEngine;

namespace TheDeep.State
{
    // The one shared WorldState for the current Play session.
    // Every component reads and records through WorldSession.State, so two objects can never
    // overwrite each other's saves (each loading its own copy would lose updates).
    // Loaded from disk the first time it's used; survives scene loads (area transitions).
    //
    // Consequences (D-024): after the world changes, call Commit() — it applies any consequence
    // rules, saves once, and raises Changed so the scene's reactors (StateGate, ConditionalText)
    // update. Each scene's ConsequenceRunner also runs the rules on load (the "world tick").
    public static class WorldSession
    {
        static WorldState state;
        static IReadOnlyList<ConsequenceRule> rules;

        // Raised after the world state changes (Commit, world tick that fired rules, or reset).
        public static event Action Changed;

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

        // Registers the rules to apply on Commit and on the world tick (set by ConsequenceRunner).
        public static void UseRules(ConsequenceRulebook rulebook)
        {
            rules = rulebook != null ? rulebook.Rules : null;
        }

        // Call after changing the world state: apply consequences, save, notify reactors.
        public static void Commit()
        {
            ApplyRules();
            Save();
            Changed?.Invoke();
        }

        // Runs the rules when an area loads. Saves and notifies only if something new fired.
        public static void RunWorldTick()
        {
            if (ApplyRules())
            {
                Save();
                Changed?.Invoke();
            }
        }

        // Deletes the save file and forgets everything in memory. Reactors update immediately;
        // destructibles keep their current look until the scene is loaded again (stop and press Play).
        public static void DeleteSave()
        {
            WorldState.DeleteSaveFile();
            state = new WorldState();
            Debug.Log($"Deleted saved world state at {WorldState.SaveFilePath}");
            Changed?.Invoke();
        }

        static bool ApplyRules()
        {
            List<string> fired = ConsequenceEngine.Apply(State, rules);
            foreach (string id in fired)
                Debug.Log($"Consequence fired: {id}");
            return fired.Count > 0;
        }

        // Unity calls this at the start of every Play session, so nothing carries over from the
        // previous session even if the Editor is set to skip reloading scripts on Play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlaySession()
        {
            state = null;
            rules = null;
            Changed = null;
        }
    }
}
