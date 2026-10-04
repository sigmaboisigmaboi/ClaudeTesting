using System;

namespace TheDeep.NPC
{
    // What an NPC is doing. Fall is permanent (it has become a physics body).
    public enum NpcState
    {
        Idle = 0,
        Travel = 1,
        Flee = 2,
        Pursue = 3,
        Stagger = 4,
        Fall = 5,
    }

    // Tuning for NpcBrain, edited in the Inspector on each NpcAgent.
    [Serializable]
    public class NpcSettings
    {
        public float noticeRadius = 7f;      // a hostile NPC starts pursuing within this distance
        public float loseRadius = 11f;       // ... and gives up beyond this distance
        public float catchRadius = 1.4f;     // ... and catches the player within this distance
        public float fleeDuration = 4f;      // seconds spent fleeing after a disturbance
        public float staggerDuration = 1f;   // seconds staggered after a strong impact
        public float staggerThreshold = 8f;  // impact strength (mass x speed) needed to stagger
    }

    // What the NPC senses this frame. Filled in by NpcAgent.
    public struct NpcInputs
    {
        public bool HasRoute;          // it has stops to travel between
        public bool Hostile;           // its hostility condition is met (e.g. Concord is Hostile)
        public float DistanceToPlayer; // float.PositiveInfinity if there is no player
        public bool Disturbed;         // a disturbance reached it this frame
        public bool LostFooting;       // the thing it stood on just broke
        public float ImpactStrength;   // strongest physical hit this frame (0 if none)
    }

    // The NPC's decision-making, in plain C# so it can be unit-tested. Priority, highest first:
    // Fall (permanent) > Stagger > Flee > Pursue (hostile) > Travel (has a route) > Idle.
    public class NpcBrain
    {
        readonly NpcSettings settings;
        float fleeLeft;
        float staggerLeft;
        bool pursuing;

        public NpcState State { get; private set; } = NpcState.Idle;

        // True on the tick the NPC is pursuing and close enough to catch the player.
        public bool CaughtPlayer { get; private set; }

        public NpcBrain(NpcSettings settings)
        {
            this.settings = settings ?? new NpcSettings();
        }

        public NpcState Tick(NpcInputs input, float deltaTime)
        {
            CaughtPlayer = false;
            if (State == NpcState.Fall)
                return State;
            if (input.LostFooting)
                return State = NpcState.Fall;

            if (deltaTime > 0f)
            {
                fleeLeft = Math.Max(0f, fleeLeft - deltaTime);
                staggerLeft = Math.Max(0f, staggerLeft - deltaTime);
            }
            if (input.ImpactStrength > 0f && input.ImpactStrength >= settings.staggerThreshold)
                staggerLeft = settings.staggerDuration;
            if (input.Disturbed)
                fleeLeft = settings.fleeDuration;

            // Pursuit is remembered through staggers and flinches until the player gets away.
            if (!input.Hostile || input.DistanceToPlayer > settings.loseRadius)
                pursuing = false;
            else if (input.DistanceToPlayer <= settings.noticeRadius)
                pursuing = true;

            if (staggerLeft > 0f)
                State = NpcState.Stagger;
            else if (fleeLeft > 0f)
                State = NpcState.Flee;
            else if (pursuing)
                State = NpcState.Pursue;
            else
                State = input.HasRoute ? NpcState.Travel : NpcState.Idle;

            CaughtPlayer = State == NpcState.Pursue && input.DistanceToPlayer <= settings.catchRadius;
            return State;
        }
    }
}
