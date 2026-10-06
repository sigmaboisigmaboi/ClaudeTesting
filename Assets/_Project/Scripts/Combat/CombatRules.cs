using UnityEngine;

namespace TheDeep.Combat
{
    // The combat formulas, kept as plain functions so they can be unit-tested.
    public static class CombatRules
    {
        // Damage from a physical hit (a thrown or pushed crate): the impact strength (mass x speed,
        // D-019) above the threshold, scaled, and capped so no single hit is overwhelming.
        public static float ImpactDamage(float strength, float threshold, float scale, float maxDamage)
        {
            if (float.IsNaN(strength) || float.IsNaN(threshold) || float.IsNaN(scale) || scale <= 0f || maxDamage <= 0f)
                return 0f;
            float damage = (strength - threshold) * scale;
            if (damage <= 0f)
                return 0f;
            return damage < maxDamage ? damage : maxDamage;
        }

        // Whether an attack lands: the target must be within reach (horizontally), roughly level,
        // and inside the cone in front of the attacker (maxAngle degrees either side).
        public static bool StrikeConnects(Vector3 attacker, Vector3 attackerForward, Vector3 target, float reach, float maxAngle)
        {
            Vector3 toTarget = target - attacker;
            if (Mathf.Abs(toTarget.y) > 1.5f)
                return false;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance > reach)
                return false;
            if (distance < 0.01f)
                return true;

            Vector3 forward = attackerForward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return false;
            float cosAngle = Vector3.Dot(forward.normalized, toTarget / distance);
            return cosAngle >= Mathf.Cos(maxAngle * Mathf.Deg2Rad);
        }

        // A fixed horizontal push of 'distance' meters, pointing from the attacker to the target.
        // If they're on top of each other, pushes along 'fallback' instead.
        public static Vector3 KnockbackDisplacement(Vector3 attacker, Vector3 target, float distance, Vector3 fallback)
        {
            Vector3 away = target - attacker;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = fallback;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f)
                    away = new Vector3(0f, 0f, 1f);
            }
            return away.normalized * Mathf.Max(0f, distance);
        }

        // An NPC that has dropped more than this far since it started falling is dead.
        public static bool IsLethalDrop(float startHeight, float currentHeight, float lethalDrop) =>
            startHeight - currentHeight > lethalDrop;
    }
}
