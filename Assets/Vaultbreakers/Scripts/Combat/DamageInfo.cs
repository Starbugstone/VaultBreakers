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

        public DamageInfo(float amount, GameObject source = null, Vector3 knockback = default)
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            SourcePosition = source != null ? source.transform.position : default;
            HasSourcePosition = source != null;
            Knockback = knockback;
        }

        public DamageInfo(float amount, Vector3 sourcePosition, GameObject source = null, Vector3 knockback = default)
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            SourcePosition = sourcePosition;
            HasSourcePosition = true;
            Knockback = knockback;
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

        public DamageResult(float previousHealth, float currentHealth, bool killed)
        {
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            AppliedDamage = Mathf.Max(0f, previousHealth - currentHealth);
            Killed = killed;
        }
    }
}
