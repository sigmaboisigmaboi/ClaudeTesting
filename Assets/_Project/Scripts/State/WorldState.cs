using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TheDeep.State
{
    // The facts the world remembers (e.g. "bootstrap.crate_on_pad"), and the
    // JSON save file that stores them. Facts are one-way events: once recorded,
    // they stay recorded.
    //
    // P3.1 persistence spike: deliberately tiny. A proper save flow (PersistentId,
    // saving at area transitions) replaces the immediate save in P3 proper.
    [Serializable]
    public class WorldState
    {
        const int CurrentVersion = 1;
        const string SaveFileName = "world_state.json";

        // Serialized by JsonUtility, so these are fields rather than properties.
        [SerializeField] int version = CurrentVersion;
        [SerializeField] List<string> facts = new List<string>();

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public int Version => version;

        public int FactCount => facts.Count;

        public bool Has(string fact) => facts.Contains(fact);

        public void Set(string fact)
        {
            if (!Has(fact))
                facts.Add(fact);
        }

        // Records a fact and saves straight away (immediate save is a spike-only shortcut).
        public void Record(string fact)
        {
            Set(fact);
            SaveToDisk();
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
