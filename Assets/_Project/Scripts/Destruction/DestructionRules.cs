namespace TheDeep.Destruction
{
    // The rules for structural integrity, kept as plain functions so they can be unit-tested.
    // Integrity is how much punishment a structure can take — it is NOT a creature health system.
    public static class DestructionRules
    {
        // Hits at or below the threshold do nothing; stronger hits deal the amount above it.
        // This lets light bumps and settling objects be ignored while strong hits still count fully.
        public static float DamageFromImpact(float impactStrength, float threshold)
        {
            if (float.IsNaN(impactStrength) || float.IsNaN(threshold))
                return 0f;
            float damage = impactStrength - threshold;
            return damage > 0f ? damage : 0f;
        }

        // Integrity never drops below zero.
        public static float ApplyDamage(float integrity, float damage)
        {
            if (float.IsNaN(damage) || damage <= 0f)
                return integrity;
            float remaining = integrity - damage;
            return remaining > 0f ? remaining : 0f;
        }

        public static bool IsBroken(float integrity) => integrity <= 0f;

        // A supported structure collapses only when it has supports and every one of them is gone.
        public static bool ShouldCollapse(int supportCount, int supportsStillStanding)
            => supportCount > 0 && supportsStillStanding <= 0;
    }
}
