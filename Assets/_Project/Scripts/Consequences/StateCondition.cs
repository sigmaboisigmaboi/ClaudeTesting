using System;
using System.Collections.Generic;
using TheDeep.Factions;
using TheDeep.State;

namespace TheDeep.Consequences
{
    // A reusable check against the world state, authored in the Inspector or in a rulebook.
    // Every part must hold; an empty condition is always met.
    [Serializable]
    public class StateCondition
    {
        public List<string> requiredFacts = new List<string>();
        public List<string> forbiddenFacts = new List<string>();
        public List<string> requiredDestroyedIds = new List<string>();
        public List<ReputationRequirement> reputation = new List<ReputationRequirement>();

        // The faction's current reputation band must be between minBand and maxBand (inclusive).
        [Serializable]
        public class ReputationRequirement
        {
            public Faction faction;
            public ReputationBand minBand = ReputationBand.Hostile;
            public ReputationBand maxBand = ReputationBand.Friendly;
        }

        public bool IsEmpty =>
            Count(requiredFacts) + Count(forbiddenFacts) + Count(requiredDestroyedIds) + Count(reputation) == 0;

        public bool IsMet(WorldState state)
        {
            if (requiredFacts != null)
                foreach (string fact in requiredFacts)
                    if (!string.IsNullOrEmpty(fact) && !state.Has(fact)) return false;

            if (forbiddenFacts != null)
                foreach (string fact in forbiddenFacts)
                    if (!string.IsNullOrEmpty(fact) && state.Has(fact)) return false;

            if (requiredDestroyedIds != null)
                foreach (string id in requiredDestroyedIds)
                    if (!string.IsNullOrEmpty(id) && !state.IsDestroyed(id)) return false;

            if (reputation != null)
                foreach (ReputationRequirement requirement in reputation)
                {
                    if (requirement == null) continue;
                    ReputationBand band = ReputationRules.BandFor(state.GetReputation(requirement.faction));
                    if (band < requirement.minBand || band > requirement.maxBand) return false;
                }

            return true;
        }

        static int Count<T>(List<T> list) => list != null ? list.Count : 0;
    }
}
