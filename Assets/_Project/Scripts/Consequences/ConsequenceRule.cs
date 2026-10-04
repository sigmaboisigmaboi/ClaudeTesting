using System;
using System.Collections.Generic;
using TheDeep.Factions;

namespace TheDeep.Consequences
{
    // One authored consequence: when the condition is met, apply the effects — once.
    // Whether it has fired is remembered as the fact "rule.<id>", so it never applies twice,
    // even across saves and reloads.
    [Serializable]
    public class ConsequenceRule
    {
        public string id = "";
        public string description = "";
        public StateCondition condition = new StateCondition();
        public List<string> factsToSet = new List<string>();
        public List<ReputationChange> reputationChanges = new List<ReputationChange>();

        [Serializable]
        public class ReputationChange
        {
            public Faction faction;
            public int amount;
        }

        public string AppliedFact => "rule." + id;

        public bool HasEffects =>
            (factsToSet != null && factsToSet.Count > 0) || (reputationChanges != null && reputationChanges.Count > 0);
    }
}
