using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Input;

namespace Vaultbreakers.Player
{
    /// <summary>
    /// Camera-relative movement on the XZ plane. Runs before <see cref="PlayerFacing"/> so facing
    /// resolves against the movement direction produced in the same frame.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(MotorExecutionOrder)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public const int MotorExecutionOrder = -20;

        [SerializeField] private PrototypeBalance balance;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform cameraTransform;

        [Header("Fallback tuning (overwritten by the balance asset when one is assigned)")]
        [SerializeField, Min(0f)] private float maximumSpeed = 5f;
        [SerializeField, Min(0f)] private float acceleration = 35f;
        [SerializeField, Min(0f)] private float deceleration = 45f;
        [SerializeField, Min(0f)] private float gravity = 25f;

        /// <summary>Keeps the controller pressed into the floor without accumulating fall speed.</summary>
        private const float GroundedVerticalVelocity = -2f;

        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalVelocity;

        public Vector3 PlanarVelocity => planarVelocity;
        public Vector3 MoveDirection { get; private set; }
        public float MaximumSpeed => maximumSpeed;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            ResolveReferences();
            ApplyBalance();
        }

        private void Update()
        {
            ResolveReferences();
            if (input == null || controller == null)
            {
                return;
            }

            MoveDirection = CameraRelativeDirection(input.Move, cameraTransform);
            var targetVelocity = MoveDirection * maximumSpeed;
            var rate = targetVelocity.sqrMagnitude > 0f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = GroundedVerticalVelocity;
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        public void Configure(PlayerInputReader reader, Transform movementCamera, PrototypeBalance prototypeBalance = null)
        {
            input = reader;
            cameraTransform = movementCamera;
            balance = prototypeBalance;
            ApplyBalance();
        }

        /// <summary>
        /// Copies the central balance values over the serialized fallbacks, which exist only so the
        /// component still behaves sensibly in a bare test scene with no asset assigned.
        /// </summary>
        public void ApplyBalance()
        {
            if (balance == null)
            {
                return;
            }

            maximumSpeed = balance.MoveSpeed;
            acceleration = balance.MoveAcceleration;
            deceleration = balance.MoveDeceleration;
            gravity = balance.Gravity;
        }

        /// <summary>
        /// Projects stick input onto the ground plane using the camera's yaw. Pure and camera-agnostic
        /// so it can be tested without a scene.
        /// </summary>
        public static Vector3 CameraRelativeDirection(Vector2 moveInput, Transform movementCamera)
        {
            var forward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            var right = movementCamera != null ? movementCamera.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
            return Vector3.ClampMagnitude(right * moveInput.x + forward * moveInput.y, 1f);
        }

        private void ResolveReferences()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            var origin = transform.position + Vector3.up * 0.1f;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(origin, origin + MoveDirection * 1.5f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + planarVelocity * 0.25f);
        }
    }
}
