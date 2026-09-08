using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Player;

namespace Vaultbreakers.Tests.EditMode
{
    /// <summary>
    /// The dodge state machine and its displacement schedule. The Phase 8 exit gate asks that
    /// invulnerability begins and ends at predictable times and that spam cannot beat the cooldown,
    /// so both are asserted at exact deltas here rather than eyeballed in play. Wall behaviour is a
    /// physics question and lives in the PlayMode suite.
    /// </summary>
    public sealed class DodgeSliceTests
    {
        private const float Frame = 1f / 60f;
        private const float Distance = 3f;
        private const float Duration = 0.2f;
        private const float Cooldown = 1f;

        private GameObject gameObject;
        private Health health;
        private PlayerActionCoordinator actions;
        private DodgeController dodge;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("DodgeTest");
            health = gameObject.AddComponent<Health>();
            health.Configure(100f);
            actions = gameObject.AddComponent<PlayerActionCoordinator>();
            actions.Configure(health);
            dodge = gameObject.AddComponent<DodgeController>();
            dodge.Configure(null, null, actions, null, null, health, null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(gameObject);

        private void Advance(float seconds)
        {
            var frames = Mathf.RoundToInt(seconds / Frame);
            for (var frame = 0; frame < frames; frame++)
            {
                dodge.Tick(Frame);
            }
        }

        // --- The burst itself -------------------------------------------------------------------

        [Test]
        public void Dodge_CoversTheAuthoredDistanceExactly()
        {
            Assert.That(dodge.TryStartDodge(), Is.True);
            Advance(Duration + Frame);

            Assert.That(dodge.IsDodging, Is.False, "The dodge must end when its duration elapses.");
            Assert.That(dodge.DistanceTravelled, Is.EqualTo(Distance).Within(0.001f),
                "A dodge with nothing in the way must land on the authored distance.");
        }

        /// <summary>
        /// The distance is a schedule rather than a per-frame speed, so a frame rate the game never
        /// sees must still produce the same displacement. This is what stops a hitch from throwing
        /// the player somewhere they did not ask to go.
        /// </summary>
        [Test]
        public void Dodge_CoversTheSameDistanceAtAnyFrameRate()
        {
            dodge.TryStartDodge();
            dodge.Tick(Duration);

            Assert.That(dodge.DistanceTravelled, Is.EqualTo(Distance).Within(0.001f),
                "One enormous frame must not overshoot the authored distance.");
            Assert.That(dodge.IsDodging, Is.False);
        }

        [Test]
        public void Dodge_MovesFastestOnTheFrameItStarts()
        {
            var quarter = DodgeController.DisplacementCurve(0.25f);
            var lastQuarter = 1f - DodgeController.DisplacementCurve(0.75f);

            Assert.That(quarter, Is.GreaterThan(lastQuarter),
                "The burst must front-load, or the dodge does not answer the threat that provoked it.");
            Assert.That(DodgeController.DisplacementCurve(0f), Is.EqualTo(0f));
            Assert.That(DodgeController.DisplacementCurve(1f), Is.EqualTo(1f));
        }

        [Test]
        public void Dodge_DisplacementCurveIsClampedOutsideItsRange()
        {
            Assert.That(DodgeController.DisplacementCurve(-2f), Is.EqualTo(0f));
            Assert.That(DodgeController.DisplacementCurve(4f), Is.EqualTo(1f));
        }

        [Test]
        public void Dodge_TravelsAlongTheLastCombatFacingWhenThereIsNoMovementInput()
        {
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.forward = Vector3.right;

            Assert.That(dodge.TryStartDodge(), Is.True);
            Assert.That(dodge.DodgeDirection.x, Is.EqualTo(1f).Within(0.001f),
                "With the stick at rest the player dodges the way they are already looking.");

            Advance(Duration + Frame);
            Assert.That(gameObject.transform.position.x, Is.EqualTo(Distance).Within(0.001f));
            Assert.That(gameObject.transform.position.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Dodge_KeepsItsDirectionForTheWholeBurst()
        {
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.forward = Vector3.forward;
            dodge.TryStartDodge();

            // Turning the body mid-dodge must not bend the burst: the direction was decided when the
            // input was accepted, which is what makes a dodge a commitment.
            Advance(Duration * 0.5f);
            gameObject.transform.forward = Vector3.back;
            Advance(Duration);

            Assert.That(dodge.DodgeDirection.z, Is.EqualTo(1f).Within(0.001f));
            Assert.That(gameObject.transform.position.z, Is.EqualTo(Distance).Within(0.001f),
                "The burst must finish in the direction it committed to, not the one the body turned to.");
        }

        // --- Invulnerability --------------------------------------------------------------------

        [Test]
        public void Dodge_IsInvulnerableForTheWholeBurstAndNotAfterIt()
        {
            dodge.TryStartDodge();
            Assert.That(health.IsInvulnerable, Is.True, "Invulnerability must begin on the frame the dodge does.");

            Advance(Duration - Frame * 2f);
            Assert.That(health.IsInvulnerable, Is.True, "The window must cover the whole movement.");

            Advance(Frame * 3f);
            Assert.That(health.IsInvulnerable, Is.False, "The window must close when the dodge ends.");
            Assert.That(dodge.IsInvulnerableFromDodge, Is.False);
        }

        [Test]
        public void Dodge_TakesNoDamageDuringItsWindow()
        {
            dodge.TryStartDodge();
            Advance(Duration * 0.5f);
            health.ReceiveDamage(new DamageInfo(25f, Vector3.forward * 3f));

            Assert.That(health.CurrentHealth, Is.EqualTo(100f), "A dodging player must not be hit.");

            Advance(Duration);
            health.ReceiveDamage(new DamageInfo(25f, Vector3.forward * 3f));
            Assert.That(health.CurrentHealth, Is.EqualTo(75f), "The player must be hittable again afterwards.");
        }

        /// <summary>
        /// The dodge is not the only thing that may ever hold this flag — the plan's debug panel asks
        /// for an invulnerability toggle — so ending a dodge must put back what it found rather than
        /// clearing the flag outright.
        /// </summary>
        [Test]
        public void Dodge_RestoresInvulnerabilityItDidNotOwn()
        {
            health.SetInvulnerable(true);

            dodge.TryStartDodge();
            Advance(Duration + Frame);

            Assert.That(health.IsInvulnerable, Is.True,
                "A dodge must not switch off invulnerability that something else turned on.");
        }

        // --- Cooldown, spam, and the held button ------------------------------------------------

        [Test]
        public void Dodge_RefusesASecondDodgeUntilTheCooldownElapses()
        {
            Assert.That(dodge.TryStartDodge(), Is.True);
            Advance(Duration);

            Advance(Cooldown - Duration - Frame * 2f);
            Assert.That(dodge.TryStartDodge(), Is.False, "The cooldown must still be running.");

            Advance(Frame * 3f);
            Assert.That(dodge.TryStartDodge(), Is.True, "The dodge must be legal the frame the cooldown ends.");
        }

        [Test]
        public void Dodge_SpamCannotBeatTheCadence()
        {
            var dodges = 0;
            dodge.DodgeStarted += _ => dodges++;

            // A press on every single frame for three seconds, which is the worst a player can do.
            for (var frame = 0; frame < Mathf.RoundToInt(3f / Frame); frame++)
            {
                dodge.BufferDodge();
                dodge.Tick(Frame);
            }

            Assert.That(dodges, Is.EqualTo(3),
                "Three seconds at a one second cooldown is three dodges, however hard the button is hit.");
        }

        /// <summary>
        /// The dodge reads the press edge rather than the held state, so leaning on the button is
        /// exactly one dodge. Without this, holding A would dodge once per cooldown forever.
        /// </summary>
        [Test]
        public void Dodge_HoldingTheButtonProducesOneDodge()
        {
            var dodges = 0;
            dodge.DodgeStarted += _ => dodges++;

            // One press, then three seconds of nothing but the button still being down.
            dodge.BufferDodge();
            for (var frame = 0; frame < Mathf.RoundToInt(3f / Frame); frame++)
            {
                dodge.Tick(Frame);
            }

            Assert.That(dodges, Is.EqualTo(1), "A held button must not re-arm the dodge.");
        }

        [Test]
        public void Dodge_CannotStartASecondBurstWhileOneIsRunning()
        {
            Assert.That(dodge.TryStartDodge(), Is.True);
            Advance(Duration * 0.5f);

            Assert.That(dodge.TryStartDodge(), Is.False, "A dodge in flight must not be restartable.");
            Assert.That(dodge.IsDodging, Is.True);
        }

        // --- The input buffer -------------------------------------------------------------------

        [Test]
        public void Dodge_FiresABufferedPressOnTheFirstLegalFrame()
        {
            dodge.TryStartDodge();
            Advance(Cooldown - Frame * 4f);

            dodge.BufferDodge();
            Assert.That(dodge.IsDodging, Is.False, "The press arrived before the cooldown ended.");

            Advance(Frame * 5f);
            Assert.That(dodge.IsDodging, Is.True,
                "A press made slightly too early must be remembered, not discarded.");
        }

        [Test]
        public void Dodge_LetsAStalePressExpireInsteadOfFiringLate()
        {
            dodge.TryStartDodge();
            Advance(Duration);

            // Pressed with most of a second of cooldown left: far outside the buffer window.
            dodge.BufferDodge();
            Advance(Cooldown);

            Assert.That(dodge.IsDodging, Is.False,
                "A press made long before the dodge was legal must not fire once it becomes legal.");
        }

        // --- Conflicts with the other verbs -----------------------------------------------------

        [Test]
        public void Dodge_TakesTheActionClaimAndReleasesItWhenTheBurstEnds()
        {
            dodge.TryStartDodge();
            Assert.That(actions.State.HasFlag(PlayerActionState.Dodging), Is.True);

            Advance(Duration + Frame);
            Assert.That(actions.State.HasFlag(PlayerActionState.Dodging), Is.False,
                "A finished dodge must give the claim back or nothing else can ever start.");
        }

        [Test]
        public void Dodge_CancelsARaisedShieldImmediately()
        {
            var shield = gameObject.AddComponent<ShieldController>();
            shield.Configure(null, null, actions, null, null, health, null);

            shield.Tick(Frame, true, Vector2.zero);
            Assert.That(shield.IsRaised, Is.True);

            dodge.TryStartDodge();
            Assert.That(actions.State.HasFlag(PlayerActionState.Shielding), Is.False,
                "The shield claim must be gone on the frame the dodge starts.");

            shield.Tick(Frame, true, Vector2.zero);
            Assert.That(shield.IsRaised, Is.False, "The shield must come down even with the button still held.");
        }

        /// <summary>
        /// The plan's default for Phase 8 task 6. The swing's active window is the only part of melee
        /// that outranks a dodge, and because the press is buffered across it the player is delayed by
        /// the authored active duration rather than having their input eaten.
        /// </summary>
        [Test]
        public void Dodge_WaitsOutTheMeleeActiveWindowInsteadOfEatingThePress()
        {
            var melee = gameObject.AddComponent<MeleeController>();
            melee.Configure(null, null, actions, null, health, null);
            dodge.Configure(null, null, actions, null, null, health, melee);

            Assert.That(melee.TryStartSwing(), Is.True);
            melee.Tick(0.07f);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Active), "The swing should be dealing damage now.");

            dodge.BufferDodge();
            dodge.Tick(Frame);
            Assert.That(dodge.IsDodging, Is.False,
                "A dodge must not cancel a swing that is already dealing damage.");

            melee.Tick(0.09f);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Recovery));

