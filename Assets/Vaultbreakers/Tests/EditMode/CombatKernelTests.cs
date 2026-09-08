using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Combat;

namespace Vaultbreakers.Tests.EditMode
{
    /// <summary>
    /// Deterministic combat kernel rules. Every hit in the game arrives through
    /// <see cref="IDamageable.ReceiveDamage"/>, so these run without a scene.
    /// </summary>
    public sealed class CombatKernelTests
    {
        private GameObject gameObject;
        private Health health;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("HealthTest");
            health = gameObject.AddComponent<Health>();
            health.Configure(100f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(gameObject);

        [Test]
        public void ZoneHealingClampsAndNeverRevivesADeadPlayer()
        {
            health.ReceiveDamage(new DamageInfo(40));Assert.That(health.Heal(100),Is.EqualTo(40));
            Assert.That(health.CurrentHealth,Is.EqualTo(100));Assert.That(health.Heal(-10),Is.Zero);
            health.ReceiveDamage(new DamageInfo(100));Assert.That(health.Heal(100),Is.Zero);Assert.That(health.IsDead,Is.True);
        }

        [Test]
        public void Health_ClampsDamageAtZero()
        {
            health.ReceiveDamage(new DamageInfo(150f));
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(health.IsDead, Is.True);
        }

        [Test]
        public void Health_ZeroAndNegativeDamageAreIgnored()
        {
            Assert.That(health.ReceiveDamage(new DamageInfo(0f)).WasApplied, Is.False);
            Assert.That(health.ReceiveDamage(new DamageInfo(-10f)).WasApplied, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void Health_InvulnerabilityRejectsDamage()
        {
            health.SetInvulnerable(true);
            Assert.That(health.ReceiveDamage(new DamageInfo(50f)).WasApplied, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void Health_RepeatedLethalHitRaisesOneDeathEvent()
        {
            var deaths = 0;
            health.Died += _ => deaths++;
            health.ReceiveDamage(new DamageInfo(100f));
            health.ReceiveDamage(new DamageInfo(100f));
            Assert.That(deaths, Is.EqualTo(1));
        }

        /// <summary>
        /// Hit reactions and feedback listen to Damaged. If death were committed after that event,
        /// the lethal hit would play an ordinary hit reaction on a body that is already dead.
        /// </summary>
        [Test]
        public void Health_LethalHitIsAlreadyDeadWhenDamagedIsRaised()
        {
            bool? deadDuringDamaged = null;
            var damagedRanBeforeDied = false;
            health.Damaged += (_, _) => deadDuringDamaged = health.IsDead;
            health.Died += _ => damagedRanBeforeDied = deadDuringDamaged.HasValue;

            health.ReceiveDamage(new DamageInfo(100f));

            Assert.That(deadDuringDamaged, Is.True, "IsDead must be final before Damaged is raised.");
            Assert.That(damagedRanBeforeDied, Is.True, "Died must follow the lethal Damaged event.");
        }

        [Test]
        public void Health_NonLethalHitDoesNotReportDeath()
        {
            bool? deadDuringDamaged = null;
            health.Damaged += (_, _) => deadDuringDamaged = health.IsDead;

            var result = health.ReceiveDamage(new DamageInfo(40f));

            Assert.That(deadDuringDamaged, Is.False);
            Assert.That(result.Killed, Is.False);
            Assert.That(result.AppliedDamage, Is.EqualTo(40f));
            Assert.That(result.PreviousHealth, Is.EqualTo(100f));
        }

        [Test]
        public void Health_ResetRestoresValidAliveState()
        {
            health.ReceiveDamage(new DamageInfo(100f));
            health.ResetHealth();
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
            Assert.That(health.IsDead, Is.False);
        }

        [Test]
        public void ActionPolicy_EnforcesCombatConflictsAndDeathCancellation()
        {
            var actions = CreateCoordinator();
            Assert.That(actions.TryStart(PlayerAction.Ranged), Is.True);
            Assert.That(actions.TryStart(PlayerAction.Shield), Is.True, "Shield should stop ranged fire.");
            Assert.That(actions.IsRangedActive, Is.False);
            Assert.That(actions.TryStart(PlayerAction.Ranged), Is.False, "Ranged cannot start under shield.");
            Assert.That(actions.TryStart(PlayerAction.Melee), Is.True, "Melee should suppress shield.");
            Assert.That(actions.State.HasFlag(PlayerActionState.Shielding), Is.False);
            Assert.That(actions.TryStart(PlayerAction.Dodge), Is.True, "Dodge should cancel attacks.");
            Assert.That(actions.State, Is.EqualTo(PlayerActionState.Dodging));

            health.ReceiveDamage(new DamageInfo(100f));
            Assert.That(actions.State, Is.EqualTo(PlayerActionState.Dead));
            Assert.That(actions.TryStart(PlayerAction.Melee), Is.False);
        }

        [Test]
        public void ActionPolicy_ShieldIsRejectedWhileMeleeIsActiveAndAllowedOnceItEnds()
        {
            var actions = CreateCoordinator();

            Assert.That(actions.TryStart(PlayerAction.Melee), Is.True);
            Assert.That(actions.TryStart(PlayerAction.Shield), Is.False, "Melee suppresses the shield until it ends.");

            actions.Stop(PlayerAction.Melee);
            Assert.That(actions.IsMeleeActive, Is.False);
            Assert.That(actions.State.HasFlag(PlayerActionState.Attacking), Is.False,
                "Attacking must clear once no attack is running.");
            Assert.That(actions.TryStart(PlayerAction.Shield), Is.True);
        }

        [Test]
        public void ActionPolicy_StoppingDodgeAllowsActionsAgain()
        {
            var actions = CreateCoordinator();

            Assert.That(actions.TryStart(PlayerAction.Dodge), Is.True);
            Assert.That(actions.TryStart(PlayerAction.Melee), Is.False, "Dodge outranks melee while it runs.");

            actions.Stop(PlayerAction.Dodge);
            Assert.That(actions.State, Is.EqualTo(PlayerActionState.None));
            Assert.That(actions.TryStart(PlayerAction.Melee), Is.True);
        }

        [Test]
        public void ActionPolicy_ResetClearsEveryFlagAfterDeath()
        {
            var actions = CreateCoordinator();
            actions.TryStart(PlayerAction.Ranged);
            health.ReceiveDamage(new DamageInfo(100f));

            actions.ResetState();

            Assert.That(actions.State, Is.EqualTo(PlayerActionState.None));
            Assert.That(actions.IsRangedActive, Is.False);
            Assert.That(actions.TryStart(PlayerAction.Ranged), Is.True);
        }

        private PlayerActionCoordinator CreateCoordinator()
        {
            var actions = gameObject.AddComponent<PlayerActionCoordinator>();
            actions.Configure(health);
            return actions;
        }
    }
}
