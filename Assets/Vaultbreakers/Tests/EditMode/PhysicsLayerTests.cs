using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Editor;

namespace Vaultbreakers.Tests.EditMode
{
    /// <summary>
    /// The collision matrix is stored as an opaque bitfield, so a wrong entry is invisible in
    /// review and only shows up as a projectile that passes through a wall. These assertions are
    /// written against the rules in PROJECT.md 5.5 and COMBAT_POC_PLAN.md 5 rather than against
    /// the setup table, so a mistake in the table does not agree with itself.
    /// </summary>
    public sealed class PhysicsLayerTests
    {
        private static readonly int[] AttackLayers =
        {
            GameLayers.PlayerProjectile, GameLayers.EnemyProjectile,
            GameLayers.PlayerMeleeHit, GameLayers.EnemyAttack
        };

        [Test]
        public void Layers_AreRegisteredAtTheirDocumentedIndices()
        {
            foreach (var definition in GameLayers.All)
            {
                Assert.AreEqual(definition.Name, LayerMask.LayerToName(definition.Index),
                    "Layer index " + definition.Index + " does not hold the documented layer.");
                Assert.AreEqual(definition.Index, LayerMask.NameToLayer(definition.Name),
                    "Layer " + definition.Name + " does not resolve back to its documented index.");
            }
        }

        [Test]
        public void Layers_DoNotOverlapUnityBuiltins()
        {
            foreach (var definition in GameLayers.All)
            {
                Assert.GreaterOrEqual(definition.Index, 8, "Layer " + definition.Name + " occupies a reserved Unity slot.");
                Assert.Less(definition.Index, 32);
            }

            CollectionAssert.AllItemsAreUnique(GameLayers.All.Select(definition => definition.Index).ToArray());
            CollectionAssert.AllItemsAreUnique(GameLayers.All.Select(definition => definition.Name).ToArray());
        }

        [Test]
        public void CollisionMatrix_MatchesTheConfiguredRules()
        {
            foreach (var a in GameLayers.All)
            {
                foreach (var b in GameLayers.All)
                {
                    var expected = VaultbreakersPhysicsSetup.ShouldCollide(a.Index, b.Index);
                    Assert.AreEqual(expected, !Physics.GetIgnoreLayerCollision(a.Index, b.Index),
                        a.Name + " vs " + b.Name + " is not applied to the project collision matrix.");
                }
            }
        }

        [Test]
        public void NoFriendlyFire()
        {
            AssertIgnored(GameLayers.PlayerProjectile, GameLayers.Player);
            AssertIgnored(GameLayers.PlayerMeleeHit, GameLayers.Player);
            AssertIgnored(GameLayers.PlayerProjectile, GameLayers.Pet);
            AssertIgnored(GameLayers.EnemyProjectile, GameLayers.Enemy);
            AssertIgnored(GameLayers.EnemyAttack, GameLayers.Enemy);
        }

        [Test]
        public void PlayersDoNotCollideWithEachOther()
        {
            AssertIgnored(GameLayers.Player, GameLayers.Player);
        }

        [Test]
        public void AttackVolumesNeverCollideWithEachOther()
        {
            foreach (var a in AttackLayers)
            {
                foreach (var b in AttackLayers)
                {
                    AssertIgnored(a, b);
                }
            }
        }

        [Test]
        public void PlayerProjectilesHitEnemiesAndGeometryOnly()
        {
            CollectionAssert.AreEquivalent(
                new[] { GameLayers.Enemy, GameLayers.Environment },
                CollidingLayers(GameLayers.PlayerProjectile));
        }

        [Test]
        public void EnemyProjectilesHitThePlayerAndGeometryOnly()
        {
            CollectionAssert.AreEquivalent(
                new[] { GameLayers.Player, GameLayers.Environment },
                CollidingLayers(GameLayers.EnemyProjectile));
        }

        [Test]
        public void MeleeAndAttackVolumesOnlyReachTheOpposingBody()
        {
            CollectionAssert.AreEquivalent(new[] { GameLayers.Enemy }, CollidingLayers(GameLayers.PlayerMeleeHit));
            CollectionAssert.AreEquivalent(new[] { GameLayers.Player }, CollidingLayers(GameLayers.EnemyAttack));
        }

        [Test]
        public void BodiesCollideWithArenaGeometry()
        {
            AssertCollides(GameLayers.Player, GameLayers.Environment);
            AssertCollides(GameLayers.Enemy, GameLayers.Environment);
            AssertCollides(GameLayers.Player, GameLayers.Enemy);
            AssertCollides(GameLayers.Enemy, GameLayers.Enemy);
        }

        [Test]
        public void PetsDoNotObstructCombat()
        {
            AssertIgnored(GameLayers.Pet, GameLayers.Player);
            AssertIgnored(GameLayers.Pet, GameLayers.Enemy);
            AssertIgnored(GameLayers.Pet, GameLayers.EnemyProjectile);
            AssertIgnored(GameLayers.Pet, GameLayers.EnemyAttack);
        }

        [Test]
        public void GemsNeverBlockMovementOrProjectiles()
        {
            AssertIgnored(GameLayers.Gem, GameLayers.Enemy);
            AssertIgnored(GameLayers.Gem, GameLayers.PlayerProjectile);
            AssertIgnored(GameLayers.Gem, GameLayers.EnemyProjectile);
            AssertIgnored(GameLayers.Gem, GameLayers.Gem);

            AssertCollides(GameLayers.Gem, GameLayers.Player);
            AssertCollides(GameLayers.Gem, GameLayers.Pet);
        }

        [Test]
        public void DebugLayerIsInertInPlay()
        {
            foreach (var definition in GameLayers.All)
            {
                AssertIgnored(GameLayers.Debug, definition.Index);
            }
        }

        private static int[] CollidingLayers(int layer)
        {
            var result = new List<int>();
            foreach (var definition in GameLayers.All)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, definition.Index))
                {
                    result.Add(definition.Index);
                }
            }

            return result.ToArray();
        }

        private static void AssertIgnored(int a, int b)
        {
            Assert.IsTrue(Physics.GetIgnoreLayerCollision(a, b),
                LayerMask.LayerToName(a) + " must not collide with " + LayerMask.LayerToName(b) + ".");
        }

        private static void AssertCollides(int a, int b)
        {
            Assert.IsFalse(Physics.GetIgnoreLayerCollision(a, b),
                LayerMask.LayerToName(a) + " must collide with " + LayerMask.LayerToName(b) + ".");
        }
    }
}
