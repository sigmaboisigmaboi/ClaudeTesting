using NUnit.Framework;
using TheDeep.Factions;
using TheDeep.State;

namespace TheDeep.Tests.EditMode
{
    public class WorldStateTests
    {
        [Test]
        public void Set_RecordsFact_AndUnsetFactIsNotPresent()
        {
            var state = new WorldState();

            state.Set("bootstrap.crate_on_pad");

            Assert.IsTrue(state.Has("bootstrap.crate_on_pad"));
            Assert.IsFalse(state.Has("some.other_fact"));
        }

        [Test]
        public void ToJson_ThenFromJson_KeepsFacts()
        {
            var original = new WorldState();
            original.Set("bootstrap.crate_on_pad");

            WorldState loaded = WorldState.FromJson(original.ToJson());

            Assert.IsTrue(loaded.Has("bootstrap.crate_on_pad"));
            Assert.AreEqual(1, loaded.FactCount);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("this is not json")]
        public void FromJson_EmptyOrInvalid_ReturnsEmptyState(string json)
        {
            WorldState state = WorldState.FromJson(json);

            Assert.AreEqual(0, state.FactCount);
            Assert.AreEqual(0, state.DestroyedCount);
        }

        [Test]
        public void MarkDestroyed_RecordsId_OnceOnly()
        {
            var state = new WorldState();

            state.MarkDestroyed("p2span.wall_alcove");
            state.MarkDestroyed("p2span.wall_alcove");

            Assert.IsTrue(state.IsDestroyed("p2span.wall_alcove"));
            Assert.IsFalse(state.IsDestroyed("p2span.pillar_left"));
            Assert.AreEqual(1, state.DestroyedCount);
        }

        [Test]
        public void ToJson_ThenFromJson_KeepsFactsAndDestroyedIds()
        {
            var original = new WorldState();
            original.Set("p2span.bridge_destroyed");
            original.MarkDestroyed("p2span.bridge_deck");

            WorldState loaded = WorldState.FromJson(original.ToJson());

            Assert.IsTrue(loaded.Has("p2span.bridge_destroyed"));
            Assert.IsTrue(loaded.IsDestroyed("p2span.bridge_deck"));
            Assert.AreEqual(WorldState.CurrentVersion, loaded.Version);
        }

        [Test]
        public void FromJson_Version1File_KeepsFactsAndStartsWithNoDestroyedIds()
        {
            // Exactly what P3.1 (WorldState version 1) wrote to disk.
            const string version1Json = "{\"version\":1,\"facts\":[\"bootstrap.crate_on_pad\"]}";

            WorldState loaded = WorldState.FromJson(version1Json);

            Assert.IsTrue(loaded.Has("bootstrap.crate_on_pad"));
            Assert.AreEqual(0, loaded.DestroyedCount);
            Assert.AreEqual(WorldState.CurrentVersion, loaded.Version);
        }

        [Test]
        public void Reputation_StartsAtZero_AndAddsUp_WithinLimits()
        {
            var state = new WorldState();

            Assert.AreEqual(0, state.GetReputation(Faction.Concord));
            state.AddReputation(Faction.Concord, -40);
            state.AddReputation(Faction.Concord, -15);
            Assert.AreEqual(-55, state.GetReputation(Faction.Concord));
            Assert.AreEqual(0, state.GetReputation(Faction.Delvers));

            state.AddReputation(Faction.Hollowers, 500);
            Assert.AreEqual(100, state.GetReputation(Faction.Hollowers));
        }

        [Test]
        public void ToJson_ThenFromJson_KeepsReputation()
        {
            var original = new WorldState();
            original.AddReputation(Faction.Concord, -40);
            original.AddReputation(Faction.Hollowers, 20);

            WorldState loaded = WorldState.FromJson(original.ToJson());

            Assert.AreEqual(-40, loaded.GetReputation(Faction.Concord));
            Assert.AreEqual(20, loaded.GetReputation(Faction.Hollowers));
            Assert.AreEqual(0, loaded.GetReputation(Faction.Delvers));
        }

        [Test]
        public void FromJson_Version2File_KeepsEverything_AndStartsWithNeutralReputation()
        {
            // Exactly what P2 (WorldState version 2) wrote to disk.
            const string version2Json = "{\"version\":2,\"facts\":[\"p2span.bridge_destroyed\"],\"destroyedIds\":[\"p2span.bridge_deck\"]}";

            WorldState loaded = WorldState.FromJson(version2Json);

            Assert.IsTrue(loaded.Has("p2span.bridge_destroyed"));
            Assert.IsTrue(loaded.IsDestroyed("p2span.bridge_deck"));
            Assert.AreEqual(0, loaded.GetReputation(Faction.Concord));
            Assert.AreEqual(0, loaded.Reputations.Count);
            Assert.AreEqual(WorldState.CurrentVersion, loaded.Version);
        }
    }
}
