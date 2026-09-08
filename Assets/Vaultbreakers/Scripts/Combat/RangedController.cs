using System;
using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Equipment;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Hold-to-fire from COMBAT_POC_PLAN.md section 6. The cadence is gameplay-authoritative and
    /// carries its own overshoot forward, so the interval between shots stays stable no matter what
    /// frame rate the game is running at and no matter how long any animation is. There is no
    /// ammunition and no reload.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(PlayerMotor.MotorExecutionOrder + 3)]
    public sealed class RangedController : MonoBehaviour
    {
        /// <summary>
        /// Half a millisecond of slack, for the same reason the melee cooldown has it: summing
        /// per-frame deltas leaves the remainder a hair either side of zero and a shot must not be
        /// dropped on the frame the cadence says it is due.
        /// </summary>
        private const float CooldownEpsilon = 0.0005f;

        [SerializeField] private PrototypeBalance balance;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerActionCoordinator actions;
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private Health health;
        [SerializeField] private AvatarSocketRegistry sockets;
        [SerializeField] private ProjectilePool pool;

        [Header("Fallback tuning (overwritten by the balance asset when one is assigned)")]
        [SerializeField, Min(0f)] private float damage = 8f;
        [SerializeField, Min(0f)] private float projectileSpeed = 32f;
        [SerializeField, Min(0f)] private float fireCooldown = 0.3f;
        [SerializeField, Min(0f)] private float projectileLifetime = 1.5f;

        [Tooltip("Muzzle height used only when the avatar has no Muzzle anchor, as in a bare test rig.")]
        [SerializeField, Min(0f)] private float fallbackMuzzleHeight = 1.2f;

        private Health subscribedHealth;
        private float fireTimer;

        /// <summary>True while the trigger is held and the coordinator allows the attack to continue.</summary>
        public bool IsFiring { get; private set; }

        public float CooldownRemaining => Mathf.Max(0f, fireTimer);
        public int ShotsFired { get; private set; }
        public ProjectilePool Pool => pool;

        /// <summary>Raised for every shot, with the muzzle position and the direction it left along.</summary>
        public event Action<Vector3, Vector3> Fired;

        private void Awake()
        {
            ResolveReferences();
            ApplyBalance();
            Subscribe();
        }

        private void OnDestroy() => Unsubscribe();

        private void Update() { if (Time.timeScale > 0) Tick(Time.deltaTime, input != null && input.RangedHeld); }

        /// <summary>
        /// One frame of the firing loop. The held state is a parameter rather than something read from
        /// the reader here, so the cadence rules can be driven without a physical input device.
        /// </summary>
        public void Tick(float deltaTime, bool fireHeld)
        {
            if (fireTimer > 0f)
            {
                fireTimer -= deltaTime;
            }

            UpdateFiringClaim(fireHeld);

            if (!IsFiring)
            {
                // Never bank a shot while the trigger is up, or a tap after a long pause would
                // release a burst instead of a single round.
                fireTimer = Mathf.Max(0f, fireTimer);
                return;
            }

            if (fireTimer > CooldownEpsilon)
            {
                return;
            }

            Fire();

            // Carrying the remainder forward rather than resetting keeps the interval between shots
            // stable when the cooldown is not a whole number of frames.
            fireTimer = Mathf.Max(0f, fireTimer + fireCooldown);
        }

        /// <summary>Drops the trigger and releases the claim, for death or a wave reset.</summary>
        public void StopFiring()
        {
            if (!IsFiring)
            {
                return;
            }

            IsFiring = false;
            if (actions != null)
            {
                actions.Stop(PlayerAction.Ranged);
            }
        }

        public void Configure(
            PrototypeBalance prototypeBalance,
            PlayerInputReader reader,
            PlayerActionCoordinator coordinator,
            PlayerFacing playerFacing,
            Health playerHealth,
            AvatarSocketRegistry socketRegistry,
            ProjectilePool projectilePool)
        {
            balance = prototypeBalance;
            input = reader;
            actions = coordinator;
            facing = playerFacing;
            health = playerHealth;
            sockets = socketRegistry;
            pool = projectilePool;
            ApplyBalance();
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

            damage = balance.ProjectileDamage;
            projectileSpeed = balance.ProjectileSpeed;
            fireCooldown = balance.FireCooldown;
            projectileLifetime = balance.ProjectileLifetime;
        }

        /// <summary>
        /// The coordinator owns the fire-versus-defence rule, not this controller. Raising the shield
        /// revokes the claim and firing stops on that frame; while the trigger stays held the claim is
        /// requested again every frame, so fire resumes by itself the moment the shield comes down.
        /// </summary>
        private void UpdateFiringClaim(bool fireHeld)
        {
            if (!fireHeld)
            {
                StopFiring();
                return;
            }

            if (!IsFiring)
            {
                IsFiring = actions == null || actions.TryStart(PlayerAction.Ranged);
                return;
            }

            if (actions != null && !actions.IsRangedActive)
            {
                IsFiring = false;
            }
        }

        private void Fire()
        {
            var origin = MuzzlePosition;
            var direction = FiringDirection;

            if (pool != null)
            {
                pool.Fire(origin, direction, projectileSpeed, damage, projectileLifetime, gameObject);
            }

            ShotsFired++;
            Fired?.Invoke(origin, direction);
        }

        /// <summary>
        /// Position comes from the Muzzle anchor so the shot leaves the weapon; direction comes from
        /// gameplay facing, not from the model, so a smoothed visual turn can never bend a shot.
        /// </summary>
        private Vector3 MuzzlePosition =>
            sockets != null && sockets.TryGet(AvatarSocketId.Muzzle, out var muzzle)
                ? muzzle.position
                : transform.position + Vector3.up * fallbackMuzzleHeight;

        private Vector3 FiringDirection
        {
            get
            {
                var direction = facing != null ? facing.LastCombatFacingDirection : transform.forward;
                direction.y = 0f;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    direction = transform.forward;
                    direction.y = 0f;
                }

                return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            }
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

        /// <summary>
        /// Death stops the trigger and clears the cadence. Projectiles already in flight are left
        /// alone deliberately: they are independent of the shooter once they exist.
        /// </summary>
        private void OnDied(DamageInfo damageInfo) => ResetCombat();

        public void ResetCombat()
        {
            StopFiring();
            fireTimer = 0f;
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

            if (sockets == null)
            {
                sockets = GetComponent<AvatarSocketRegistry>();
            }

            if (pool == null)
            {
                pool = GetComponent<ProjectilePool>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsFiring ? Color.cyan : new Color(0f, 0.6f, 0.8f, 0.5f);
            var origin = MuzzlePosition;
            Gizmos.DrawLine(origin, origin + FiringDirection * projectileSpeed * projectileLifetime);
            Gizmos.DrawWireSphere(origin, 0.1f);
        }
    }
}
