using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;

namespace Vaultbreakers.Tests.EditMode
{
    /// <summary>
    /// The deterministic half of the melee slice: swing geometry, target correction, and the phase
    /// and cooldown timing. None of it needs a scene, a collider, or a rendered frame, so a
    /// regression here is caught in the EditMode pass rather than during a playtest.
    /// </summary>
    public sealed class MeleeSliceTests
    {
        private const float Range = 2.5f;
        private const float Radius = 1.5f;
        private const float Correction = 15f;

        private GameObject gameObject;
        private Health health;
        private PlayerActionCoordinator actions;
        private MeleeController melee;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("MeleeTest");
            health = gameObject.AddComponent<Health>();
            health.Configure(100f);
            actions = gameObject.AddComponent<PlayerActionCoordinator>();
            actions.Configure(health);
            melee = gameObject.AddComponent<MeleeController>();
            melee.Configure(null, null, actions, null, health, null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(gameObject);

        [Test]
        public void QueryCentre_PutsTheFarEdgeOfTheSphereAtTheConfiguredRange()
        {
            var centre = MeleeSwing.QueryCentre(Vector3.zero, Vector3.forward, Range, Radius);

            Assert.That(centre.z + Radius, Is.EqualTo(Range).Within(0.0001f));
            Assert.That(centre.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void QueryCentre_IgnoresTheVerticalComponentOfTheSwingDirection()
        {
            var tilted = MeleeSwing.QueryCentre(Vector3.zero, new Vector3(0f, 5f, 1f), Range, Radius);
            var flat = MeleeSwing.QueryCentre(Vector3.zero, Vector3.forward, Range, Radius);

            Assert.That(Vector3.Distance(tilted, flat), Is.LessThan(0.0001f));
        }

        [Test]
        public void QueryCentre_FallsBackToTheOriginForAZeroDirection()
        {
            Assert.That(
                Vector3.Distance(MeleeSwing.QueryCentre(Vector3.one, Vector3.zero, Range, Radius), Vector3.one),
                Is.LessThan(0.0001f));
        }

        [Test]
        public void IsWithinSwing_AcceptsTheEdgeOfRangeAndRejectsJustBeyondIt()
        {
            Assert.That(
                MeleeSwing.IsWithinSwing(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, Range - 0.01f), Range, Radius),
                Is.True, "A target just inside the configured range must be hit.");

            Assert.That(
                MeleeSwing.IsWithinSwing(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, Range + 0.01f), Range, Radius),
                Is.False, "A target beyond the configured range must be missed.");
        }

        [Test]
        public void IsWithinSwing_RejectsATargetBehindTheAttacker()
        {
            Assert.That(
                MeleeSwing.IsWithinSwing(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -2f), Range, Radius),
                Is.False);
        }

        /// <summary>
        /// An enemy pressed against the player is inside the sphere by construction. That is
        /// deliberate: melee has to remain usable when something has closed the distance.
        /// </summary>
        [Test]
        public void IsWithinSwing_AcceptsATargetPressedAgainstTheAttacker()
        {
            Assert.That(MeleeSwing.IsWithinSwing(Vector3.zero, Vector3.forward, Vector3.zero, Range, Radius), Is.True);
        }

        [Test]
        public void IsWithinSwing_IgnoresHeightDifferences()
        {
            Assert.That(
                MeleeSwing.IsWithinSwing(Vector3.zero, Vector3.forward, new Vector3(0f, 6f, 1.5f), Range, Radius),
                Is.True);
        }

        [Test]
        public void TargetCorrection_SnapsOntoATargetInsideTheCone()
        {
            var target = Quaternion.Euler(0f, 10f, 0f) * Vector3.forward * 2f;

            var corrected = MeleeSwing.ApplyTargetCorrection(Vector3.forward, Vector3.zero, target, Correction);

            Assert.That(Vector3.Angle(corrected, target), Is.LessThan(0.01f));
            Assert.That(Vector3.Angle(Vector3.forward, corrected), Is.EqualTo(10f).Within(0.01f));
        }

        [Test]
        public void TargetCorrection_IgnoresATargetOutsideTheCone()
        {
            var target = Quaternion.Euler(0f, 40f, 0f) * Vector3.forward * 2f;

            var corrected = MeleeSwing.ApplyTargetCorrection(Vector3.forward, Vector3.zero, target, Correction);

            Assert.That(Vector3.Angle(Vector3.forward, corrected), Is.EqualTo(0f).Within(0.01f),
                "A target the player did not aim at must never steal the swing.");
        }

