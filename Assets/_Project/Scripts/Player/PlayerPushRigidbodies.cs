using UnityEngine;

namespace TheDeep.Player
{
    // Lets the CharacterController push Rigidbody objects it walks into.
    // A CharacterController treats Rigidbodies like walls by default, so this adds the push.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPushRigidbodies : MonoBehaviour
    {
        [Tooltip("Push force in newtons. The same push moves light objects far more than heavy ones.")]
        [SerializeField] float pushStrength = 300f;

        // Called by Unity whenever the CharacterController touches a collider while moving.
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.rigidbody;
            if (body == null || body.isKinematic)
                return;

            // Ignore contacts from above (standing on an object), so we don't shove it into the floor.
            if (hit.moveDirection.y < -0.3f)
                return;

            Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized;

            // Impulse scaled by frame time = a steady force, independent of frame rate.
            // An impulse respects mass; pushing at the contact point lets objects tip naturally.
            body.AddForceAtPosition(pushDirection * pushStrength * Time.deltaTime, hit.point, ForceMode.Impulse);
        }
    }
}
