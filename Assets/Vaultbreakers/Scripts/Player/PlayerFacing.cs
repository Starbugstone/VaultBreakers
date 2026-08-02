using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Input;

namespace Vaultbreakers.Player
{
    /// <summary>
    /// Resolves the combat facing direction from the priority rules in COMBAT_POC_PLAN.md section 4.
    /// Gameplay facing is decided in Update, immediately after <see cref="PlayerMotor"/>, so combat
    /// controllers see this frame's direction. Visual rotation is smoothed separately in LateUpdate
    /// and never delays the gameplay value.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(PlayerMotor.MotorExecutionOrder + 1)]
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerFacing : MonoBehaviour
    {
        [SerializeField] private PrototypeBalance balance;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform cameraTransform;

        [Header("Fallback tuning (overwritten by the balance asset when one is assigned)")]
        [SerializeField, Min(0f)] private float visualTurnSpeed = 900f;

        [Tooltip("Aim magnitude required to override movement facing. The input asset applies no aim " +
                 "dead zone, so this is the single documented threshold.")]
        [SerializeField, Range(0f, 1f)] private float aimDeadZone = 0.25f;

        public Vector3 LastCombatFacingDirection { get; private set; } = Vector3.forward;
        public float AimDeadZone => aimDeadZone;

        private void Awake()
        {
            ResolveReferences();
            ApplyBalance();
            if (visualRoot == null)
            {
                return;
            }

            var initial = Vector3.ProjectOnPlane(visualRoot.forward, Vector3.up);
            if (initial.sqrMagnitude > 0.0001f)
            {
                LastCombatFacingDirection = initial.normalized;
            }
        }

        private void Update()
        {
            ResolveReferences();
            if (input == null || motor == null)
            {
                return;
            }

            var aim = input.Aim;
            LastCombatFacingDirection = ResolveFacing(
                LastCombatFacingDirection,
                PlayerMotor.CameraRelativeDirection(aim, cameraTransform),
                aim.magnitude,
                motor.MoveDirection,
                aimDeadZone);
        }

        private void LateUpdate()
        {
            if (visualRoot == null || LastCombatFacingDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            var target = Quaternion.LookRotation(LastCombatFacingDirection, Vector3.up);
            visualRoot.rotation = Quaternion.RotateTowards(
                visualRoot.rotation,
                target,
                visualTurnSpeed * Time.deltaTime);
        }

        public void Configure(
            PlayerInputReader reader,
            PlayerMotor playerMotor,
            Transform model,
            Transform movementCamera,
            PrototypeBalance prototypeBalance = null)
        {
            input = reader;
            motor = playerMotor;
            visualRoot = model;
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

            visualTurnSpeed = balance.VisualTurnSpeed;
            aimDeadZone = balance.AimDeadZone;
        }

        /// <summary>
        /// Priority 3 of the facing rules: a swing that snapped onto a nearby target reports the
        /// corrected direction here so the visual matches the strike. Deliberate aim and active
        /// movement both outrank it again on the next frame.
        /// </summary>
        public void ApplyAttackFacing(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                LastCombatFacingDirection = direction.normalized;
            }
        }

        /// <summary>
        /// Facing priority: aim above the dead zone, then movement, then the last valid direction.
        /// Pure so the locked rules can be tested without a scene.
        /// </summary>
        public static Vector3 ResolveFacing(
            Vector3 previousFacing,
            Vector3 aimDirection,
            float aimMagnitude,
            Vector3 moveDirection,
            float deadZone)
        {
            if (aimMagnitude >= deadZone && aimDirection.sqrMagnitude > 0.0001f)
            {
                return aimDirection.normalized;
            }

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                return moveDirection.normalized;
            }

            var preserved = Vector3.ProjectOnPlane(previousFacing, Vector3.up);
            return preserved.sqrMagnitude > 0.0001f ? preserved.normalized : Vector3.forward;
        }

        private void ResolveReferences()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            var origin = transform.position + Vector3.up * 0.15f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + LastCombatFacingDirection * 2f);

            if (input == null)
            {
                return;
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(origin, origin + PlayerMotor.CameraRelativeDirection(input.Aim, cameraTransform) * 1.6f);
        }
    }
}
