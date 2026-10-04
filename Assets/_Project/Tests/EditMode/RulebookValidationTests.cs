using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TheDeep.Consequences;
using TheDeep.Factions;
using TheDeep.State;
using UnityEditor;

namespace TheDeep.Tests.EditMode
{
    // Guards hand-authored consequence data: every rulebook asset in the project must have
    // rules with unique ids, a real condition, and at least one effect. Also runs the real
    // P4 rulebook through the bridge chain, including a save made before the bypass rule existed.
    public class RulebookValidationTests
    {
        static List<ConsequenceRulebook> AllRulebooks() =>
            AssetDatabase.FindAssets("t:ConsequenceRulebook")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ConsequenceRulebook>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(book => book != null)
                .ToList();

        [Test]
        public void AtLeastOneRulebookExists()
        {
            Assert.IsNotEmpty(AllRulebooks(), "No ConsequenceRulebook assets found in the project.");
        }

        [Test]
        public void Rules_HaveUniqueIds_ConditionsAndEffects()
        {
            var problems = new List<string>();
            var seenIds = new HashSet<string>();

            foreach (ConsequenceRulebook book in AllRulebooks())
            {
                foreach (ConsequenceRule rule in book.Rules)
                {
                    if (rule == null || string.IsNullOrWhiteSpace(rule.id))
                    {
                        problems.Add($"{book.name}: a rule has no id");
                        continue;
                    }
                    if (!seenIds.Add(rule.id))
                        problems.Add($"{book.name}: duplicate rule id '{rule.id}'");
                    if (rule.condition == null || rule.condition.IsEmpty)
                        problems.Add($"{book.name}/{rule.id}: empty condition (it would fire immediately)");
                    if (!rule.HasEffects)
                        problems.Add($"{book.name}/{rule.id}: no effects");
                }
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        const string P4RulebookPath = "Assets/_Project/Data/Consequences/P4_Rulebook.asset";

        static IReadOnlyList<ConsequenceRule> P4Rules()
        {
            var book = AssetDatabase.LoadAssetAtPath<ConsequenceRulebook>(P4RulebookPath);
            Assert.IsNotNull(book, "Missing " + P4RulebookPath);
            return book.Rules;
        }

        [Test]
        public void P4Rulebook_BridgeDestroyed_ClosesTheSpan_AndOpensTheBypass()
        {
            var state = new WorldState();
            state.Set("p2span.bridge_destroyed");

            ConsequenceEngine.Apply(state, P4Rules());

            Assert.IsTrue(state.Has("outpost.span_closed"));
            Assert.IsTrue(state.Has("p2span.bypass_open"));
            Assert.AreEqual(ReputationBand.Hostile, ReputationRules.BandFor(state.GetReputation(Faction.Concord)));
        }

        [Test]
        public void P4Rulebook_SaveFromBeforeTheBypassRule_GetsTheBypass_WithoutRepeatingOldConsequences()
        {
            // What a P4 save looks like after the bridge fell: span_collapse already applied.
            var state = new WorldState();
            state.Set("p2span.bridge_destroyed");
            state.Set("outpost.span_closed");
            state.Set("outpost.rationing");
            state.Set("rule.span_collapse");
            state.AddReputation(Faction.Concord, -40);

            List<string> fired = ConsequenceEngine.Apply(state, P4Rules());

            CollectionAssert.AreEqual(new[] { "bypass_opens" }, fired);
            Assert.IsTrue(state.Has("p2span.bypass_open"));
            Assert.AreEqual(-40, state.GetReputation(Faction.Concord));
        }

        [Test]
        public void P4Rulebook_FreshWorld_KeepsTheBypassClosed()
        {
            var state = new WorldState();

            ConsequenceEngine.Apply(state, P4Rules());

            Assert.IsFalse(state.Has("p2span.bypass_open"));
        }
    }
}
