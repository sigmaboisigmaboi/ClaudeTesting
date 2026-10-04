using NUnit.Framework;
using TheDeep.NPC;

namespace TheDeep.Tests.EditMode
{
    public class NpcBrainTests
    {
        static readonly NpcSettings Settings = new NpcSettings
        {
            noticeRadius = 7f, loseRadius = 11f, catchRadius = 1.4f,
            fleeDuration = 4f, staggerDuration = 1f, staggerThreshold = 8f,
        };

        static NpcInputs Calm(bool hasRoute = true) =>
            new NpcInputs { HasRoute = hasRoute, DistanceToPlayer = float.PositiveInfinity };

        [Test]
        public void WithARoute_Travels_WithoutOne_Idles()
        {
            Assert.AreEqual(NpcState.Travel, new NpcBrain(Settings).Tick(Calm(), 0.1f));
            Assert.AreEqual(NpcState.Idle, new NpcBrain(Settings).Tick(Calm(hasRoute: false), 0.1f));
        }

        [Test]
        public void Disturbance_MakesItFlee_ThenItReturnsToItsRoute()
        {
            var brain = new NpcBrain(Settings);
            NpcInputs disturbed = Calm();
            disturbed.Disturbed = true;

            Assert.AreEqual(NpcState.Flee, brain.Tick(disturbed, 0.1f));
            Assert.AreEqual(NpcState.Flee, brain.Tick(Calm(), 3.5f));
            Assert.AreEqual(NpcState.Travel, brain.Tick(Calm(), 1f));
        }

        [Test]
        public void HostileNpc_PursuesWithinNoticeRadius_AndGivesUpBeyondLoseRadius()
        {
            var brain = new NpcBrain(Settings);
            NpcInputs input = Calm();
            input.Hostile = true;

            input.DistanceToPlayer = 9f;   // outside notice radius: not noticed yet
            Assert.AreEqual(NpcState.Travel, brain.Tick(input, 0.1f));
            input.DistanceToPlayer = 6f;   // noticed
            Assert.AreEqual(NpcState.Pursue, brain.Tick(input, 0.1f));
            input.DistanceToPlayer = 10f;  // still within lose radius: keeps chasing
            Assert.AreEqual(NpcState.Pursue, brain.Tick(input, 0.1f));
            input.DistanceToPlayer = 12f;  // got away
            Assert.AreEqual(NpcState.Travel, brain.Tick(input, 0.1f));
        }

        [Test]
        public void NonHostileNpc_IgnoresThePlayer()
        {
            var brain = new NpcBrain(Settings);
            NpcInputs input = Calm();
            input.DistanceToPlayer = 1f;

            Assert.AreEqual(NpcState.Travel, brain.Tick(input, 0.1f));
            Assert.IsFalse(brain.CaughtPlayer);
        }

        [Test]
        public void Pursuer_CatchesThePlayer_OnlyWithinCatchRadius()
        {
            var brain = new NpcBrain(Settings);
            NpcInputs input = Calm();
            input.Hostile = true;

            input.DistanceToPlayer = 3f;
            brain.Tick(input, 0.1f);
            Assert.IsFalse(brain.CaughtPlayer);
            input.DistanceToPlayer = 1.2f;
            brain.Tick(input, 0.1f);
            Assert.IsTrue(brain.CaughtPlayer);
        }

        [Test]
        public void StrongImpact_Staggers_WeakImpactDoesNot()
        {
            var brain = new NpcBrain(Settings);
            NpcInputs weak = Calm();
            weak.ImpactStrength = 5f;
            NpcInputs strong = Calm();
            strong.ImpactStrength = 10f;

            Assert.AreEqual(NpcState.Travel, brain.Tick(weak, 0.1f));
            Assert.AreEqual(NpcState.Stagger, brain.Tick(strong, 0.1f));
            Assert.AreEqual(NpcState.Stagger, brain.Tick(Calm(), 0.5f));
            Assert.AreEqual(NpcState.Travel, brain.Tick(Calm(), 0.6f));
        }

        [Test]
        public void StaggeredPursuer_ResumesTheChase_IfThePlayerIsStillClose()
        {
            var brain = new NpcBrain(Settings);
            NpcInputs chase = Calm();
            chase.Hostile = true;
            chase.DistanceToPlayer = 5f;
            brain.Tick(chase, 0.1f);

            NpcInputs hit = chase;
            hit.DistanceToPlayer = 9f; // the player backed off past the notice radius while it was hit
            hit.ImpactStrength = 20f;
            Assert.AreEqual(NpcState.Stagger, brain.Tick(hit, 0.1f));

            NpcInputs after = chase;
            after.DistanceToPlayer = 9f;
            Assert.AreEqual(NpcState.Pursue, brain.Tick(after, 1.5f));
        }

        [Test]
        public void LostFooting_FallsForGood()
        {
            var brain = new NpcBrain(Settings);
            NpcInputs falling = Calm();
            falling.LostFooting = true;

            Assert.AreEqual(NpcState.Fall, brain.Tick(falling, 0.1f));
            NpcInputs disturbed = Calm();
            disturbed.Disturbed = true;
            Assert.AreEqual(NpcState.Fall, brain.Tick(disturbed, 5f));
            Assert.AreEqual(NpcState.Fall, brain.Tick(Calm(), 5f));
        }

        [Test]
        public void Stagger_TakesPriorityOverFlee_AndFleeOverPursuit()
        {
            var brain = new NpcBrain(Settings);
            NpcInputs everything = Calm();
            everything.Hostile = true;
            everything.DistanceToPlayer = 3f;
            everything.Disturbed = true;
            everything.ImpactStrength = 50f;

            Assert.AreEqual(NpcState.Stagger, brain.Tick(everything, 0.1f));
            NpcInputs stillChasing = Calm();
            stillChasing.Hostile = true;
            stillChasing.DistanceToPlayer = 3f;
            Assert.AreEqual(NpcState.Flee, brain.Tick(stillChasing, 1.1f));   // stagger over, still fleeing
            Assert.AreEqual(NpcState.Pursue, brain.Tick(stillChasing, 3f));   // flee over, back to the chase
        }
    }
}
