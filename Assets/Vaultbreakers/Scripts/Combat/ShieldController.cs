using System;
using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Combat
{
    public enum ShieldState
    {
        /// <summary>Down and regenerating quickly. Can be raised.</summary>
        Lowered = 0,

        /// <summary>Up, blocking its arc, regenerating slowly, and slowing movement.</summary>
        Raised = 1,

        /// <summary>
        /// Stability hit zero. Locked out and unusable, but already refilling, so the shield comes
        /// back with a real charge the moment the lockout ends.
        /// </summary>
        Broken = 2,

        /// <summary>Down, usable, and still short of full. The tail of a break or of ordinary chip damage.</summary>
        Recovering = 3
    }

    /// <summary>
    /// The directional shield from COMBAT_POC_PLAN.md section 6, and the reason ranged fire is a
    /// choice rather than a default. Movement stays independent while it is up; shield facing starts
    /// from combat facing and afterwards answers only to deliberate aim, so the player can back away
    /// from one threat while still covering it.
    ///
    /// Blocking works through <see cref="IDamageMitigator"/> rather than by intercepting attacks, so
    /// every attack in the game keeps calling <see cref="IDamageable.ReceiveDamage"/> and none of them
    /// need to know a shield exists.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(PlayerMotor.MotorExecutionOrder + 4)]
    public sealed class ShieldController : MonoBehaviour, IDamageMitigator
    {
        [SerializeField] private PrototypeBalance balance;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerActionCoordinator actions;
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Health health;
        [SerializeField] private Transform cameraTransform;

        [Header("Fallback tuning (overwritten by the balance asset when one is assigned)")]
        [SerializeField, Min(1f)] private float maximumStability = 100f;
        [SerializeField, Range(0f, 360f)] private float arcDegrees = 120f;
        [SerializeField, Min(0f)] private float lightStabilityDamage = 10f;
        [SerializeField, Min(0f)] private float loweredRegeneration = 12f;
        [SerializeField, Min(0f)] private float raisedRegeneration = 4f;
        [SerializeField, Min(0f)] private float breakLockout = 2.5f;
        [SerializeField, Range(0f, 1f)] private float moveMultiplier = 0.85f;
        [SerializeField, Range(0f, 1f)] private float aimDeadZone = 0.25f;

        private Health subscribedHealth;
        private float lockoutRemaining;

        public ShieldState State { get; private set; } = ShieldState.Lowered;
        public float Stability { get; private set; } = 100f;
        public float MaximumStability => maximumStability;
        public float StabilityFraction => Mathf.Clamp01(Stability / Mathf.Max(1f, maximumStability));
        public float ArcDegrees => arcDegrees;
        public Vector3 ShieldFacing { get; private set; } = Vector3.forward;
        public bool IsRaised => State == ShieldState.Raised;

        /// <summary>
        /// True only while the shield is broken. Recovering is a usable state: coming back at a
        /// partial charge and having to spend it carefully is the interesting version of a break,
        /// where being locked out until full is just waiting.
        /// </summary>
        public bool IsUnavailable => State == ShieldState.Broken;

        /// <summary>
        /// How far through the lockout a broken shield is, 0 to 1. This is the whole wait, so
        /// presentation can show one bar that means exactly "this is when you get it back".
        /// </summary>
        public float RecoveryProgress => State == ShieldState.Broken && breakLockout > 0f
            ? 1f - Mathf.Clamp01(lockoutRemaining / breakLockout)
            : 1f;

        /// <summary>The last hit this shield absorbed, and the stability left afterwards.</summary>
        public event Action<DamageInfo, float> Blocked;

        public event Action<ShieldState> StateChanged;

        private void Awake()
        {
            ResolveReferences();
            ApplyBalance();
            Stability = maximumStability;
            Subscribe();
        }

        private void OnDestroy() => Unsubscribe();

        private void OnDisable() => Lower();

        private void Update() => Tick(Time.deltaTime, input != null && input.ShieldHeld, input != null ? input.Aim : Vector2.zero);

        /// <summary>
        /// One frame of the shield. Held state and aim are parameters rather than reads from the
        /// input reader so the state machine can be driven without a device.
        /// </summary>
        public void Tick(float deltaTime, bool shieldHeld, Vector2 aim)
        {
            AdvanceRecovery(deltaTime);

            if (State == ShieldState.Raised && actions != null && !actions.State.HasFlag(PlayerActionState.Shielding))
            {
                // Melee, dodge, or death took the claim. The shield comes down on the same frame.
                Lower();
            }

            if (shieldHeld && !IsUnavailable)
            {
                if (State != ShieldState.Raised)
                {
                    TryRaise();
                }
            }
            else if (State == ShieldState.Raised)
            {
                Lower();
            }

            if (State == ShieldState.Raised)
            {
                UpdateShieldFacing(aim);
                Regenerate(raisedRegeneration, deltaTime);
                if (facing != null)
                {
                    facing.ApplyAttackFacing(ShieldFacing);
                }
            }
            else if (State != ShieldState.Broken)
            {
                Regenerate(loweredRegeneration, deltaTime);
                SettleLoweredState();
            }
        }

        /// <summary>
        /// The mitigator contract. Only a raised shield facing the hit absorbs it; everything else
        /// falls through to health untouched.
        /// </summary>
        public bool TryAbsorb(in DamageInfo damage)
        {
            if (State != ShieldState.Raised ||
                !ShieldArc.CanBlock(damage, ShieldFacing, transform.position, arcDegrees))
            {
                return false;
            }

            var cost = damage.StabilityDamage > 0f ? damage.StabilityDamage : lightStabilityDamage;
            Stability = Mathf.Max(0f, Stability - cost);
            Blocked?.Invoke(damage, Stability);

            // A hit is blocked in full even when there was not enough stability left to pay for it.
            // The cost of coming up short is the break, not leaked damage.
            if (Stability <= 0f)
            {
                Break();
            }

            return true;
        }

        /// <summary>Forces a break, for the debug panel and for enemy attacks that shatter a guard.</summary>
        public void Break()
        {
            Stability = 0f;
            lockoutRemaining = breakLockout;
            ReleaseHold();
            SetState(ShieldState.Broken);
            SetBrokenFlag(true);
        }

        /// <summary>Returns the shield to a full, lowered, usable state.</summary>
        public void ResetShield()
        {
            Stability = maximumStability;
            lockoutRemaining = 0f;
            ReleaseHold();
            SetBrokenFlag(false);
            SetState(ShieldState.Lowered);
        }

        public void Configure(
            PrototypeBalance prototypeBalance,
            PlayerInputReader reader,
            PlayerActionCoordinator coordinator,
            PlayerFacing playerFacing,
            PlayerMotor playerMotor,
            Health playerHealth,
            Transform movementCamera)
        {
            balance = prototypeBalance;
            input = reader;
            actions = coordinator;
            facing = playerFacing;
            motor = playerMotor;
            health = playerHealth;
            cameraTransform = movementCamera;
            ApplyBalance();
            Stability = maximumStability;
            Subscribe();
        }

        /// <summary>
        /// Copies the central balance values over the serialized fallbacks, which exist only so the
        /// component still behaves sensibly in a bare test rig with no asset assigned.
        /// </summary>
        public void ApplyBalance()
        {
            if (balance == null)
            {
                return;
            }

            maximumStability = balance.ShieldStability;
            arcDegrees = balance.ShieldArcDegrees;
            lightStabilityDamage = balance.LightStabilityDamage;
            loweredRegeneration = balance.LoweredStabilityRegeneration;
            raisedRegeneration = balance.RaisedStabilityRegeneration;
            breakLockout = balance.ShieldBreakLockout;
            moveMultiplier = balance.ShieldMoveMultiplier;
            aimDeadZone = balance.AimDeadZone;
        }

        private void TryRaise()
        {
            if (actions != null && !actions.TryStart(PlayerAction.Shield))
            {
                return;
            }

            // Shield facing starts from wherever the player is already looking, then answers only to
            // deliberate aim. Movement is free to go somewhere else entirely from this moment on.
            var start = facing != null ? facing.LastCombatFacingDirection : transform.forward;
            start.y = 0f;
            ShieldFacing = start.sqrMagnitude > 0.0001f ? start.normalized : Vector3.forward;

            if (motor != null)
            {
                motor.SetSpeedMultiplier(moveMultiplier);
            }

            SetState(ShieldState.Raised);
        }

        private void Lower()
        {
            if (State != ShieldState.Raised)
            {
                return;
            }

            ReleaseHold();
            SettleLoweredState();
        }

        /// <summary>
        /// Gives back the action claim and the movement penalty, whatever ended the hold. Explicit
        /// null checks rather than <c>?.</c> throughout this component: the null-conditional operator
        /// skips Unity's lifetime check, so a destroyed motor would read as alive and throw here.
        /// </summary>
        private void ReleaseHold()
        {
            if (motor != null)
            {
                motor.SetSpeedMultiplier(1f);
            }

            if (actions != null)
            {
                actions.Stop(PlayerAction.Shield);
            }
        }

        private void SetBrokenFlag(bool broken)
        {
            if (actions != null)
            {
                actions.SetShieldBroken(broken);
            }
        }

        /// <summary>
        /// A broken shield refills while it is locked out, so when the wait ends it is immediately
        /// worth something. The player is out of the fight for exactly the lockout and no longer.
        /// </summary>
        private void AdvanceRecovery(float deltaTime)
        {
            if (State != ShieldState.Broken)
            {
                return;
            }

            Regenerate(loweredRegeneration, deltaTime);
            lockoutRemaining -= deltaTime;

            if (lockoutRemaining > 0f)
            {
                return;
            }

            lockoutRemaining = 0f;
            SetBrokenFlag(false);
            SettleLoweredState();
        }

        /// <summary>
        /// Down and whole is Lowered; down and short of full is Recovering. Both are usable, so the
        /// distinction is only there for feedback and for the HUD that arrives in Phase 11.
        /// </summary>
        private void SettleLoweredState() =>
            SetState(Stability >= maximumStability ? ShieldState.Lowered : ShieldState.Recovering);

        private void Regenerate(float rate, float deltaTime) =>
            Stability = Mathf.Min(maximumStability, Stability + rate * deltaTime);

        /// <summary>
        /// Deliberate aim is the only thing that turns a raised shield. Movement must not, or the
        /// player could not retreat from the threat they are covering.
        /// </summary>
        private void UpdateShieldFacing(Vector2 aim)
        {
            if (aim.magnitude < aimDeadZone)
            {
                return;
            }

            var aimed = PlayerMotor.CameraRelativeDirection(aim, cameraTransform);
            if (aimed.sqrMagnitude > 0.0001f)
            {
                ShieldFacing = aimed.normalized;
            }
        }

        private void SetState(ShieldState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }

        private void Subscribe()
        {
            Unsubscribe();
            subscribedHealth = health;
            if (subscribedHealth == null)
            {
                return;
            }

            subscribedHealth.Died += OnDied;
            subscribedHealth.ResetPerformed += OnHealthReset;
            subscribedHealth.SetMitigator(this);
        }

        private void Unsubscribe()
        {
            if (subscribedHealth != null)
            {
                subscribedHealth.Died -= OnDied;
                subscribedHealth.ResetPerformed -= OnHealthReset;
                subscribedHealth.SetMitigator(null);
            }

            subscribedHealth = null;
        }

        /// <summary>
        /// Death drops a raised shield but does not repair a broken one. Reviving is what restores it,
        /// so dying mid-lockout cannot be used to skip the cost of a break.
        /// </summary>
        private void OnDied(DamageInfo damage) => Lower();

        /// <summary>A revived player gets a whole shield; a broken one would be a hidden penalty.</summary>
        private void OnHealthReset() => ResetShield();

        private void ResolveReferences()
        {
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            if (actions == null)
            {
                actions = GetComponent<PlayerActionCoordinator>();
            }

            if (facing == null)
            {
                facing = GetComponent<PlayerFacing>();
            }

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            var origin = transform.position + Vector3.up * 1f;
            var direction = Application.isPlaying ? ShieldFacing : transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            direction.Normalize();
            var half = arcDegrees * 0.5f;

            Gizmos.color = IsRaised ? Color.cyan : new Color(0f, 0.5f, 0.6f, 0.4f);
            Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(-half, Vector3.up) * direction * 2f);
            Gizmos.DrawLine(origin, origin + Quaternion.AngleAxis(half, Vector3.up) * direction * 2f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(origin, origin + direction * 2.2f);
        }
    }
}
