using UnityEngine;
using UnityEngine.InputSystem;

namespace TheDeep.Player
{
    // Pick up, carry, drop and throw light physics objects.
    // E (Player/Interact) picks up or drops; left mouse (Player/Attack) throws while holding.
    // Held objects are moved by setting their velocity (never teleported), so they can't pass through walls.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerGrabThrow : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraTransform;

        [Header("Grabbing")]
        [Tooltip("How far away an object can be picked up, in meters.")]
        [SerializeField] float grabRange = 3f;
        [Tooltip("Thickness of the aim check, so aiming doesn't need to be exact (no crosshair yet).")]
        [SerializeField] float grabRadius = 0.2f;
        [Tooltip("Objects heavier than this (kg) can't be picked up.")]
        [SerializeField] float maxGrabMass = 30f;

        [Header("Holding")]
        [Tooltip("How far in front of the camera a held object floats, in meters.")]
        [SerializeField] float holdDistance = 2f;
        [Tooltip("How quickly a held object moves toward the hold point.")]
        [SerializeField] float followStrength = 15f;
        [Tooltip("Top speed of a held object, in m/s. Keeps it from flinging around.")]
        [SerializeField] float maxHoldSpeed = 15f;
        [Tooltip("If a held object gets snagged this far from the hold point, it is dropped.")]
        [SerializeField] float autoDropDistance = 3f;

        [Header("Throwing")]
        [Tooltip("Throw impulse. The same throw sends light objects much further than heavy ones.")]
        [SerializeField] float throwImpulse = 15f;

        CharacterController controller;
        InputAction interactAction;
        InputAction attackAction;

        Rigidbody heldBody;
        Collider[] heldColliders;
        bool heldBodyUsedGravity;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            interactAction = InputSystem.actions.FindAction("Player/Interact", throwIfNotFound: true);
            attackAction = InputSystem.actions.FindAction("Player/Attack", throwIfNotFound: true);
        }

        void Update()
        {
            // WasPressedThisFrame reacts to the key press itself, so a quick tap of E works
            // even though the template's Interact action is set up as a "Hold".
            if (interactAction.WasPressedThisFrame())
            {
                if (heldBody != null)
                    Drop();
                else
                    TryGrab();
            }
            else if (heldBody != null && attackAction.WasPressedThisFrame())
            {
                Throw();
            }
        }

        void FixedUpdate()
        {
            if (heldBody == null)
                return;

            Vector3 holdPoint = cameraTransform.position + cameraTransform.forward * holdDistance;
            Vector3 toHoldPoint = holdPoint - heldBody.position;

            if (toHoldPoint.magnitude > autoDropDistance)
            {
                Drop();
                return;
            }

            heldBody.linearVelocity = Vector3.ClampMagnitude(toHoldPoint * followStrength, maxHoldSpeed);
            heldBody.angularVelocity *= 0.8f; // calm spinning while carried
        }

        void OnDisable()
        {
            if (heldBody != null)
                Drop();
        }

        void TryGrab()
        {
            Ray aim = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!Physics.SphereCast(aim, grabRadius, out RaycastHit hit, grabRange, ~0, QueryTriggerInteraction.Ignore))
                return;

            Rigidbody body = hit.rigidbody;
            if (body == null || body.isKinematic || body.mass > maxGrabMass)
                return;

            heldBody = body;
            heldColliders = body.GetComponentsInChildren<Collider>();
            heldBodyUsedGravity = body.useGravity;

            // Stop the held object from bumping into (or being pushed by) the player while carried.
            SetCollisionWithPlayer(ignore: true);
            body.useGravity = false;
        }

        void Drop()
        {
            Rigidbody body = Release();
            body.linearVelocity = Vector3.zero; // let it fall gently instead of keeping its carry speed
        }

        void Throw()
        {
            Rigidbody body = Release();
            body.AddForce(cameraTransform.forward * throwImpulse, ForceMode.Impulse);
        }

        // Restores the held object to normal physics and returns it.
        Rigidbody Release()
        {
            Rigidbody body = heldBody;
            SetCollisionWithPlayer(ignore: false);
            body.useGravity = heldBodyUsedGravity;

            heldBody = null;
            heldColliders = null;
            return body;
        }

        void SetCollisionWithPlayer(bool ignore)
        {
            foreach (Collider heldCollider in heldColliders)
            {
                if (heldCollider != null)
                    Physics.IgnoreCollision(heldCollider, controller, ignore);
            }
        }
    }
}
