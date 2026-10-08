using System;

namespace TheDeep.Combat
{
    // Hit points for creatures (the player, the Scrapper). Plain C# so it can be unit-tested.
    // Structures don't use this: they have integrity (DestructionRules).
    public class Health
    {
        public float Max { get; }
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;

        public Health(float max)
        {
            Max = float.IsNaN(max) || max <= 0f ? 1f : max;
            Current = Max;
        }

        // Takes damage and returns how much was actually lost. Zero, negative or invalid
        // amounts do nothing, and nothing more happens once it's dead.
        public float TakeDamage(float amount)
        {
            if (IsDead || float.IsNaN(amount) || amount <= 0f)
                return 0f;
            float taken = Math.Min(amount, Current);
            Current -= taken;
            return taken;
        }

        // Instant death (e.g. falling into the chasm).
        public void Kill() => Current = 0f;
    }
}
