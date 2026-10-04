namespace TheDeep.World
{
    // Shared impact formula (D-019), used by ImpactMeter and Destructible.
    public static class ImpactMath
    {
        // Impact strength = incoming mass (kg) × impact speed (m/s), i.e. the incoming object's momentum.
        // Doubling either the mass or the speed doubles the strength. Invalid input gives 0.
        public static float Strength(float mass, float speed)
        {
            if (float.IsNaN(mass) || float.IsNaN(speed) || mass <= 0f || speed <= 0f)
                return 0f;
            return mass * speed;
        }
    }
}
