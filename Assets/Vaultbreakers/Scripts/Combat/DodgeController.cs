using System;
using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// The dodge from COMBAT_POC_PLAN.md section 6, and the movement answer to threats that are not
    /// worth spending a shield on. Displacement is authored rather than input-driven: the motor hands
    /// over horizontal movement for the duration, so a dodge covers exactly the tuned distance whether
    /// the player was sprinting or standing still.
    ///
    /// Collision is not handled here at all. The burst goes through <see cref="CharacterController"/>,
    /// which is what makes "a dodge never crosses an arena wall" true by construction rather than by a
    /// distance check that a corner could defeat.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(PlayerMotor.MotorExecutionOrder + 5)]
    public sealed class DodgeController : MonoBehaviour
    {
        /// <summary>
        /// Half a millisecond of slack, for the same reason melee and ranged carry it: summing
        /// per-frame deltas leaves the remainder a hair either side of zero, and a dodge must not be
        /// refused on the exact frame the authored cooldown says it is ready.
        /// </summary>
        private const float CooldownEpsilon = 0.0005f;

        [SerializeField] private PrototypeBalance balance;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerActionCoordinator actions;
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Health health;
        [SerializeField] private MeleeController melee;

        [Header("Fallback tuning (overwritten by the balance asset when one is assigned)")]
        [SerializeField, Min(0f)] private float distance = 3f;
        [SerializeField, Min(0.01f)] private float duration = 0.2f;
        [SerializeField, Min(0f)] private float cooldown = 1f;
        [SerializeField, Min(0f)] private float invulnerability = 0.2f;
        [SerializeField, Range(0f, 0.4f)] private float inputBuffer = 0.15f;

        private CharacterController controller;
        private Health subscribedHealth;
        private Vector3 direction = Vector3.forward;
        private float elapsed;
        private float scheduledDistance;
        private float cooldownRemaining;
        private float bufferedDodge;
        private bool ownsInvulnerability;
        private bool restoreInvulnerable;

        public bool IsDodging { get; private set; }

        /// <summary>The direction committed to when the dodge was accepted. It never turns mid-dodge.</summary>
        public Vector3 DodgeDirection => direction;

        public float CooldownRemaining => Mathf.Max(0f, cooldownRemaining);

        /// <summary>True when a press made this frame would produce a dodge.</summary>
        public bool IsReady => !IsDodging && cooldownRemaining <= CooldownEpsilon && !IsMeleeCommitted;

        /// <summary>How far through the burst the dodge is, 0 to 1. Presentation reads the authored
        /// timing from here rather than keeping a second set of timers that could drift.</summary>
        public float Progress => IsDodging && duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;

        /// <summary>
        /// True only while this component is the reason the player is invulnerable. Distinguished from
        /// <see cref="Health.IsInvulnerable"/> so a debug toggle or a future hit-reaction can hold the
        /// same flag without either of them clearing the other's window.
        /// </summary>
        public bool IsInvulnerableFromDodge => ownsInvulnerability;

        /// <summary>Ground distance actually covered by the dodge in progress, or by the last one.
        /// Less than the authored distance whenever geometry stopped it short.</summary>
        public float DistanceTravelled { get; private set; }

        public float DodgeDistance => distance;
        public float DodgeDuration => duration;

        /// <summary>Raised when a dodge is accepted, with the direction it committed to.</summary>
        public event Action<Vector3> DodgeStarted;

        /// <summary>Raised when the burst finishes, is cancelled, or is ended by death.</summary>
        public event Action DodgeEnded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            ResolveReferences();
            ApplyBalance();
            Subscribe();
        }

        private void OnDestroy() => Unsubscribe();

        /// <summary>A disabled dodge must never leave the player invulnerable or unable to move.</summary>
        private void OnDisable() => EndDodge();

        private void Update()
        {
            if (Time.timeScale <= 0) return;
            if (input != null && input.DodgePressedThisFrame)
            {
                BufferDodge();
            }

            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Queues a dodge. A press made during the melee active window or during the tail of the
        /// cooldown fires on the first legal frame instead of being dropped, which is the project-wide
        /// responsiveness rule in COMBAT_POC_PLAN.md section 4 rather than anything specific to dodge.
        /// The buffer cannot beat the cooldown: it only decides whether an early press survives.
        /// </summary>
        public void BufferDodge() => bufferedDodge = Mathf.Max(bufferedDodge, inputBuffer);

        /// <summary>
        /// One frame of the dodge, exposed so the timing rules can be driven at an exact delta rather
        /// than inferred from whatever the editor happened to render at.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= deltaTime;
            }

            // The coordinator is the arbiter. Nothing outranks a dodge today, so in practice this is
            // death and an explicit state reset - but the rule belongs here rather than in a list of
            // the specific things that are currently allowed to interrupt.
            if (IsDodging && actions != null && (actions.State & PlayerActionState.Dodging) == 0)
            {
                EndDodge();
            }

            AdvanceDodge(deltaTime);

            // Consumed after the burst has advanced, so a press queued mid-dodge lands on the very
            // first frame the next one is legal.
            if (bufferedDodge <= 0f)
            {
                return;
            }

            if (TryStartDodge())
            {
                bufferedDodge = 0f;
                return;
            }

            bufferedDodge = Mathf.Max(0f, bufferedDodge - deltaTime);
        }

        /// <summary>
        /// Entry point for both the input path and the automated tests. Returns false while the
        /// cooldown is running, while a dodge is already in flight, during the melee active window, or
        /// when the coordinator refuses, so neither spam nor a held button can produce more dodges
        /// than the cadence allows.
        /// </summary>
        public bool TryStartDodge()
        {
            if (!IsReady)
            {
                return false;
            }

            if (actions != null && !actions.TryStart(PlayerAction.Dodge))
            {
                return false;
            }

            direction = ResolveDodgeDirection();
            elapsed = 0f;
            scheduledDistance = 0f;
            DistanceTravelled = 0f;
            cooldownRemaining = cooldown;
            IsDodging = true;

            GrantInvulnerability();
            if (motor != null)
            {
                motor.SetMovementSuspended(true);
            }

            DodgeStarted?.Invoke(direction);

            // A zero-length dodge must resolve on the frame the input was accepted rather than one
            // frame later, for the same reason a zero-length melee startup does.
            AdvanceDodge(0f);
            return true;
        }

        /// <summary>
        /// Ends the burst early and keeps the cooldown. Spending the cooldown on a dodge that was cut
        /// short is deliberate: refunding it would make cancelling into another dodge free.
        /// </summary>
        public void CancelDodge() => EndDodge();

        public void Configure(
            PrototypeBalance prototypeBalance,
            PlayerInputReader reader,
            PlayerActionCoordinator coordinator,
            PlayerFacing playerFacing,
            PlayerMotor playerMotor,
            Health playerHealth,
            MeleeController meleeController)
        {
            balance = prototypeBalance;
            input = reader;
            actions = coordinator;
            facing = playerFacing;
            motor = playerMotor;
            health = playerHealth;
            melee = meleeController;
            ApplyBalance();
            Subscribe();
        }

        /// <summary>
        /// Copies the central balance values over the serialized fallbacks, which exist so the
        /// component still behaves sensibly in a bare test rig with no asset assigned, never as a
        /// second source of truth once one is.
        /// </summary>
        public void ApplyBalance()
        {
            if (balance == null)
            {
                return;
            }

            distance = balance.DodgeDistance;
            duration = balance.DodgeDuration;
            cooldown = balance.DodgeCooldown;
            invulnerability = balance.DodgeInvulnerability;
            inputBuffer = balance.DodgeInputBuffer;
        }

        /// <summary>
        /// The plan's default for Phase 8 task 6: a dodge does not cancel a swing that is already
        /// dealing damage. Startup and recovery are both interruptible, so this costs at most the
        /// authored active window - and the press is buffered across it rather than eaten.
        /// </summary>
        private bool IsMeleeCommitted => melee != null && melee.Phase == MeleePhase.Active;

        /// <summary>
        /// Active movement input decides the direction; with the stick at rest the player dodges the
        /// way they are already looking. Read from the motor rather than the reader so it is the same
        /// camera-relative direction locomotion would have used this frame.
        /// </summary>
        private Vector3 ResolveDodgeDirection()
        {
            var intent = motor != null ? motor.MoveDirection : Vector3.zero;
            intent.y = 0f;
            if (intent.sqrMagnitude > 0.0001f)
            {
                return intent.normalized;
            }

            var fallback = facing != null ? facing.LastCombatFacingDirection : transform.forward;
            fallback.y = 0f;
            if (fallback.sqrMagnitude <= 0.0001f)
            {
                fallback = transform.forward;
                fallback.y = 0f;
            }

            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }

        /// <summary>
        /// Moves the burst along its curve. The curve is a schedule of total distance rather than a
        /// per-frame speed, so the dodge covers exactly the authored distance at any frame rate, and a
        /// long frame cannot overshoot it.
        ///
        /// Combat facing is deliberately not forced to the dodge direction: rolling away from a threat
        /// while still aiming at it is the point of having independent aim, and overriding facing here
        /// would take that away.
        /// </summary>
        private void AdvanceDodge(float deltaTime)
        {
            if (!IsDodging)
            {
                return;
            }

            elapsed += deltaTime;

            if (ownsInvulnerability && elapsed >= invulnerability)
            {
                ReleaseInvulnerability();
            }

            var normalized = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            var target = distance * DisplacementCurve(normalized);
            var step = target - scheduledDistance;
            scheduledDistance = target;

            if (step > 0f)
            {
                Displace(step);
            }

            if (elapsed >= duration)
            {
                EndDodge();
            }
        }

        /// <summary>
        /// Through the character controller when there is one, so walls, corners, and other bodies
        /// stop the burst without any of this code knowing they exist. The transform fallback is only
        /// for a bare rig with no controller, where there is nothing to collide with anyway.
        /// </summary>
        private void Displace(float step)
        {
            var before = transform.position;

            if (controller != null && controller.enabled)
            {
                controller.Move(direction * step);
            }
            else
            {
                transform.position = before + direction * step;
            }

            // Measured rather than assumed: what the schedule asked for and what geometry allowed are
            // different numbers the moment there is a wall in the way.
            var moved = transform.position - before;
            moved.y = 0f;
            DistanceTravelled += moved.magnitude;
        }

        /// <summary>
        /// Fraction of the total distance covered by fraction <paramref name="t"/> of the duration.
        /// Quadratic ease-out, so the burst is fastest on the frame the button is pressed and settles
        /// into the player's own movement rather than stopping dead.
        /// </summary>
        public static float DisplacementCurve(float t)
        {
            var clamped = Mathf.Clamp01(t);
            var remaining = 1f - clamped;
            return 1f - remaining * remaining;
        }

        private void EndDodge()
        {
            if (!IsDodging)
            {
                ReleaseInvulnerability();
                return;
            }

            IsDodging = false;
            elapsed = 0f;
            scheduledDistance = 0f;

            ReleaseInvulnerability();

            // Explicit null checks rather than ?.: the null-conditional operator skips Unity's
            // lifetime check, so a destroyed motor or coordinator would read as alive and throw.
            if (motor != null)
            {
                motor.SetMovementSuspended(false);
            }

            if (actions != null)
            {
                actions.Stop(PlayerAction.Dodge);
            }

            DodgeEnded?.Invoke();
        }

        /// <summary>
        /// Takes the invulnerability window, remembering whatever was there before. Restoring rather
        /// than clearing means the debug panel's own invulnerability toggle, and anything else that
        /// later holds the flag, survives a dodge instead of being silently switched off by it.
        /// </summary>
        private void GrantInvulnerability()
        {
            if (health == null || ownsInvulnerability || invulnerability <= 0f)
            {
                return;
            }

            restoreInvulnerable = health.IsInvulnerable;
            ownsInvulnerability = true;
            health.SetInvulnerable(true);
        }

        private void ReleaseInvulnerability()
        {
            if (!ownsInvulnerability)
            {
                return;
            }

            ownsInvulnerability = false;
            if (health != null)
            {
                health.SetInvulnerable(restoreInvulnerable);
            }
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
        }

        private void Unsubscribe()
        {
            if (subscribedHealth != null)
            {
                subscribedHealth.Died -= OnDied;
                subscribedHealth.ResetPerformed -= OnHealthReset;
            }

            subscribedHealth = null;
        }

        /// <summary>Death ends the burst and clears the queue, so a press made as the player died
        /// cannot fire the instant they are revived.</summary>
        private void OnDied(DamageInfo damage) => ResetDodge();

        /// <summary>
        /// Part of the reset contract Phase 10 relies on: a revived player is not mid-dodge, is not
        /// still invulnerable from one, and does not owe a cooldown from their last life.
        /// </summary>
        private void OnHealthReset() => ResetDodge();

        public void ResetDodge()
        {
            EndDodge();
            cooldownRemaining = 0f;
            bufferedDodge = 0f;
            DistanceTravelled = 0f;
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

            if (motor == null)
            {
                motor = GetComponent<PlayerMotor>();
            }

            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (melee == null)
            {
                melee = GetComponent<MeleeController>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            var origin = transform.position + Vector3.up * 0.2f;
            var shown = Application.isPlaying && IsDodging ? direction : transform.forward;
            shown.y = 0f;
            if (shown.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Gizmos.color = IsDodging ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            Gizmos.DrawLine(origin, origin + shown.normalized * distance);
            Gizmos.DrawWireSphere(origin + shown.normalized * distance, 0.15f);
        }
    }
}
