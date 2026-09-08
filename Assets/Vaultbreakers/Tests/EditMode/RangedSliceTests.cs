using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;

namespace Vaultbreakers.Tests.EditMode
{
    /// <summary>
    /// The deterministic half of ranged fire: cadence, tap versus hold, and the fire-versus-defence
    /// rule. The controller is driven with an explicit held flag and an exact delta, so none of this
    /// needs a device, a projectile, or a frame of play.
    /// </summary>
    public sealed class RangedSliceTests
    {
        private const float Frame = 1f / 60f;

        private GameObject gameObject;
        private Health health;
        private PlayerActionCoordinator actions;
        private RangedController ranged;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("RangedTest");
            health = gameObject.AddComponent<Health>();
            health.Configure(100f);
            actions = gameObject.AddComponent<PlayerActionCoordinator>();
            actions.Configure(health);
            ranged = gameObject.AddComponent<RangedController>();
            ranged.Configure(null, null, actions, null, health, null, null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(gameObject);

        private void Hold(int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                ranged.Tick(Frame, true);
            }
        }

        private void Release(int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                ranged.Tick(Frame, false);
            }
        }

        [Test]
        public void HoldingFire_ProducesTheAuthoredCadenceWithNoAmmunitionOrReload()
        {
            Hold(60);

            Assert.That(ranged.ShotsFired, Is.EqualTo(4),
                "One second of held fire at a 0.3 second cadence is four shots, starting immediately.");
            Assert.That(ranged.IsFiring, Is.True);
        }

        [Test]
        public void HoldingFire_KeepsTheCadenceStableOverALongBurst()
        {
            Hold(600);

            // Ten seconds, first shot at t=0: 1 + floor(10 / 0.3) = 34.
            Assert.That(ranged.ShotsFired, Is.EqualTo(34),
                "The cadence must not drift when the cooldown is not a whole number of frames.");
        }

        [Test]
        public void TappingFire_ProducesExactlyOneShot()
        {
            ranged.Tick(Frame, true);
            Assert.That(ranged.ShotsFired, Is.EqualTo(1), "The first shot must leave on the frame the trigger is pulled.");

            Release(5);
            ranged.Tick(Frame, true);
            Assert.That(ranged.ShotsFired, Is.EqualTo(1), "Re-tapping inside the cooldown must not add a shot.");
        }

        /// <summary>
        /// The cadence timer carries its remainder forward, so it has to be prevented from banking
        /// while the trigger is up. Without that, a long pause would empty a burst on the next tap.
        /// </summary>
        [Test]
        public void ReleasingTheTrigger_NeverBanksShots()
        {
            ranged.Tick(Frame, true);
            Release(120);
            ranged.Tick(Frame, true);

            Assert.That(ranged.ShotsFired, Is.EqualTo(2),
                "Two seconds of released trigger must produce one shot on the next pull, not a burst.");
        }

        [Test]
        public void Fire_IsRefusedWhileShieldingAndResumesWhenTheShieldDrops()
        {
            Assert.That(actions.TryStart(PlayerAction.Shield), Is.True);

            Hold(30);
            Assert.That(ranged.ShotsFired, Is.Zero, "Ranged fire cannot start while shielding.");
            Assert.That(ranged.IsFiring, Is.False);

            actions.Stop(PlayerAction.Shield);
            ranged.Tick(Frame, true);

            Assert.That(ranged.ShotsFired, Is.EqualTo(1), "Fire must resume by itself once the shield is down.");
        }

        [Test]
        public void RaisingTheShield_StopsAnActiveBurstImmediately()
        {
            ranged.Tick(Frame, true);
            Assert.That(ranged.ShotsFired, Is.EqualTo(1));
            Assert.That(ranged.IsFiring, Is.True);

            Assert.That(actions.TryStart(PlayerAction.Shield), Is.True, "Raising the shield must be allowed while firing.");
            ranged.Tick(Frame, true);
            Assert.That(ranged.IsFiring, Is.False, "The shield revoked the claim, so firing must stop on that frame.");

            Hold(60);
            Assert.That(ranged.ShotsFired, Is.EqualTo(1), "No shot may leave while the shield stays up.");
        }

        [Test]
        public void Dodging_StopsFireAndItResumesOnceTheDodgeEnds()
        {
            ranged.Tick(Frame, true);
            Assert.That(actions.TryStart(PlayerAction.Dodge), Is.True);

            Hold(60);
            Assert.That(ranged.ShotsFired, Is.EqualTo(1));

            actions.Stop(PlayerAction.Dodge);
            ranged.Tick(Frame, true);
            Assert.That(ranged.ShotsFired, Is.EqualTo(2));
        }

        [Test]
        public void Death_StopsFireAndBlocksItUntilTheStateIsReset()
        {
            Hold(30);
            var beforeDeath = ranged.ShotsFired;
            Assert.That(beforeDeath, Is.GreaterThan(0));

            health.ReceiveDamage(new DamageInfo(100f));
            Assert.That(ranged.IsFiring, Is.False);

            Hold(60);
            Assert.That(ranged.ShotsFired, Is.EqualTo(beforeDeath), "A dead player must not keep firing.");

            health.ResetHealth();
            actions.ResetState();
            ranged.Tick(Frame, true);
            Assert.That(ranged.ShotsFired, Is.EqualTo(beforeDeath + 1));
        }

        [Test]
        public void PrototypeBalance_ShipsTheDocumentedRangedValues()
        {
            var balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                Assert.That(balance.ProjectileDamage, Is.EqualTo(8f));
                Assert.That(balance.ProjectileSpeed, Is.EqualTo(32f));
                Assert.That(balance.FireCooldown, Is.EqualTo(0.3f));
                Assert.That(balance.ProjectileSpeed * balance.ProjectileLifetime, Is.GreaterThan(28.3f),
                    "A projectile must outlive the arena's corner-to-corner distance.");

                // The whole arena is 20 units across. A shot that takes longer than the time between
                // shots to cross it stops feeling like a response to the trigger.
                Assert.That(20f / balance.ProjectileSpeed, Is.LessThan(balance.FireCooldown * 2.5f),
                    "Shots must arrive while the pull that fired them still feels connected.");
                Assert.That(balance.ProjectilePoolSize,
                    Is.GreaterThan(Mathf.CeilToInt(balance.ProjectileLifetime / balance.FireCooldown)),
                    "The pool must comfortably outnumber the projectiles the cadence can keep alive.");
            }
            finally
            {
                Object.DestroyImmediate(balance);
            }
        }

        /// <summary>
        /// The fallbacks on the component happen to match the asset's defaults, so this deliberately
        /// tunes the asset away from them. Otherwise the test would pass even if nothing read the asset.
        /// </summary>
        [Test]
        public void RangedController_TakesItsCadenceFromTheBalanceAssetAndNotItsFallback()
        {
            var balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(balance);
                serialized.FindProperty("fireCooldown").floatValue = 0.5f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                ranged.Configure(balance, null, actions, null, health, null, null);
                Hold(60);

                Assert.That(ranged.ShotsFired, Is.EqualTo(2),
                    "One second at the asset's 0.5 second cadence is two shots, not the fallback's four.");
            }
            finally
            {
                Object.DestroyImmediate(balance);
            }
        }
    }
}
