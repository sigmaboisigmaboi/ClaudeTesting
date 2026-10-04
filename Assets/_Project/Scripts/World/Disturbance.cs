using System;
using UnityEngine;

namespace TheDeep.World
{
    // Something loud and physical happened here (a structure broke or collapsed). NPCs within the
    // radius react (flee), and anyone standing on the broken object's footprint loses their footing.
    // DESIGN §10: a simple "disturbance" broadcast instead of per-NPC perception.
    public readonly struct Disturbance
    {
        // How far below / above the top of the footprint someone's feet can be and still count as standing on it.
        const float FootprintBelowTolerance = 0.5f;
        const float FootprintAboveTolerance = 0.75f;

        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly bool HasFootprint;
        public readonly Bounds Footprint;

        public Disturbance(Vector3 position, float radius, Bounds? footprint = null)
        {
            Position = position;
            Radius = radius;
            HasFootprint = footprint.HasValue;
            Footprint = footprint ?? default(Bounds);
        }

        // True if the point is within the radius.
        public bool Reaches(Vector3 point) =>
            Radius > 0f && (point - Position).sqrMagnitude <= Radius * Radius;

        // True if feet at this point were standing on top of the object that broke.
        public bool IsUnderfoot(Vector3 feet)
        {
            if (!HasFootprint)
                return false;
            Vector3 min = Footprint.min, max = Footprint.max;
            return feet.x >= min.x && feet.x <= max.x &&
                   feet.z >= min.z && feet.z <= max.z &&
                   feet.y >= min.y - FootprintBelowTolerance &&
                   feet.y <= max.y + FootprintAboveTolerance;
        }
    }

    // Where disturbances are announced. Destructible raises one when it breaks; NpcAgent listens.
    public static class Disturbances
    {
        public static event Action<Disturbance> Raised;

        public static void Raise(Disturbance disturbance) => Raised?.Invoke(disturbance);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForNewPlaySession() => Raised = null;
    }
}
