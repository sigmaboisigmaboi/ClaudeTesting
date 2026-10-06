using TheDeep.NPC;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheDeep.Player
{
    // A simple first-person melee swing (P6): left click (Player/Attack) when empty-handed.
    //   - an NPC: fixed damage (if it has health) plus a small shove (it can shove a Scrapper off a ledge)
    //   - a loose physics object (crate): a mass-aware knock, so crates can be batted into enemies
    //   - anything else, including Destructibles: nothing (melee doesn't break structures in P6)
    // Runs before PlayerGrabThrow, so a click while holding something is still a throw, never both.
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMelee : MonoBehaviour
    {
        [Tooltip("The player's camera. Found automatically if left empty.")]
        [SerializeField] Transform cameraTransform;
        [Tooltip("How far the swing reaches, in meters.")]
        [SerializeField] float reach = 2.2f;
        [Tooltip("Thickness of the swing, so aiming doesn't need to be exact.")]
        [SerializeField] float swingRadius = 0.3f;
        [Tooltip("Seconds between swings.")]
        [SerializeField] float cooldown = 0.5f;
        [Tooltip("Damage to an NPC that has health.")]
        [SerializeField] float damage = 15f;
        [Tooltip("How hard the swing counts as a hit (like mass x speed) for staggering and shoving NPCs.")]
        [SerializeField] float hitStrength = 10f;
        [Tooltip("Impulse given to a loose physics object that is hit.")]
        [SerializeField] float objectImpulse = 20f;

        const float JabDistance = 0.08f;
        const float JabDuration = 0.12f;

        InputAction attackAction;
        float nextSwingTime;
        float jabUntil = -1f;
        Vector3 cameraRestPosition;

        void Awake()
        {
            attackAction = InputSystem.actions.FindAction("Player/Attack", throwIfNotFound: true);
            if (cameraTransform == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null)
                    cameraTransform = childCamera.transform;
            }
            if (cameraTransform != null)
                cameraRestPosition = cameraTransform.localPosition;
        }

        void OnDisable()
        {
            if (cameraTransform != null)
                cameraTransform.localPosition = cameraRestPosition;
        }

        void Update()
        {
            if (cameraTransform == null || !attackAction.WasPressedThisFrame())
                return;
            if (PlayerGrabThrow.CurrentlyHeld != null) // holding something: this click is a throw
                return;
            if (Time.time < nextSwingTime)
                return;

            nextSwingTime = Time.time + cooldown;
            Swing();
        }

        void LateUpdate()
        {
            // A quick forward jab of the camera so every swing is visible, hit or miss.
            if (cameraTransform != null)
                cameraTransform.localPosition = Time.time < jabUntil ? cameraRestPosition + Vector3.forward * JabDistance : cameraRestPosition;
        }

        void Swing()
        {
            jabUntil = Time.time + JabDuration;

            var aim = new Ray(cameraTransform.position, cameraTransform.forward);
            if (!Physics.SphereCast(aim, swingRadius, out RaycastHit hit, reach, ~0, QueryTriggerInteraction.Ignore))
                return;

            NpcAgent npc = hit.collider.GetComponentInParent<NpcAgent>();
            if (npc != null)
            {
                npc.ReceiveMeleeHit(hitStrength, damage, transform.position);
                return;
            }

            Rigidbody body = hit.rigidbody;
            if (body != null && !body.isKinematic)
                body.AddForceAtPosition(cameraTransform.forward * objectImpulse, hit.point, ForceMode.Impulse);
        }

        // A small aiming dot in the middle of the screen (prototype display).
        void OnGUI()
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 2f, Screen.height * 0.5f - 2f, 4f, 4f), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}
