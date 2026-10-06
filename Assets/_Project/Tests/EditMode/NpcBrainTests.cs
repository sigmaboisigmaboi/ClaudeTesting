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

        // ---- P6: combat (a Scrapper's settings) ----

        static readonly NpcSettings Scrapper = new NpcSettings
        {
            noticeRadius = 9f, loseRadius = 14f, catchRadius = 0f,
            fleeDuration = 0f, staggerDuration = 1f, staggerThreshold = 12f,
            alertDuration = 0.5f, attackRange = 1.8f, attackWindup = 0.6f, attackRecovery = 0.5f, attackCooldown = 1f,
        };

        static NpcInputs Chasing(float distance) =>
            new NpcInputs { HasRoute = true, Hostile = true, DistanceToPlayer = distance };

        // Notices the player at striking distance and gets through the alert: the attack starts.
        static NpcBrain ScrapperMidAttack()
        {
            var brain = new NpcBrain(Scrapper);
            brain.Tick(Chasing(1.5f), 0.1f);   // Alert
            brain.Tick(Chasing(1.5f), 0.5f);   // alert over: the attack starts
            return brain;
        }

        [Test]
        public void Noticing_GoesAlert_ThenPursues()
        {
            var brain = new NpcBrain(Scrapper);

            Assert.AreEqual(NpcState.Alert, brain.Tick(Chasing(8f), 0.1f));
            Assert.AreEqual(NpcState.Alert, brain.Tick(Chasing(8f), 0.3f));
            Assert.AreEqual(NpcState.Pursue, brain.Tick(Chasing(8f), 0.3f));
        }

        [Test]
        public void Attack_StartsOnlyWithinRange()
        {
            var brain = new NpcBrain(Scrapper);
            brain.Tick(Chasing(5f), 0.1f);
            Assert.AreEqual(NpcState.Pursue, brain.Tick(Chasing(5f), 0.5f));
            Assert.AreEqual(NpcState.Attack, brain.Tick(Chasing(1.5f), 0.1f));
        }

        [Test]
        public void Attack_StrikesExactlyOnce_AfterTheWindup_ThenCoolsDown()
        {
            NpcBrain brain = ScrapperMidAttack();
            Assert.AreEqual(NpcState.Attack, brain.State);
            Assert.IsTrue(brain.WindingUp);

            brain.Tick(Chasing(1.5f), 0.3f);
            Assert.IsFalse(brain.StrikeNow);              // still winding up
            brain.Tick(Chasing(1.5f), 0.35f);
            Assert.IsTrue(brain.StrikeNow);               // past 0.6 s: the blow
            Assert.IsFalse(brain.WindingUp);
            brain.Tick(Chasing(1.5f), 0.1f);
            Assert.IsFalse(brain.StrikeNow);              // only once
            Assert.AreEqual(NpcState.Attack, brain.State); // recovering

            Assert.AreEqual(NpcState.Pursue, brain.Tick(Chasing(1.5f), 0.4f)); // done; cooling down
            Assert.AreEqual(NpcState.Attack, brain.Tick(Chasing(1.5f), 1f));   // ready again
        }

        [Test]
        public void Attack_IsCommitted_EvenIfThePlayerStepsBack()
        {
            NpcBrain brain = ScrapperMidAttack();

            brain.Tick(Chasing(4f), 0.3f);
            Assert.AreEqual(NpcState.Attack, brain.State);
            brain.Tick(Chasing(4f), 0.35f);
            Assert.IsTrue(brain.StrikeNow); // it swings anyway; NpcAgent decides it misses
        }

        [Test]
        public void StrongHit_StaggersAndCancelsTheWindup_NoStrike()
        {
            NpcBrain brain = ScrapperMidAttack();
            NpcInputs hit = Chasing(1.5f);
            hit.ImpactStrength = 15f;

            Assert.AreEqual(NpcState.Stagger, brain.Tick(hit, 0.1f));
            Assert.IsFalse(brain.StrikeNow);
            for (int i = 0; i < 11; i++)
            {
                brain.Tick(Chasing(1.5f), 0.1f);
                Assert.IsFalse(brain.StrikeNow, "a cancelled attack must never strike");
            }
            Assert.AreEqual(NpcState.Attack, brain.State); // recovered and cooled down: a fresh wind-up
        }

        [Test]
        public void WeakHit_DoesNotStaggerTheScrapper()
        {
            NpcBrain brain = ScrapperMidAttack();
            NpcInputs melee = Chasing(1.5f);
            melee.ImpactStrength = 10f; // the player's melee: below the Scrapper's threshold of 12

            Assert.AreEqual(NpcState.Attack, brain.Tick(melee, 0.3f));
            brain.Tick(Chasing(1.5f), 0.35f);
            Assert.IsTrue(brain.StrikeNow);
        }

        [Test]
        public void Scrapper_DoesNotFlee()
        {
            var brain = new NpcBrain(Scrapper);
            NpcInputs disturbed = Chasing(20f);
            disturbed.Disturbed = true;

            Assert.AreEqual(NpcState.Travel, brain.Tick(disturbed, 0.1f));
        }

        [Test]
        public void Dead_IsPermanent_AndOverridesEverything()
        {
            NpcBrain brain = ScrapperMidAttack();
            var dead = new NpcInputs { Dead = true };

            Assert.AreEqual(NpcState.Dead, brain.Tick(dead, 0.1f));
            NpcInputs everything = Chasing(1f);
            everything.ImpactStrength = 50f;
            everything.Disturbed = true;
            everything.LostFooting = true;
            Assert.AreEqual(NpcState.Dead, brain.Tick(everything, 0.1f));
            Assert.IsFalse(brain.StrikeNow);
        }

        [Test]
        public void Falling_ThenDying_EndsDead()
        {
            var brain = new NpcBrain(Scrapper);
            NpcInputs falling = Chasing(5f);
            falling.LostFooting = true;

            Assert.AreEqual(NpcState.Fall, brain.Tick(falling, 0.1f));
            Assert.AreEqual(NpcState.Dead, brain.Tick(new NpcInputs { Dead = true }, 0.1f));
        }

        [Test]
        public void NonCombatNpc_NeverAttacksOrAlerts()
        {
            // The P5 defaults: no attack range, no alert. A guard right next to the player only pursues.
            var brain = new NpcBrain(Settings);
            NpcInputs input = Calm();
            input.Hostile = true;
            input.DistanceToPlayer = 0.5f;

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(NpcState.Pursue, brain.Tick(input, 0.1f));
                Assert.IsFalse(brain.StrikeNow);
            }
        }
    }
}
