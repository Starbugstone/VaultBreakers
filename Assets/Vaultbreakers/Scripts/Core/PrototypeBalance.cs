using UnityEngine;

namespace Vaultbreakers.Core
{
    /// <summary>
    /// Central tuning data for the combat POC, required by COMBAT_POC_PLAN.md section 3 so gameplay
    /// values never scatter into constants across controllers. Configuration only: every runtime
    /// timer, cooldown, and health value lives on the scene or prefab instance that owns it.
    /// Sections are added as their phase lands; nothing here is authored ahead of the code that
    /// reads it.
    /// </summary>
    [CreateAssetMenu(fileName = "PrototypeBalance", menuName = "Vaultbreakers/Prototype Balance")]
    public sealed class PrototypeBalance : ScriptableObject
    {
        [Header("Locomotion (phase 3)")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float moveAcceleration = 35f;
        [SerializeField, Min(0f)] private float moveDeceleration = 45f;
        [SerializeField, Min(0f)] private float gravity = 25f;

        [Tooltip("Aim magnitude required to override movement facing. The input asset applies no aim " +
                 "processor, so this is the single threshold in the project.")]
        [SerializeField, Range(0f, 1f)] private float aimDeadZone = 0.25f;

        [SerializeField, Min(0f)] private float visualTurnSpeed = 900f;

        [Header("Player (phase 4)")]
        [SerializeField, Min(1f)] private float playerMaximumHealth = 100f;

        [Header("Melee (phase 5)")]
        [SerializeField, Min(0f)] private float meleeDamage = 10f;

        [Tooltip("Time from swing start until the next swing may begin. Set equal to the phase " +
                 "durations below so swings chain seamlessly: a cooldown longer than the swing " +
                 "leaves a window where the game is doing nothing and refusing input.")]
        [SerializeField, Min(0f)] private float meleeCooldown = 0.3f;

        [Tooltip("How long a swing press stays queued. A press that lands during recovery fires the " +
                 "moment the swing becomes legal instead of being dropped, which is the difference " +
                 "between mashing that works and mashing that eats inputs.")]
        [SerializeField, Range(0f, 0.4f)] private float meleeInputBuffer = 0.15f;

        [Tooltip("Distance from the attack origin to the far edge of the query volume.")]
        [SerializeField, Min(0f)] private float meleeRange = 2.5f;

        [Tooltip("Radius of the overlap sphere. The sphere centre sits at (range - radius) in front " +
                 "of the attacker, so the far edge lands exactly at the range above.")]
        [SerializeField, Min(0f)] private float meleeRadius = 1.5f;

        [Tooltip("Height above the player's feet at which the query is centred.")]
        [SerializeField, Min(0f)] private float meleeAttackHeight = 1f;

        [Tooltip("Half-angle of the correction cone. A target inside it snaps the swing; a target " +
                 "outside it is ignored, so the correction can never exceed this many degrees.")]
        [SerializeField, Range(0f, 45f)] private float meleeTargetCorrection = 15f;

        [SerializeField, Min(0f)] private float meleeKnockback = 4f;
        [SerializeField, Min(0f)] private float meleeStartupDuration = 0.06f;
        [SerializeField, Min(0f)] private float meleeActiveDuration = 0.08f;
        [SerializeField, Min(0f)] private float meleeRecoveryDuration = 0.16f;

        [Header("Ranged (phase 6)")]
        [SerializeField, Min(0f)] private float projectileDamage = 8f;

        [Tooltip("Fast enough that a shot lands while the trigger pull still feels connected to it. " +
                 "At this speed a shot crosses the 20-unit arena in well under a second.")]
        [SerializeField, Min(0f)] private float projectileSpeed = 32f;

        [Tooltip("Time between shots while the trigger is held. Gameplay owns this; no animation, " +
                 "ammunition, or reload may change it.")]
        [SerializeField, Min(0f)] private float fireCooldown = 0.3f;

        [Tooltip("Seconds before an unobstructed projectile recycles itself. 1.5s at 20 units/sec " +
                 "crosses the 20x20 arena corner to corner with room to spare.")]
        [SerializeField, Min(0f)] private float projectileLifetime = 1.5f;

        [Tooltip("Sweep radius. Large enough that a projectile cannot slip past a thin target, small " +
                 "enough that it does not clip walls it visibly passed.")]
        [SerializeField, Min(0.01f)] private float projectileRadius = 0.12f;

        [Tooltip("Projectiles allocated up front. The pool never grows: at this cadence and lifetime " +
                 "no more than a handful can be alive, so exhaustion means something else is wrong.")]
        [SerializeField, Min(1)] private int projectilePoolSize = 32;

        [Header("Shield (phase 7)")]
        [SerializeField, Min(1f)] private float shieldStability = 100f;

        [Tooltip("Total width of the blocked arc, centred on shield facing. A hit is blocked when it " +
                 "arrives within half of this from the facing direction.")]
        [SerializeField, Range(0f, 360f)] private float shieldArc = 120f;

        [Tooltip("Stability drained by an attack that does not declare its own value.")]
        [SerializeField, Min(0f)] private float lightStabilityDamage = 10f;

        [Tooltip("Reference value for heavy attacks. Phase 9 enemies declare this on their own hits.")]
        [SerializeField, Min(0f)] private float heavyStabilityDamage = 30f;

        [SerializeField, Min(0f)] private float loweredStabilityRegeneration = 12f;
        [SerializeField, Min(0f)] private float raisedStabilityRegeneration = 4f;

        [Tooltip("Seconds a broken shield is unusable, and the whole cost of a break. Stability " +
                 "refills throughout, so the shield returns with a real charge the instant the " +
                 "lockout ends. One number, one wait: the player is punished and then back in it.")]
        [SerializeField, Min(0f)] private float shieldBreakLockout = 2.5f;

        [SerializeField, Range(0f, 1f)] private float shieldMoveMultiplier = 0.85f;

        [Header("Dodge (phase 8)")]
        [Tooltip("Ground distance covered by one dodge. Reached exactly unless geometry stops it.")]
        [SerializeField, Min(0f)] private float dodgeDistance = 3f;

        [Tooltip("How long the burst takes. Short enough to read as a burst rather than a sprint.")]
        [SerializeField, Min(0.01f)] private float dodgeDuration = 0.2f;

        [Tooltip("Time from the start of one dodge until the next may begin. Long enough that a dodge " +
                 "is a decision rather than a second movement speed.")]
        [SerializeField, Min(0f)] private float dodgeCooldown = 1f;

        [Tooltip("Invulnerable window, measured from the start of the dodge. Must cover the whole " +
                 "movement, or the player is hittable while visibly mid-dodge, which reads as a lie.")]
        [SerializeField, Min(0f)] private float dodgeInvulnerability = 0.2f;

        [Tooltip("How long a dodge press stays queued, for the same reason melee buffers one: a press " +
                 "made during the melee active window or the tail of the cooldown fires on the first " +
                 "legal frame instead of being dropped. Must stay well under the cooldown.")]
        [SerializeField, Range(0f, 0.4f)] private float dodgeInputBuffer = 0.15f;

        [Header("Feedback (phase 5)")]
        [Tooltip("Unscaled seconds of hit-stop on a connecting swing. Zero disables it.")]
        [SerializeField, Range(0f, 0.2f)] private float hitStopDuration = 0.05f;

        public float MoveSpeed => moveSpeed;
        public float MoveAcceleration => moveAcceleration;
        public float MoveDeceleration => moveDeceleration;
        public float Gravity => gravity;
        public float AimDeadZone => aimDeadZone;
        public float VisualTurnSpeed => visualTurnSpeed;

        public float PlayerMaximumHealth => playerMaximumHealth;

        public float MeleeDamage => meleeDamage;
        public float MeleeCooldown => meleeCooldown;
        public float MeleeInputBuffer => meleeInputBuffer;
        public float MeleeRange => meleeRange;
        public float MeleeRadius => meleeRadius;
        public float MeleeAttackHeight => meleeAttackHeight;
        public float MeleeTargetCorrection => meleeTargetCorrection;
        public float MeleeKnockback => meleeKnockback;
        public float MeleeStartupDuration => meleeStartupDuration;
        public float MeleeActiveDuration => meleeActiveDuration;
        public float MeleeRecoveryDuration => meleeRecoveryDuration;

        public float ProjectileDamage => projectileDamage;
        public float ProjectileSpeed => projectileSpeed;
        public float FireCooldown => fireCooldown;
        public float ProjectileLifetime => projectileLifetime;
        public float ProjectileRadius => projectileRadius;
        public int ProjectilePoolSize => projectilePoolSize;

        public float ShieldStability => shieldStability;
        public float ShieldArcDegrees => shieldArc;
        public float LightStabilityDamage => lightStabilityDamage;
        public float HeavyStabilityDamage => heavyStabilityDamage;
        public float LoweredStabilityRegeneration => loweredStabilityRegeneration;
        public float RaisedStabilityRegeneration => raisedStabilityRegeneration;
        public float ShieldBreakLockout => shieldBreakLockout;
        public float ShieldMoveMultiplier => shieldMoveMultiplier;

        /// <summary>
        /// The whole cost of a break: the lockout, and nothing after it. Stability refills during the
        /// lockout, so the shield is usable again the moment it ends.
        /// </summary>
        public float ShieldTotalRecoveryTime => shieldBreakLockout;

        /// <summary>Stability the shield carries when it returns from a break.</summary>
        public float StabilityAfterBreak =>
            Mathf.Min(shieldStability, loweredStabilityRegeneration * shieldBreakLockout);

        public float DodgeDistance => dodgeDistance;
        public float DodgeDuration => dodgeDuration;
        public float DodgeCooldown => dodgeCooldown;
        public float DodgeInvulnerability => dodgeInvulnerability;
        public float DodgeInputBuffer => dodgeInputBuffer;

        /// <summary>
        /// Average speed of the burst. Only a readability aid for tuning and the debug overlay: the
        /// dodge follows a decelerating curve, so this is not the speed at any particular instant.
        /// </summary>
        public float DodgeAverageSpeed => dodgeDuration > 0f ? dodgeDistance / dodgeDuration : 0f;

        public float HitStopDuration => hitStopDuration;

        /// <summary>Total authored length of one swing, before the cooldown is considered.</summary>
        public float MeleeSwingDuration => meleeStartupDuration + meleeActiveDuration + meleeRecoveryDuration;
    }
}
