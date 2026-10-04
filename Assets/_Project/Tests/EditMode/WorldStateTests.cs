using NUnit.Framework;
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
        }
    }
}