        [Test]
        public void TargetCorrection_NeverExceedsTheConfiguredDegrees()
        {
            for (var offset = -90f; offset <= 90f; offset += 1f)
            {
                var target = Quaternion.Euler(0f, offset, 0f) * Vector3.forward * 2f;
                var corrected = MeleeSwing.ApplyTargetCorrection(Vector3.forward, Vector3.zero, target, Correction);

                Assert.That(Vector3.Angle(Vector3.forward, corrected), Is.LessThanOrEqualTo(Correction + 0.01f),
                    "Correction exceeded the cap at an offset of " + offset + " degrees.");
            }
        }

        [Test]
        public void TargetCorrection_SurvivesZeroLengthVectors()
        {
            Assert.That(
                Vector3.Distance(
                    MeleeSwing.ApplyTargetCorrection(Vector3.forward, Vector3.zero, Vector3.zero, Correction),
                    Vector3.forward),
                Is.LessThan(0.0001f),
                "A target standing exactly on the attacker must not produce a NaN direction.");

            Assert.That(
                MeleeSwing.ApplyTargetCorrection(Vector3.zero, Vector3.zero, Vector3.forward, Correction).magnitude,
                Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void CorrectionCandidate_RejectsTargetsOutOfReachOrOutOfCone()
        {
            var insideCone = Quaternion.Euler(0f, 8f, 0f) * Vector3.forward;

            Assert.That(
                MeleeSwing.IsCorrectionCandidate(Vector3.forward, Vector3.zero, insideCone * 2f, Range, Correction),
                Is.True);
            Assert.That(
                MeleeSwing.IsCorrectionCandidate(Vector3.forward, Vector3.zero, insideCone * 6f, Range, Correction),
                Is.False, "A target beyond reach is not worth correcting toward.");
            Assert.That(
                MeleeSwing.IsCorrectionCandidate(Vector3.forward, Vector3.zero, Vector3.back * 2f, Range, Correction),
                Is.False);
        }

        [Test]
        public void Knockback_PointsAwayFromTheAttackerAndFallsBackToTheSwing()
        {
            var away = MeleeSwing.KnockbackDirection(Vector3.zero, new Vector3(0f, 3f, 2f), Vector3.forward);
            Assert.That(Vector3.Distance(away, Vector3.forward), Is.LessThan(0.0001f),
                "Knockback must be horizontal and directed away from the attacker.");

            var coincident = MeleeSwing.KnockbackDirection(Vector3.zero, Vector3.zero, Vector3.right);
            Assert.That(Vector3.Distance(coincident, Vector3.right), Is.LessThan(0.0001f));
        }

        [Test]
        public void Swing_RunsStartupActiveAndRecoveryAtTheAuthoredTimes()
        {
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Ready));
            Assert.That(melee.TryStartSwing(), Is.True);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Startup));

