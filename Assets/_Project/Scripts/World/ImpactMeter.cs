using UnityEngine;

namespace TheDeep.World
{
    // P1.4 prototype: measures physical impacts on this object, logs them, and adds a knockback shove.
    // Not a health/damage system — it only measures and reacts, so we can judge how impacts feel.
    // Needs a Rigidbody (the knockback is applied to it) and a collider on this GameObject.
    [RequireComponent(typeof(Rigidbody))]
    public class ImpactMeter : MonoBehaviour
    {
        [Tooltip("Impacts weaker than this (kg·m/s) are ignored: no log, no knockback. Filters out gentle touches.")]
        [SerializeField] float minimumStrength = 5f;

        [Tooltip("Extra knockback momentum per unit of impact strength. 0 = rely on Unity's collision response only.")]
        [SerializeField] float knockbackScale = 3f;

        [Tooltip("Upper limit on the speed one knockback can add, in m/s, so huge hits don't launch the target.")]
        [SerializeField] float maxKnockbackSpeed = 6f;

        Rigidbody body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        void OnCollisionEnter(Collision collision)
        {
            // Only moving physics objects count (crates), not the floor or walls.
            Rigidbody other = collision.rigidbody;
            if (other == null)
                return;

            // relativeVelocity is the closing speed measured by the physics engine at the moment
            // of contact, before the collision is resolved — more reliable than reading the
            // other object's velocity afterwards.
            float speed = collision.relativeVelocity.magnitude;
            float strength = ImpactMath.Strength(other.mass, speed);
            if (strength < minimumStrength)
                return;

            Debug.Log($"Impact on {name}: {other.name} ({other.mass:0.#} kg) at {speed:0.0} m/s → strength {strength:0.0}");

            ApplyKnockback(other, strength);
        }

        // A 60 kg target barely moves from light hits through physics alone, so add a shove
        // proportional to the measured impact, pushed horizontally away from the incoming object.
        void ApplyKnockback(Rigidbody other, float strength)
        {
            Vector3 away = transform.position - other.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                return;

            float knockbackMomentum = Mathf.Min(strength * knockbackScale, maxKnockbackSpeed * body.mass);
            body.AddForce(away.normalized * knockbackMomentum, ForceMode.Impulse);
        }
    }
}
