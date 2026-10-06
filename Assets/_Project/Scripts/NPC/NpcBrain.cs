using System;

namespace TheDeep.NPC
{
    // What an NPC is doing. Fall and Dead are permanent.
    // New values are only ever added at the end.
    public enum NpcState
    {
        Idle = 0,
        Travel = 1,
        Flee = 2,
        Pursue = 3,
        Stagger = 4,
        Fall = 5,
        Alert = 6,   // P6: has just noticed the player and is about to chase
        Attack = 7,  // P6: winding up / striking / recovering
        Dead = 8,    // P6: out of health or dropped to its death
    }

    // Tuning for NpcBrain, edited in the Inspector on each NpcAgent.
    // The P6 combat settings default to "off", so P5 NPCs behave exactly as before.
    [Serializable]
    public class NpcSettings
    {
        public float noticeRadius = 7f;      // a hostile NPC starts pursuing within this distance
        public float loseRadius = 11f;       // ... and gives up beyond this distance
        public float catchRadius = 1.4f;     // ... and catches the player within this distance
        public float fleeDuration = 4f;      // seconds spent fleeing after a disturbance (0 = never flees)
        public float staggerDuration = 1f;   // seconds staggered after a strong impact
        public float staggerThreshold = 8f;  // impact strength (mass x speed) needed to stagger
        public float alertDuration = 0f;     // seconds standing alert before chasing (0 = chase at once)
        public float attackRange = 0f;       // starts an attack within this distance (0 = never attacks)
        public float attackWindup = 0.6f;    // seconds from starting an attack to the strike
        public float attackRecovery = 0.5f;  // seconds after the strike before it can move again
        public float attackCooldown = 1f;    // seconds after an attack before the next can start
    }

    // What the NPC senses this frame. Filled in by NpcAgent.
    public struct NpcInputs
    {
        public bool HasRoute;          // it has stops to travel between
        public bool Hostile;           // it is hostile to the player right now
        public float DistanceToPlayer; // float.PositiveInfinity if there is no player
        public bool Disturbed;         // a disturbance reached it this frame
        public bool LostFooting;       // the thing it stood on just broke
        public float ImpactStrength;   // strongest physical hit this frame (0 if none)
        public bool Dead;              // its health ran out, or it dropped to its death
    }

    // The NPC's decision-making, in plain C# so it can be unit-tested. Priority, highest first:
    // Dead > Fall > Stagger > Flee > Attack > Alert > Pursue (hostile) > Travel (has a route) > Idle.
    // A stagger or a flinch cancels an attack before it strikes.
    public class NpcBrain
    {
        readonly NpcSettings settings;
        float fleeLeft;
        float staggerLeft;
        float alertLeft;
        float attackLeft;
        float cooldownLeft;
        bool pursuing;
        bool attacking;
        bool strikePending;

        public NpcState State { get; private set; } = NpcState.Idle;

        // True on the tick the NPC is pursuing and close enough to catch the player.
        public bool CaughtPlayer { get; private set; }

        // True on the single tick an attack's wind-up ends: the moment the blow lands (or misses).
        public bool StrikeNow { get; private set; }

        // True while an attack is winding up (before the strike), for the visual tell.
        public bool WindingUp => attacking && strikePending;

        public NpcBrain(NpcSettings settings)
        {
            this.settings = settings ?? new NpcSettings();
        }

        public NpcState Tick(NpcInputs input, float deltaTime)
        {
            CaughtPlayer = false;
            StrikeNow = false;
            if (State == NpcState.Dead)
                return State;
            if (input.Dead)
            {
                CancelAttack();
                return State = NpcState.Dead;
            }
            if (State == NpcState.Fall)
                return State;
            if (input.LostFooting)
            {
                CancelAttack();
                return State = NpcState.Fall;
            }

            if (deltaTime > 0f)
            {
                fleeLeft = Math.Max(0f, fleeLeft - deltaTime);
                staggerLeft = Math.Max(0f, staggerLeft - deltaTime);
                alertLeft = Math.Max(0f, alertLeft - deltaTime);
                cooldownLeft = Math.Max(0f, cooldownLeft - deltaTime);
                if (attacking)
                    attackLeft -= deltaTime;
            }
            if (input.ImpactStrength > 0f && input.ImpactStrength >= settings.staggerThreshold)
            {
                staggerLeft = settings.staggerDuration;
                CancelAttack();
            }
            if (input.Disturbed && settings.fleeDuration > 0f)
            {
                fleeLeft = settings.fleeDuration;
                CancelAttack();
            }

            // Pursuit is remembered through staggers and flinches until the player gets away.
            bool wasPursuing = pursuing;
            if (!input.Hostile || input.DistanceToPlayer > settings.loseRadius)
                pursuing = false;
            else if (input.DistanceToPlayer <= settings.noticeRadius)
                pursuing = true;
            if (pursuing && !wasPursuing)
                alertLeft = settings.alertDuration;
            if (!pursuing)
                alertLeft = 0f;

            // An attack in progress: strike once when the wind-up is over, then recover.
            if (attacking && strikePending && attackLeft <= settings.attackRecovery)
            {
                StrikeNow = true;
                strikePending = false;
            }
            if (attacking && attackLeft <= 0f)
            {
                attacking = false;
                cooldownLeft = settings.attackCooldown;
            }

            // Start a new attack when chasing, close enough, and ready.
            if (!attacking && pursuing && settings.attackRange > 0f && staggerLeft <= 0f && fleeLeft <= 0f &&
                alertLeft <= 0f && cooldownLeft <= 0f && input.DistanceToPlayer <= settings.attackRange)
            {
                attacking = true;
                strikePending = true;
                attackLeft = settings.attackWindup + settings.attackRecovery;
                if (attackLeft <= settings.attackRecovery) // no wind-up: strike at once
                {
                    StrikeNow = true;
                    strikePending = false;
                }
            }

            if (staggerLeft > 0f)
                State = NpcState.Stagger;
            else if (fleeLeft > 0f)
                State = NpcState.Flee;
            else if (attacking)
                State = NpcState.Attack;
            else if (pursuing && alertLeft > 0f)
                State = NpcState.Alert;
            else if (pursuing)
                State = NpcState.Pursue;
            else
                State = input.HasRoute ? NpcState.Travel : NpcState.Idle;

            CaughtPlayer = State == NpcState.Pursue && input.DistanceToPlayer <= settings.catchRadius;
            return State;
        }

        // A cancelled attack doesn't strike, and still has to cool down.
        void CancelAttack()
        {
            if (!attacking)
                return;
            attacking = false;
            strikePending = false;
            attackLeft = 0f;
            cooldownLeft = settings.attackCooldown;
        }
    }
}
