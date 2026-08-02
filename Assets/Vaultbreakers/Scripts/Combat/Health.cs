using System;
using UnityEngine;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// The single damage sink for every damageable body. Health owns no presentation and records no
    /// diagnostics of its own; listeners such as the debug overlay subscribe to these events instead,
    /// which keeps string formatting out of the per-hit path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField] private bool invulnerable;

        private bool initialized;

        public float MaximumHealth => Mathf.Max(1f, maximumHealth);
        public float CurrentHealth { get; private set; }
        public bool IsInvulnerable => invulnerable;
        public bool IsDead { get; private set; }

        /// <summary>Raised for every hit that reached health. IsDead is already final when this fires.</summary>
        public event Action<DamageInfo, DamageResult> Damaged;

        /// <summary>Raised exactly once per life, immediately after the lethal Damaged event.</summary>
        public event Action<DamageInfo> Died;

        public event Action ResetPerformed;

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            CurrentHealth = MaximumHealth;
            IsDead = false;
        }

        public DamageResult ReceiveDamage(in DamageInfo damage)
        {
            Initialize();
            var previous = CurrentHealth;
            if (IsDead || invulnerable || damage.Amount <= 0f)
            {
                return new DamageResult(previous, previous, false);
            }

            CurrentHealth = Mathf.Clamp(CurrentHealth - damage.Amount, 0f, MaximumHealth);
            var killed = CurrentHealth <= 0f;
            var result = new DamageResult(previous, CurrentHealth, killed);

            // Death is committed before the hit is announced, so no listener can observe a body that
            // is simultaneously at zero health and alive.
            IsDead = killed;
            Damaged?.Invoke(damage, result);

            if (killed)
            {
                Died?.Invoke(damage);
            }

            return result;
        }

        public void ResetHealth()
        {
            Initialize();
            CurrentHealth = MaximumHealth;
            IsDead = false;
            ResetPerformed?.Invoke();
        }

        public void SetInvulnerable(bool value) => invulnerable = value;

        public void Configure(float newMaximumHealth)
        {
            maximumHealth = Mathf.Max(1f, newMaximumHealth);
            initialized = false;
            Initialize();
        }
    }
}