            dodge.Tick(Frame);
            Assert.That(dodge.IsDodging, Is.True,
                "Recovery is interruptible, and the buffered press must fire on the first legal frame.");
        }

        [Test]
        public void Dodge_InterruptsMeleeStartupOutright()
        {
            var melee = gameObject.AddComponent<MeleeController>();
            melee.Configure(null, null, actions, null, health, null);
            dodge.Configure(null, null, actions, null, null, health, melee);

            melee.TryStartSwing();
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Startup));

            Assert.That(dodge.TryStartDodge(), Is.True, "A swing that has not connected yet is interruptible.");

            melee.Tick(Frame);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Ready), "The swing must be cancelled by the dodge.");
        }

        [Test]
        public void Dodge_IsRefusedWhileTheCoordinatorIsBlocked()
        {
            health.ReceiveDamage(new DamageInfo(200f, Vector3.forward));
            Assert.That(health.IsDead, Is.True);

            Assert.That(dodge.TryStartDodge(), Is.False, "A dead player must not dodge.");
        }

        [Test]
        public void Dodge_CancelledMidBurstReleasesEverythingAndStillOwesTheCooldown()
        {
            dodge.TryStartDodge();
            Advance(Duration * 0.25f);

            dodge.CancelDodge();

            Assert.That(dodge.IsDodging, Is.False);
            Assert.That(health.IsInvulnerable, Is.False, "A cancelled dodge must not leave the player invulnerable.");
            Assert.That(actions.State.HasFlag(PlayerActionState.Dodging), Is.False);
            Assert.That(dodge.TryStartDodge(), Is.False,
                "Refunding the cooldown would make cancelling straight into another dodge free.");
        }

        /// <summary>
        /// A player cannot normally die mid-dodge, because the whole burst is invulnerable. The
        /// window is tuned away here so the death path itself is exercised rather than assumed.
        /// </summary>
        [Test]
        public void Dodge_EndsOnDeathWithoutLeavingThePlayerStuck()
        {
            var balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(balance);
                serialized.FindProperty("dodgeInvulnerability").floatValue = 0f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                dodge.Configure(balance, null, actions, null, null, health, null);

                dodge.TryStartDodge();
                Advance(Duration * 0.25f);
                Assert.That(dodge.IsDodging, Is.True, "The burst should still be running.");

                health.ReceiveDamage(new DamageInfo(200f, Vector3.forward));

                Assert.That(health.IsDead, Is.True);
                Assert.That(dodge.IsDodging, Is.False, "Death must end the burst.");
                Assert.That(health.IsInvulnerable, Is.False, "A dead player must not stay invulnerable from a dodge.");
                Assert.That(actions.State.HasFlag(PlayerActionState.Dodging), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(balance);
            }
        }

        [Test]
        public void Dodge_IsFullyResetWhenHealthIsReset()
        {
            dodge.TryStartDodge();
            Advance(Duration * 0.5f);

            health.ResetHealth();

            Assert.That(dodge.IsDodging, Is.False, "A revived player must not still be mid-dodge.");
            Assert.That(dodge.CooldownRemaining, Is.EqualTo(0f), "A new life must not owe the last life's cooldown.");
            Assert.That(health.IsInvulnerable, Is.False);
            Assert.That(dodge.TryStartDodge(), Is.True, "A revived player must be able to dodge immediately.");
        }

        // --- Tuning ------------------------------------------------------------------------------

        [Test]
        public void PrototypeBalance_ShipsTheDocumentedDodgeValues()
        {
            var balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                Assert.That(balance.DodgeDistance, Is.EqualTo(3f));
                Assert.That(balance.DodgeDuration, Is.EqualTo(0.2f));
                Assert.That(balance.DodgeCooldown, Is.EqualTo(1f));

                Assert.That(balance.DodgeInvulnerability, Is.GreaterThanOrEqualTo(balance.DodgeDuration),
                    "The window must cover the whole movement, or the player is hittable mid-dodge.");
                Assert.That(balance.DodgeInputBuffer, Is.LessThan(balance.DodgeCooldown),
                    "A buffer as long as the cooldown would queue a spare dodge instead of forgiving an early press.");
                Assert.That(balance.DodgeCooldown, Is.GreaterThan(balance.DodgeDuration),
                    "A cooldown shorter than the burst would make the dodge a second movement speed.");
                Assert.That(balance.DodgeAverageSpeed, Is.GreaterThan(balance.MoveSpeed * 2f),
                    "A dodge that is not clearly faster than running solves nothing that running does not.");
            }
            finally
            {
                Object.DestroyImmediate(balance);
            }
        }

        [Test]
        public void DodgeController_TakesItsDistanceFromTheBalanceAssetAndNotItsFallback()
        {
            var balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(balance);
                serialized.FindProperty("dodgeDistance").floatValue = 8f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                dodge.Configure(balance, null, actions, null, null, health, null);
                Assert.That(dodge.DodgeDistance, Is.EqualTo(8f));

                gameObject.transform.position = Vector3.zero;
                gameObject.transform.forward = Vector3.forward;
                dodge.TryStartDodge();
                Advance(Duration);

                Assert.That(gameObject.transform.position.z, Is.EqualTo(8f).Within(0.001f),
                    "The asset's distance is the one in force, not the serialized fallback.");
            }
            finally
            {
                Object.DestroyImmediate(balance);
            }
        }
    }
}
