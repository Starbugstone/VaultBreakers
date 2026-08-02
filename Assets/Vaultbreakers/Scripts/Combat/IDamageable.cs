namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Every hit in the game reaches its target through this one call, so melee, projectiles, enemy
    /// attacks, hazards, and debug tools all follow the same resolution order.
    /// </summary>
    public interface IDamageable
    {
        DamageResult ReceiveDamage(in DamageInfo damage);
    }
}
