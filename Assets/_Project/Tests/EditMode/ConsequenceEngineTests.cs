using System.Collections.Generic;
using NUnit.Framework;
using TheDeep.Consequences;
using TheDeep.Factions;
using TheDeep.State;

namespace TheDeep.Tests.EditMode
{
    public class ConsequenceEngineTests
    {
        static ConsequenceRule SpanCollapse() => new ConsequenceRule
        {
            id = "span_collapse",
            condition = new StateCondition { requiredFacts = new List<string> { "p2span.bridge_destroyed" } },
            factsToSet = new List<string> { "outpost.span_closed" },
            reputationChanges = new List<ConsequenceRule.ReputationChange>
            {
                new ConsequenceRule.ReputationChange { faction = Faction.Concord, amount = -40 },
                new ConsequenceRule.ReputationChange { faction = Faction.Hollowers, amount = 20 },
            }
        };

        static ConsequenceRule CacheLooted() => new ConsequenceRule
        {
            id = "cache_looted",
            condition = new StateCondition { requiredDestroyedIds = new List<string> { "p2span.wall_alcove" } },
            factsToSet = new List<string> { "outpost.cache_looted" },
            reputationChanges = new List<ConsequenceRule.ReputationChange>
            {
                new ConsequenceRule.ReputationChange { faction = Faction.Hollowers, amount = -40 },
            }
        };

        [Test]
        public void Rule_DoesNotFire_WhenConditionUnmet()
        {
            var state = new WorldState();

            List<string> fired = ConsequenceEngine.Apply(state, new[] { SpanCollapse() });

            Assert.IsEmpty(fired);
            Assert.IsFalse(state.Has("outpost.span_closed"));
            Assert.AreEqual(0, state.GetReputation(Faction.Concord));
        }

        [Test]
        public void Rule_Fires_AndAppliesItsEffects()
        {
            var state = new WorldState();
            state.Set("p2span.bridge_destroyed");

            List<string> fired = ConsequenceEngine.Apply(state, new[] { SpanCollapse() });

            CollectionAssert.AreEqual(new[] { "span_collapse" }, fired);
            Assert.IsTrue(state.Has("outpost.span_closed"));
            Assert.AreEqual(-40, state.GetReputation(Faction.Concord));
            Assert.AreEqual(20, state.GetReputation(Faction.Hollowers));
        }

        [Test]
        public void Rule_FiresOnlyOnce_EvenWhenAppliedRepeatedly()
        {
            var state = new WorldState();
            state.Set("p2span.bridge_destroyed");
            var rules = new[] { SpanCollapse() };

            ConsequenceEngine.Apply(state, rules);
            List<string> secondTime = ConsequenceEngine.Apply(state, rules);
            ConsequenceEngine.Apply(state, rules);

            Assert.IsEmpty(secondTime);
            Assert.AreEqual(-40, state.GetReputation(Faction.Concord));
        }

        [Test]
        public void RuleOnceFlag_IsRecordedAsAFact()
        {
            var state = new WorldState();
            state.Set("p2span.bridge_destroyed");
            ConsequenceEngine.Apply(state, new[] { SpanCollapse() });

            Assert.IsTrue(state.Has("rule.span_collapse"));
        }

        [Test]
        public void IndependentRules_Combine()
        {
            var state = new WorldState();
            state.Set("p2span.bridge_destroyed");
            state.MarkDestroyed("p2span.wall_alcove");

            List<string> fired = ConsequenceEngine.Apply(state, new[] { SpanCollapse(), CacheLooted() });

            Assert.AreEqual(2, fired.Count);
            Assert.AreEqual(-20, state.GetReputation(Faction.Hollowers)); // +20 then -40
            Assert.AreEqual(ReputationBand.Unfriendly, ReputationRules.BandFor(state.GetReputation(Faction.Hollowers)));
        }

        [Test]
        public void OneRulesEffects_CanEnableAnotherRule()
        {
            var followUp = new ConsequenceRule
            {
                id = "follow_up",
                condition = new StateCondition { requiredFacts = new List<string> { "outpost.span_closed" } },
                factsToSet = new List<string> { "outpost.follow_up_happened" }
            };
            var state = new WorldState();
            state.Set("p2span.bridge_destroyed");

            // The follow-up is listed first, so it can only fire on the engine's second pass.
            List<string> fired = ConsequenceEngine.Apply(state, new[] { followUp, SpanCollapse() });

            CollectionAssert.AreEqual(new[] { "span_collapse", "follow_up" }, fired);
            Assert.IsTrue(state.Has("outpost.follow_up_happened"));
        }

        [Test]
        public void NullOrIncompleteInput_IsIgnored()
        {
            var state = new WorldState();

            Assert.IsEmpty(ConsequenceEngine.Apply(state, null));
            Assert.IsEmpty(ConsequenceEngine.Apply(null, new[] { SpanCollapse() }));
            Assert.IsEmpty(ConsequenceEngine.Apply(state, new ConsequenceRule[] { null, new ConsequenceRule { id = "" } }));
        }
    }
}
