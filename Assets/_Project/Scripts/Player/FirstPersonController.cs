using UnityEngine;
using UnityEngine.InputSystem;

namespace TheDeep.Player
{
    // First-person movement and mouse look on top of Unity's CharacterController.
    // Input comes from the project-wide Input Actions asset (Player/Move, Player/Look).
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        // Small constant downward speed while grounded, so the controller stays
        // pressed onto the floor and isGrounded stays reliable.
        const float GroundedDownSpeed = -2f;

        [Header("References")]
        [SerializeField] Transform cameraTransform;

        [Header("Movement")]
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float gravity = -20f;

        [Header("Look")]
        [Tooltip("Degrees of rotation per pixel of mouse movement.")]
        [SerializeField] float mouseSensitivity = 0.1f;
        [Tooltip("How far up or down the camera can tilt, in degrees.")]
        [SerializeField] float maxLookAngle = 85f;

        CharacterController controller;
        InputAction moveAction;
        InputAction lookAction;
        float verticalVelocity;
        float pitch;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            moveAction = InputSystem.actions.FindAction("Player/Move", throwIfNotFound: true);
            lookAction = InputSystem.actions.FindAction("Player/Look", throwIfNotFound: true);
        }

        void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            Look();
            Move();
        }

        void Look()
        {
            // Mouse delta is already "movement this frame", so no Time.deltaTime here.
            Vector2 delta = lookAction.ReadValue<Vector2>() * mouseSensitivity;

            // Left/right turns the whole body; up/down tilts only the camera.
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -maxLookAngle, maxLookAngle);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void Move()
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector3 move = transform.right * input.x + transform.forward * input.y;
            move = Vector3.ClampMagnitude(move, 1f);

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = GroundedDownSpeed;
            else
                verticalVelocity += gravity * Time.deltaTime;

            // One Move call per frame keeps collision and isGrounded consistent.
            Vector3 velocity = move * moveSpeed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }
    }
}
