using System.Collections.Generic;
using UnityEngine;

namespace Vaultbreakers.Core
{
    public readonly struct GameLayerDefinition
    {
        public readonly int Index;
        public readonly string Name;

        public GameLayerDefinition(int index, string name)
        {
            Index = index;
            Name = name;
        }
    }

    /// <summary>
    /// Physics layer indices from PROJECT.md section 5.5. Layer indices are serialized into every
    /// scene, prefab, and collision matrix, so append new layers instead of reordering these.
    /// Faction checks use these constants rather than tag or name comparisons.
    /// </summary>
    public static class GameLayers
    {
        public const int Player = 8;
        public const int Enemy = 9;
        public const int PlayerProjectile = 10;
        public const int EnemyProjectile = 11;
        public const int PlayerMeleeHit = 12;
        public const int EnemyAttack = 13;
        public const int Gem = 14;
        public const int Pet = 15;
        public const int Hazard = 16;
        public const int Environment = 17;
        public const int Interactable = 18;
        public const int Debug = 19;

        private static readonly GameLayerDefinition[] all =
        {
            new(Player, "Player"),
            new(Enemy, "Enemy"),
            new(PlayerProjectile, "PlayerProjectile"),
            new(EnemyProjectile, "EnemyProjectile"),
            new(PlayerMeleeHit, "PlayerMeleeHit"),
            new(EnemyAttack, "EnemyAttack"),
            new(Gem, "Gem"),
            new(Pet, "Pet"),
            new(Hazard, "Hazard"),
            new(Environment, "Environment"),
            new(Interactable, "Interactable"),
            new(Debug, "Debug")
        };

        public static IReadOnlyList<GameLayerDefinition> All => all;

        /// <summary>Layers a player melee or projectile query should consider.</summary>
        public static LayerMask EnemyTargets => 1 << Enemy;

        /// <summary>Layers an enemy attack query should consider.</summary>
        public static LayerMask PlayerTargets => 1 << Player;

        /// <summary>Geometry that stops movement, dodges, and projectiles.</summary>
        public static LayerMask Blocking => 1 << Environment;

        /// <summary>
        /// Everything a player projectile must resolve against. A projectile sweeps against this in
        /// one query rather than relying on trigger callbacks, so it cannot tunnel at speed.
        /// </summary>
        public static LayerMask PlayerProjectileHits => (1 << Enemy) | (1 << Environment);
    }
}
