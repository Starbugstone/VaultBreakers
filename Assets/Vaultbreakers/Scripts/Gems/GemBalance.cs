using UnityEngine;

namespace Vaultbreakers.Gems
{
    public enum GemKind { Melee, Ranged, Shield, Relic, Gold }
    public enum LootSource { Grunt, Shooter, Bruiser, Crate, Chest }

    [CreateAssetMenu(menuName = "Vaultbreakers/Gem balance")]
    public sealed class GemBalance : ScriptableObject
    {
        [Min(8)] public int capacity = 40;
        [Min(.1f)] public float lifetime = 8f;
        [Min(0)] public float magnetDelay = .2f;
        [Min(.1f)] public float magnetRadius = 2.5f;
        [Min(.1f)] public float pickupRadius = .65f;
        [Min(.1f)] public float pullSpeed = 5f;
        [Min(.1f)] public float pullAcceleration = 32f;
        [Min(.1f)] public float maximumPullSpeed = 18f;
        [Min(0)] public float collectionWindow = 3f;
        [Min(2)] public int gruntGems = 3, shooterGems = 4, bruiserGems = 6;
        [Min(2)] public int crateGems = 6, chestGems = 12;
        [Min(1)] public float crateHealth = 14f, chestHealth = 28f;
        [Min(1)] public int goldScore = 25;

        public int DropCount(LootSource source) => Mathf.Max(2, source switch
        {
            LootSource.Grunt => gruntGems, LootSource.Shooter => shooterGems,
            LootSource.Bruiser => bruiserGems, LootSource.Crate => crateGems, _ => chestGems
        });
    }
}
