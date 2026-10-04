using System.Collections.Generic;
using TheDeep.State;

namespace TheDeep.Consequences
{
    // Applies consequence rules to a world state. Plain C#, so it can be unit-tested.
    public static class ConsequenceEngine
    {
        // Fires every rule whose condition is met and that hasn't fired before, applying its effects.
        // Repeats until nothing new fires, so one rule's effects can enable another.
        // Returns the ids of the rules that fired this call (empty if none).
        public static List<string> Apply(WorldState state, IReadOnlyList<ConsequenceRule> rules)
        {
            var fired = new List<string>();
            if (state == null || rules == null)
                return fired;

            bool firedThisPass = true;
            while (firedThisPass)
            {
                firedThisPass = false;
                foreach (ConsequenceRule rule in rules)
                {
                    if (rule == null || string.IsNullOrEmpty(rule.id) || rule.condition == null)
                        continue;
                    if (state.Has(rule.AppliedFact) || !rule.condition.IsMet(state))
                        continue;

                    if (rule.factsToSet != null)
                        foreach (string fact in rule.factsToSet)
                            if (!string.IsNullOrEmpty(fact))
                                state.Set(fact);

                    if (rule.reputationChanges != null)
                        foreach (ConsequenceRule.ReputationChange change in rule.reputationChanges)
                            if (change != null)
                                state.AddReputation(change.faction, change.amount);

                    state.Set(rule.AppliedFact);
                    fired.Add(rule.id);
                    firedThisPass = true;
                }
            }
            return fired;
        }
    }
}
