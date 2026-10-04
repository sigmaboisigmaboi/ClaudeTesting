using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TheDeep.State
{
    // Everything the world remembers, and the JSON save file that stores it:
    // - facts: named one-way events (e.g. "bootstrap.crate_on_pad", "p2span.bridge_destroyed")
    // - destroyed ids: PersistentIds of objects that have been destroyed
    // Once recorded, both stay recorded.
    //
    // During play, use the single shared copy in WorldSession rather than loading your own.
    [Serializable]
    public class WorldState
    {
        // Version 2 added destroyedIds. Version 1 files (facts only) still load: the
        // missing list simply starts empty, and the file is saved as version 2 next time.
        public const int CurrentVersion = 2;
        const string SaveFileName = "world_state.json";

        // Serialized by JsonUtility, so these are fields rather than properties.
        [SerializeField] int version = CurrentVersion;
        [SerializeField] List<string> facts = new List<string>();
        [SerializeField] List<string> destroyedIds = new List<string>();

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public int Version => version;

        public int FactCount => facts.Count;

        public int DestroyedCount => destroyedIds.Count;

        public IReadOnlyList<string> Facts => facts;

        public IReadOnlyList<string> DestroyedIds => destroyedIds;

        public bool Has(string fact) => facts.Contains(fact);

        public void Set(string fact)
        {
            if (!Has(fact))
                facts.Add(fact);
        }

        // Records a fact and saves straight away.
        public void Record(string fact)
        {
            Set(fact);
            SaveToDisk();
        }

        public bool IsDestroyed(string persistentId) => destroyedIds.Contains(persistentId);

        public void MarkDestroyed(string persistentId)
        {
            if (!IsDestroyed(persistentId))
                destroyedIds.Add(persistentId);
        }

        public string ToJson() => JsonUtility.ToJson(this, prettyPrint: true);

        // Missing, empty, or unreadable JSON gives an empty world rather than an error.
        public static WorldState FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new WorldState();

            try
            {
                WorldState state = JsonUtility.FromJson<WorldState>(json);
                if (state == null)
                    return new WorldState();
                if (state.facts == null)
                    state.facts = new List<string>();
                if (state.destroyedIds == null)
                    state.destroyedIds = new List<string>();
                state.version = CurrentVersion; // older files are upgraded in memory
                return state;
            }
            catch (ArgumentException)
            {
                return new WorldState();
            }
        }

        public static WorldState LoadFromDisk()
        {
            if (!File.Exists(SaveFilePath))
                return new WorldState();
            return FromJson(File.ReadAllText(SaveFilePath));
        }

        public void SaveToDisk() => File.WriteAllText(SaveFilePath, ToJson());

        public static void DeleteSaveFile()
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
        }
    }
}
