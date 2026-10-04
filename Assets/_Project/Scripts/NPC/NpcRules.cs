using UnityEngine;

namespace TheDeep.NPC
{
    // Small pure calculations used by NpcAgent, kept separate so they can be unit-tested.
    public static class NpcRules
    {
        // The stop after the current one, looping back to the first.
        public static int NextStop(int current, int count) => count <= 0 ? 0 : (current + 1) % count;

        // Horizontal direction pointing from 'source' to 'self' (away from the source).
        // Falls back to 'fallback' (flattened) when they're on top of each other.
        public static Vector3 AwayDirection(Vector3 source, Vector3 self, Vector3 fallback)
        {
            Vector3 away = self - source;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = fallback;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f)
                    away = new Vector3(0f, 0f, 1f);
            }
            return away.normalized;
        }

        // How far a hit shoves a staggered NPC: proportional to impact strength, capped.
        public static float ShoveDistance(float strength, float metersPerStrength, float maxDistance)
        {
            if (float.IsNaN(strength) || strength <= 0f || metersPerStrength <= 0f || maxDistance <= 0f)
                return 0f;
            return Mathf.Min(strength * metersPerStrength, maxDistance);
        }
    }
}
