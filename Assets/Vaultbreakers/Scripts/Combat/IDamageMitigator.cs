namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Given first refusal on a hit before it reaches <see cref="Health"/>. This is the seam the
    /// directional shield uses, and it exists so damage keeps a single entry point: every attack in
    /// the game still calls <see cref="IDamageable.ReceiveDamage"/> and knows nothing about shields.
    /// </summary>
    public interface IDamageMitigator
    {
        /// <summary>
        /// Returns true when the hit was fully absorbed and must not reach health. A mitigator that
        /// absorbs is responsible for its own cost and its own feedback.
        /// </summary>
        bool TryAbsorb(in DamageInfo damage);
    }
}
