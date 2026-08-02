using System;
using System.Collections.Generic;
using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Combat
{
    public enum MeleePhase
    {
        Ready = 0,
        Startup = 1,
        Active = 2,
        Recovery = 3
    }

    /// <summary>
    /// The melee vertical slice from COMBAT_POC_PLAN.md section 6. Timing is gameplay-authoritative:
    /// the hit resolves the moment the active window opens, never when an animation happens to end,
    /// and presentation listens to the phase rather than driving it.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(PlayerMotor.MotorExecutionOrder + 2)]
    public sealed class MeleeController : MonoBehaviour
    {
        /// <summary>Enough for every enemy that can physically stand inside one swing volume.</summary>
        private const int MaximumHitsPerQuery = 16;

        /// <summary>
        /// Half a millisecond of slack on the cooldown. Summing per-frame deltas leaves the remainder
        /// a hair either side of zero, and a swing must not be refused on the exact frame the authored
        /// cadence says it is ready.
        /// </summary>
        private const float CooldownEpsilon = 0.0005f;

        [SerializeField] private PrototypeBalance balance;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerActionCoordinator actions;
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private Health health;
        [SerializeField] private HitStop hitStop;

        [Header("Fallback tuning (overwritten by the balance asset when one is assigned)")]
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float cooldown = 0.4f;
        [SerializeField, Min(0f)] private float range = 2.5f;
        [SerializeField, Min(0f)] private float radius = 1.5f;
        [SerializeField, Min(0f)] private float attackHeight = 1f;
        [SerializeField, Range(0f, 45f)] private float targetCorrection = 15f;
        [SerializeField, Min(0f)] private float knockback = 4f;
        [SerializeField, Min(0f)] private float startupDuration = 0.06f;
        [SerializeField, Min(0f)] private float activeDuration = 0.08f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.16f;
        [SerializeField, Range(0f, 0.2f)] private float hitStopDuration = 0.05f;

        private readonly Collider[] queryBuffer = new Collider[MaximumHitsPerQuery];

        /// <summary>
        /// Keyed on the receiver rather than the collider, so a body wearing several colliders is
        /// still damaged once per swing.
        /// </summary>
        private readonly HashSet<Health> hitTargets = new();

        private Health subscribedHealth;
        private float phaseTimer;
        private float phaseDuration;
        private float cooldownRemaining;
        private Vector3 swingDirection = Vector3.forward;

        public MeleePhase Phase { get; private set; } = MeleePhase.Ready;
        public Vector3 SwingDirection => swingDirection;
        public float CooldownRemaining => Mathf.Max(0f, cooldownRemaining);
        public bool IsReady => Phase == MeleePhase.Ready && cooldownRemaining <= CooldownEpsilon;

        /// <summary>How far the current phase has run, 0 to 1. Presentation reads the authored timing
        /// from here instead of keeping a second set of timers that could drift from gameplay.</summary>
        public float PhaseProgress => phaseDuration > 0f
            ? Mathf.Clamp01(1f - phaseTimer / phaseDuration)
            : 1f;

        /// <summary>Targets damaged by the swing currently running, or by the last one to finish.</summary>
        public int HitsThisSwing => hitTargets.Count;

        public float Range => range;
        public float Radius => radius;
        public float TargetCorrectionDegrees => targetCorrection;

        /// <summary>Raised when a swing is accepted, with the direction it committed to.</summary>
        public event Action<Vector3> SwingStarted;

        /// <summary>Raised once per phase transition, including the return to <see cref="MeleePhase.Ready"/>.</summary>
        public event Action<MeleePhase> PhaseChanged;

        /// <summary>Raised once per target per swing, after the damage has been applied.</summary>
        public event Action<Health, DamageResult> Hit;

        private void Awake()
        {
            ResolveReferences();
            ApplyBalance();
            Subscribe();
        }

        private void OnDestroy() => Unsubscribe();

        private void Update() => Tick(Time.deltaTime);

        /// <summary>
        /// One frame of the swing state machine, exposed so the locked timing rules can be driven at
        /// an exact delta by the tests instead of being inferred from whatever the editor rendered at.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= deltaTime;
            }

            // The coordinator is the arbiter, not this controller. If something with higher priority
            // took the attack away - a dodge, a stun, death - the swing must stop dealing damage on
            // the same frame rather than finishing its authored window unopposed.
            if (Phase != MeleePhase.Ready && actions != null && !actions.IsMeleeActive)
            {
                CancelSwing();
            }

            AdvancePhase(deltaTime);

            if (input != null && input.MeleePressedThisFrame)
            {
                TryStartSwing();
            }
        }

        /// <summary>
        /// Entry point for both the input path and the automated tests. Returns false when the
        /// cooldown has not elapsed or the action coordinator refuses the attack, so holding or
        /// spamming the button cannot produce more swings than the cadence allows.
        /// </summary>
        public bool TryStartSwing()
        {
            if (!IsReady)
            {
                return false;
            }

            if (actions != null && !actions.TryStart(PlayerAction.Melee))
            {
                return false;
            }

            swingDirection = ResolveSwingDirection();
            hitTargets.Clear();
            cooldownRemaining = cooldown;
            SetPhase(MeleePhase.Startup, startupDuration);
            SwingStarted?.Invoke(swingDirection);

            // A zero-length startup must land its hit on the frame the input was accepted rather
            // than one frame later, so the phase machine is given a chance to settle immediately.
            AdvancePhase(0f);
            return true;
        }

        /// <summary>Ends the swing without dealing damage and releases the action claim.</summary>
        public void CancelSwing()
        {
            if (Phase == MeleePhase.Ready)
            {
                return;
            }

            hitTargets.Clear();
            EndSwing();
        }

        public void Configure(
            PrototypeBalance prototypeBalance,
            PlayerInputReader reader,
            PlayerActionCoordinator coordinator,
            PlayerFacing playerFacing,
            Health playerHealth,
            HitStop feedback)
        {
            balance = prototypeBalance;
            input = reader;
            actions = coordinator;
            facing = playerFacing;
            health = playerHealth;
            hitStop = feedback;
            ApplyBalance();
            Subscribe();
        }

        /// <summary>
        /// Copies the central balance values over the serialized fallbacks. The fallbacks exist so the
        /// component is usable in a test or a bare scene without an asset, never as a second source
        /// of truth once one is assigned.
        /// </summary>
        public void ApplyBalance()
        {
            if (balance == null)
            {
                return;
            }

            damage = balance.MeleeDamage;
            cooldown = balance.MeleeCooldown;
            range = balance.MeleeRange;
            radius = balance.MeleeRadius;
            attackHeight = balance.MeleeAttackHeight;
            targetCorrection = balance.MeleeTargetCorrection;
            knockback = balance.MeleeKnockback;
            startupDuration = balance.MeleeStartupDuration;
            activeDuration = balance.MeleeActiveDuration;
            recoveryDuration = balance.MeleeRecoveryDuration;
            hitStopDuration = balance.HitStopDuration;
        }

        private void AdvancePhase(float deltaTime)
        {
            if (Phase == MeleePhase.Ready)
            {
                return;
            }

            phaseTimer -= deltaTime;
            var resolvedThisFrame = false;

            while (Phase != MeleePhase.Ready && phaseTimer <= 0f)
            {
                // Carrying the overshoot forward keeps a long frame from stretching the swing.
                var carry = phaseTimer;
                switch (Phase)
                {
                    case MeleePhase.Startup:
                        SetPhase(MeleePhase.Active, activeDuration + carry);
                        ResolveHits();
                        resolvedThisFrame = true;
                        break;
                    case MeleePhase.Active:
                        SetPhase(MeleePhase.Recovery, recoveryDuration + carry);
                        break;
                    default:
                        EndSwing();
                        break;
                }
            }

            if (Phase == MeleePhase.Active && !resolvedThisFrame)
            {
                ResolveHits();
            }
        }

        /// <summary>
        /// One non-allocating overlap against the enemy layer, deduplicated per swing so a target
        /// standing inside the volume for the whole active window is still damaged exactly once.
        /// </summary>
        private void ResolveHits()
        {
            var origin = AttackOrigin;
            var centre = MeleeSwing.QueryCentre(origin, swingDirection, range, radius);
            var count = Physics.OverlapSphereNonAlloc(
                centre, radius, queryBuffer, GameLayers.EnemyTargets, QueryTriggerInteraction.Collide);

            var landed = false;
            for (var index = 0; index < count; index++)
            {
                var candidate = queryBuffer[index];
                if (candidate == null)
                {
                    continue;
                }

                var target = candidate.GetComponentInParent<Health>();
                if (target == null || target.IsDead || !hitTargets.Add(target))
                {
                    continue;
                }

                var push = MeleeSwing.KnockbackDirection(origin, target.transform.position, swingDirection) * knockback;
                var result = target.ReceiveDamage(new DamageInfo(damage, origin, gameObject, push));
                if (!result.WasApplied)
                {
                    continue;
                }

                landed = true;
                Hit?.Invoke(target, result);
            }

            if (landed && hitStop != null)
            {
                hitStop.Request(hitStopDuration);
            }
        }

        /// <summary>
        /// Combat facing, nudged toward a target that is already inside the correction cone. The
        /// player is never moved toward the target; only the direction of the strike changes.
        /// </summary>
        private Vector3 ResolveSwingDirection()
        {
            var direction = facing != null ? facing.LastCombatFacingDirection : transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
                direction.y = 0f;
            }

            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;

            var origin = AttackOrigin;
            if (TryFindCorrectionTarget(direction, origin, out var targetPosition))
            {
                direction = MeleeSwing.ApplyTargetCorrection(direction, origin, targetPosition, targetCorrection);
            }

            if (facing != null)
            {
                facing.ApplyAttackFacing(direction);
            }

            return direction;
        }

        /// <summary>Picks the best-aligned living enemy inside the cone, so the nudge is predictable.</summary>
        private bool TryFindCorrectionTarget(Vector3 direction, Vector3 origin, out Vector3 targetPosition)
        {
            targetPosition = default;
            if (targetCorrection <= 0f)
            {
                return false;
            }

            var count = Physics.OverlapSphereNonAlloc(
                origin, range, queryBuffer, GameLayers.EnemyTargets, QueryTriggerInteraction.Collide);

            var bestAngle = float.MaxValue;
            var found = false;

            for (var index = 0; index < count; index++)
            {
                var candidate = queryBuffer[index];
                if (candidate == null)
                {
                    continue;
                }

                var target = candidate.GetComponentInParent<Health>();
                if (target == null || target.IsDead)
                {
                    continue;
                }

                var position = target.transform.position;
                if (!MeleeSwing.IsCorrectionCandidate(direction, origin, position, range, targetCorrection))
                {
                    continue;
                }

                var offset = position - origin;
                offset.y = 0f;
                var angle = Vector3.Angle(direction, offset);
                if (angle >= bestAngle)
                {
                    continue;
                }

                bestAngle = angle;
                targetPosition = position;
                found = true;
            }

            return found;
        }

        private Vector3 AttackOrigin => transform.position + Vector3.up * attackHeight;

        private void SetPhase(MeleePhase phase, float duration)
        {
            Phase = phase;
            phaseTimer = Mathf.Max(0f, duration);
            phaseDuration = phaseTimer;
            PhaseChanged?.Invoke(phase);
        }

        private void EndSwing()
        {
            phaseTimer = 0f;
            phaseDuration = 0f;
            Phase = MeleePhase.Ready;
            actions?.Stop(PlayerAction.Melee);
            PhaseChanged?.Invoke(MeleePhase.Ready);
        }

        private void Subscribe()
        {
            Unsubscribe();
            subscribedHealth = health;
            if (subscribedHealth != null)
            {
                subscribedHealth.Died += OnDied;
            }
        }

        private void Unsubscribe()
        {
            if (subscribedHealth != null)
            {
                subscribedHealth.Died -= OnDied;
            }

            subscribedHealth = null;
        }

        /// <summary>Death cancels the swing in progress and clears the cooldown for the next life.</summary>
        private void OnDied(DamageInfo damageInfo)
        {
            CancelSwing();
            cooldownRemaining = 0f;
        }

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

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (hitStop == null)
            {
                hitStop = GetComponent<HitStop>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            var origin = transform.position + Vector3.up * attackHeight;
            var direction = Application.isPlaying && Phase != MeleePhase.Ready
                ? swingDirection
                : transform.forward;

            Gizmos.color = Phase == MeleePhase.Active ? Color.red : new Color(1f, 0.55f, 0f, 0.6f);
            Gizmos.DrawWireSphere(MeleeSwing.QueryCentre(origin, direction, range, radius), radius);

            Gizmos.color = Color.yellow;
            var left = Quaternion.AngleAxis(-targetCorrection, Vector3.up) * direction;
            var right = Quaternion.AngleAxis(targetCorrection, Vector3.up) * direction;
            Gizmos.DrawLine(origin, origin + left * range);
            Gizmos.DrawLine(origin, origin + right * range);
        }
    }
}