            melee.Tick(0.06f);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Active), "The active window must open after startup.");

            melee.Tick(0.08f);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Recovery));

            melee.Tick(0.16f);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Ready));
        }

        [Test]
        public void Swing_ClaimsTheAttackStateAndReleasesItWhenRecoveryEnds()
        {
            melee.TryStartSwing();
            Assert.That(actions.IsMeleeActive, Is.True);
            Assert.That(actions.State.HasFlag(PlayerActionState.Attacking), Is.True);
            Assert.That(actions.TryStart(PlayerAction.Shield), Is.False, "Melee must suppress the shield while it runs.");

            melee.Tick(0.06f);
            melee.Tick(0.08f);
            melee.Tick(0.16f);

            Assert.That(actions.IsMeleeActive, Is.False);
            Assert.That(actions.State.HasFlag(PlayerActionState.Attacking), Is.False);
            Assert.That(actions.TryStart(PlayerAction.Shield), Is.True);
        }

        /// <summary>
        /// The cadence is set equal to the authored swing, so a swing flows straight into the next
        /// one. A cooldown longer than the swing would leave a window where the game is visibly idle
        /// and still refusing input, which is the classic source of "it ate my press".
        /// </summary>
        [Test]
        public void Cooldown_RejectsSpamAndLetsTheNextSwingFollowImmediately()
        {
            Assert.That(melee.TryStartSwing(), Is.True);
            Assert.That(melee.TryStartSwing(), Is.False, "A second swing during startup must be refused.");

            melee.Tick(0.06f);
            Assert.That(melee.TryStartSwing(), Is.False, "Nor during the active window.");

            melee.Tick(0.08f);
            Assert.That(melee.TryStartSwing(), Is.False, "Nor during recovery.");

            melee.Tick(0.16f);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Ready));
            Assert.That(melee.CooldownRemaining, Is.EqualTo(0f).Within(0.001f));
            Assert.That(melee.TryStartSwing(), Is.True, "The swing ends and the next one may start on that frame.");
        }

        /// <summary>
        /// The reactivity rule: a press during recovery is remembered, not discarded. Without this,
        /// mashing produces fewer swings than pressing in perfect rhythm, which is the opposite of
        /// how an arcade game should reward a player.
        /// </summary>
        [Test]
        public void BufferedPress_FiresOnTheFirstFrameTheSwingBecomesLegal()
        {
            Assert.That(melee.TryStartSwing(), Is.True);
            melee.Tick(0.06f);
            melee.Tick(0.08f);

            // Pressed midway through recovery, well before another swing is allowed.
            melee.BufferSwing();
            melee.Tick(0.08f);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Recovery), "Still recovering, so nothing has started yet.");

            melee.Tick(0.08f);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Startup), "The queued press must fire the moment it can.");
        }

        /// <summary>
        /// The buffer forgives a press made slightly too early, not one made a swing and a half ago.
        /// Without an expiry it would be a queue, and the player would watch swings they no longer
        /// want play out after they stopped pressing.
        /// </summary>
        [Test]
        public void BufferedPress_ExpiresWhenTheSwingStaysIllegalForLongerThanTheWindow()
        {
            Assert.That(melee.TryStartSwing(), Is.True);
            melee.BufferSwing();

            // 0.2 seconds of the press being refused, against a 0.15 second window.
            melee.Tick(0.1f);
            melee.Tick(0.1f);
            melee.Tick(0.1f);

            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Ready),
                "The first swing has ended and the stale press must not have started another.");
        }

        [Test]
        public void Swing_IsRefusedWhileDodgingAndAfterDeath()
        {
            Assert.That(actions.TryStart(PlayerAction.Dodge), Is.True);
            Assert.That(melee.TryStartSwing(), Is.False, "Dodge outranks melee.");
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Ready));

            actions.Stop(PlayerAction.Dodge);
            Assert.That(melee.TryStartSwing(), Is.True);

            health.ReceiveDamage(new DamageInfo(100f));
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Ready), "Death must cancel the swing in progress.");
            Assert.That(melee.TryStartSwing(), Is.False);
        }

        [Test]
        public void Swing_ResumesAfterDeathIsResetWithoutWaitingOutTheOldCooldown()
        {
            melee.TryStartSwing();
            health.ReceiveDamage(new DamageInfo(100f));

            health.ResetHealth();
            actions.ResetState();

            Assert.That(melee.TryStartSwing(), Is.True);
        }

        /// <summary>
        /// The starting values in COMBAT_POC_PLAN.md section 4 are the contract between the design and
        /// the code. Tuning them is expected; changing them by accident is not.
        /// </summary>
        [Test]
        public void PrototypeBalance_ShipsTheDocumentedStartingValues()
        {
            var balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                Assert.That(balance.MoveSpeed, Is.EqualTo(5f));
                Assert.That(balance.AimDeadZone, Is.EqualTo(0.25f));
                Assert.That(balance.PlayerMaximumHealth, Is.EqualTo(100f));
                Assert.That(balance.MeleeDamage, Is.EqualTo(10f));
                Assert.That(balance.MeleeRange, Is.EqualTo(2.5f));
                Assert.That(balance.MeleeRadius, Is.EqualTo(1.5f));
                Assert.That(balance.MeleeTargetCorrection, Is.EqualTo(15f));
                Assert.That(balance.MeleeSwingDuration, Is.EqualTo(balance.MeleeCooldown).Within(0.0001f),
                    "The cadence is deliberately the swing length: any longer and there is a window " +
                    "where the game is idle and still refusing input.");
                Assert.That(balance.MeleeInputBuffer, Is.GreaterThan(0f),
                    "A press made slightly early must be forgiven, or mashing loses to metronomic timing.");
                Assert.That(balance.MeleeInputBuffer, Is.LessThan(balance.MeleeCooldown),
                    "The buffer forgives an early press; it must not queue a whole extra swing.");
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
        public void MeleeController_TakesItsTuningFromTheBalanceAssetAndNotItsFallback()
        {
            var balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(balance);
                serialized.FindProperty("meleeRange").floatValue = 7f;
                serialized.FindProperty("meleeTargetCorrection").floatValue = 30f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                melee.Configure(balance, null, actions, null, health, null);

                Assert.That(melee.Range, Is.EqualTo(7f));
                Assert.That(melee.TargetCorrectionDegrees, Is.EqualTo(30f));
                Assert.That(melee.Radius, Is.EqualTo(balance.MeleeRadius));
            }
            finally
            {
                Object.DestroyImmediate(balance);
            }
        }
    }
}
