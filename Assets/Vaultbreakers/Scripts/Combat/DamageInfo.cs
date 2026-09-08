using UnityEngine;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// One incoming hit. The source position is captured at construction so later shield-arc maths
    /// can run without the attacker still existing. A hit with no known source reports
    /// <see cref="HasSourcePosition"/> false and is therefore unshieldable by default, which avoids
    /// accidental omnidirectional blocks.
    /// </summary>
    public readonly struct DamageInfo
    {
        public float Amount { get; }
        public GameObject Source { get; }
        public Vector3 SourcePosition { get; }
        public bool HasSourcePosition { get; }
        public Vector3 Knockback { get; }

        /// <summary>
        /// Stability drained from a shield that blocks this hit. Zero means the attack did not declare
        /// one, which a shield treats as an ordinary light hit; heavy attacks state their own value.
        /// </summary>
        public float StabilityDamage { get; }

        public DamageInfo(
            float amount,
            GameObject source = null,
            Vector3 knockback = default,
            float stabilityDamage = 0f)
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            SourcePosition = source != null ? source.transform.position : default;
            HasSourcePosition = source != null;
            Knockback = knockback;
            StabilityDamage = Mathf.Max(0f, stabilityDamage);
        }

        public DamageInfo(
            float amount,
            Vector3 sourcePosition,
            GameObject source = null,
            Vector3 knockback = default,
            float stabilityDamage = 0f)
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            SourcePosition = sourcePosition;
            HasSourcePosition = true;
            Knockback = knockback;
            StabilityDamage = Mathf.Max(0f, stabilityDamage);
        }
    }

    /// <summary>Outcome of one <see cref="IDamageable.ReceiveDamage"/> call.</summary>
    public readonly struct DamageResult
    {
        public float PreviousHealth { get; }
        public float CurrentHealth { get; }
        public float AppliedDamage { get; }
        public bool WasApplied => AppliedDamage > 0f;
        public bool Killed { get; }

        /// <summary>
        /// True when a mitigator absorbed the hit before it reached health. Distinguishes a shielded
        /// hit from one that simply missed or landed on an invulnerable body, both of which also
        /// report no applied damage.
        /// </summary>
        public bool Blocked { get; }

        public DamageResult(float previousHealth, float currentHealth, bool killed, bool blocked = false)
        {
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            AppliedDamage = Mathf.Max(0f, previousHealth - currentHealth);
            Killed = killed;
            Blocked = blocked;
        }
    }
}
