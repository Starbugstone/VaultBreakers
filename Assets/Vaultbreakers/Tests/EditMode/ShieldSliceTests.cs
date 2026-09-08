using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Player;

namespace Vaultbreakers.Tests.EditMode
{
    /// <summary>
    /// The arc maths and the shield state machine. "Shield becomes omnidirectional" is a named risk
    /// for this phase, so every boundary the plan lists — centre, exact edge, just outside, rear, the
    /// zero vector, and the wrap through north — is asserted here rather than eyeballed in play.
    /// </summary>
    public sealed class ShieldSliceTests
    {
        private const float Arc = 120f;
        private const float Frame = 1f / 60f;

        private GameObject gameObject;
        private Health health;
        private PlayerActionCoordinator actions;
        private ShieldController shield;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("ShieldTest");
            health = gameObject.AddComponent<Health>();
            health.Configure(100f);
            actions = gameObject.AddComponent<PlayerActionCoordinator>();
            actions.Configure(health);
            shield = gameObject.AddComponent<ShieldController>();
            shield.Configure(null, null, actions, null, null, health, null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(gameObject);

        private void Hold(int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                shield.Tick(Frame, true, Vector2.zero);
            }
        }

        private void Release(int frames)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                shield.Tick(Frame, false, Vector2.zero);
            }
        }

        /// <summary>A hit arriving from the given bearing, four units out.</summary>
        private static DamageInfo HitFrom(float degrees, float amount = 10f, float stability = 0f)
        {
            var origin = Quaternion.Euler(0f, degrees, 0f) * Vector3.forward * 4f;
            return new DamageInfo(amount, origin, null, Vector3.zero, stability);
        }

        [Test]
        public void Arc_BlocksDeadCentre()
        {
            Assert.That(ShieldArc.IsWithinArc(Vector3.forward, Vector3.zero, Vector3.forward * 3f, Arc), Is.True);
        }

        [Test]
        public void Arc_BlocksTheExactEdgeAndNotAHairBeyondIt()
        {
            var onEdge = Quaternion.Euler(0f, Arc * 0.5f, 0f) * Vector3.forward * 3f;
            var pastEdge = Quaternion.Euler(0f, Arc * 0.5f + 0.5f, 0f) * Vector3.forward * 3f;

            Assert.That(ShieldArc.IsWithinArc(Vector3.forward, Vector3.zero, onEdge, Arc), Is.True,
                "The edge of the arc is inside it.");
            Assert.That(ShieldArc.IsWithinArc(Vector3.forward, Vector3.zero, pastEdge, Arc), Is.False,
                "Half a degree outside the arc must not block.");
        }

        [Test]
        public void Arc_DoesNotBlockFromBehind()
        {
            Assert.That(ShieldArc.IsWithinArc(Vector3.forward, Vector3.zero, Vector3.back * 3f, Arc), Is.False);
        }

        /// <summary>
        /// The case that turns a directional shield into an omnidirectional one if it is handled by
        /// accident: nothing to measure against must mean "cannot block", never "blocks everything".
        /// </summary>
        [Test]
        public void Arc_RefusesToBlockWhenThereIsNoDirectionToJudge()
        {
            Assert.That(ShieldArc.IsWithinArc(Vector3.forward, Vector3.zero, Vector3.zero, Arc), Is.False,
                "A source standing exactly on the defender is unshieldable.");
            Assert.That(ShieldArc.IsWithinArc(Vector3.zero, Vector3.zero, Vector3.forward * 3f, Arc), Is.False,
                "A shield with no facing has no arc.");
            Assert.That(ShieldArc.IsWithinArc(Vector3.forward, Vector3.zero, Vector3.up * 3f, Arc), Is.False,
                "A purely vertical offset flattens to nothing and cannot be judged.");
        }

        [Test]
        public void Arc_WrapsThroughNorthWithoutAGap()
        {
            var facing = Quaternion.Euler(0f, 350f, 0f) * Vector3.forward;
            var source = Quaternion.Euler(0f, 10f, 0f) * Vector3.forward * 3f;
            var outside = Quaternion.Euler(0f, 60f, 0f) * Vector3.forward * 3f;

            Assert.That(ShieldArc.IsWithinArc(facing, Vector3.zero, source, Arc), Is.True,
                "Twenty degrees apart across the zero crossing is well inside a 120 degree arc.");
            Assert.That(ShieldArc.IsWithinArc(facing, Vector3.zero, outside, Arc), Is.False);
        }

        [Test]
        public void Arc_IgnoresHeightDifferences()
        {
            var high = new Vector3(0f, 12f, 3f);
            Assert.That(ShieldArc.IsWithinArc(Vector3.forward, Vector3.zero, high, Arc), Is.True);
        }

        [Test]
        public void Arc_TreatsAHitWithNoKnownSourceAsUnshieldable()
        {
            var sourceless = new DamageInfo(10f);
            Assert.That(sourceless.HasSourcePosition, Is.False);
            Assert.That(ShieldArc.CanBlock(sourceless, Vector3.forward, Vector3.zero, Arc), Is.False);
        }

        [Test]
        public void Raising_ClaimsTheActionAndLoweringGivesItBack()
        {
            shield.Tick(Frame, true, Vector2.zero);
            Assert.That(shield.State, Is.EqualTo(ShieldState.Raised));
            Assert.That(actions.State.HasFlag(PlayerActionState.Shielding), Is.True);

            shield.Tick(Frame, false, Vector2.zero);
            Assert.That(shield.State, Is.EqualTo(ShieldState.Lowered));
            Assert.That(actions.State.HasFlag(PlayerActionState.Shielding), Is.False);
        }

        [Test]
        public void FrontalHit_DrainsStabilityAndLeavesHealthAlone()
        {
            Hold(1);

            var result = health.ReceiveDamage(HitFrom(0f));

            Assert.That(result.Blocked, Is.True);
            Assert.That(result.WasApplied, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
            Assert.That(shield.Stability, Is.EqualTo(90f), "A hit with no declared stability cost is a light hit.");
        }

        [Test]
        public void RearHit_DrainsHealthAndLeavesStabilityAlone()
        {
            Hold(1);

            var result = health.ReceiveDamage(HitFrom(180f));

            Assert.That(result.Blocked, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(90f));
            Assert.That(shield.Stability, Is.EqualTo(100f));
        }

        [Test]
        public void LoweredShield_BlocksNothingEvenFromDeadAhead()
        {
            health.ReceiveDamage(HitFrom(0f));

            Assert.That(health.CurrentHealth, Is.EqualTo(90f));
            Assert.That(shield.Stability, Is.EqualTo(100f));
        }

        [Test]
        public void HeavyHit_DrainsTheStabilityItDeclares()
        {
            Hold(1);

            health.ReceiveDamage(HitFrom(0f, 20f, 30f));

            Assert.That(shield.Stability, Is.EqualTo(70f));
            Assert.That(health.CurrentHealth, Is.EqualTo(100f));
        }

        /// <summary>
        /// Coming up short costs the break, not leaked damage. A shield that let part of a hit through
        /// would make the moment of breaking impossible to read.
        /// </summary>
        [Test]
        public void AHitLargerThanTheRemainingStability_IsStillBlockedInFull()
        {
            Hold(1);
            health.ReceiveDamage(HitFrom(0f, 5f, 95f));
            Assert.That(shield.Stability, Is.EqualTo(5f));

            var result = health.ReceiveDamage(HitFrom(0f, 40f, 30f));

            Assert.That(result.Blocked, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(100f), "No damage may leak through the breaking hit.");
            Assert.That(shield.State, Is.EqualTo(ShieldState.Broken));
        }

        /// <summary>
        /// A break costs exactly the lockout and nothing after it. The shield refills throughout, so
        /// when the wait ends the player gets a guard worth having back rather than a token one — and
        /// the punishment is one number the player can learn.
        /// </summary>
        [Test]
        public void Breaking_CostsExactlyTheLockoutAndReturnsAUsableShield()
        {
            Hold(1);
            shield.Break();

            Assert.That(shield.State, Is.EqualTo(ShieldState.Broken));
            Assert.That(shield.Stability, Is.Zero);
            Assert.That(actions.State.HasFlag(PlayerActionState.ShieldBroken), Is.True);
            Assert.That(actions.TryStart(PlayerAction.Shield), Is.False, "A broken shield cannot be raised.");

            // Just short of the 2.5 second lockout.
            Release(149);
            Assert.That(shield.State, Is.EqualTo(ShieldState.Broken), "The lockout is not over yet.");
            Assert.That(shield.IsRaised, Is.False);

            Release(2);
            Assert.That(shield.State, Is.EqualTo(ShieldState.Recovering), "The lockout ends and the shield is back.");
            Assert.That(actions.State.HasFlag(PlayerActionState.ShieldBroken), Is.False);
            Assert.That(shield.Stability, Is.EqualTo(30f).Within(0.5f),
                "It refilled at the lowered rate throughout the lockout, so it returns with a real charge.");

            shield.Tick(Frame, true, Vector2.zero);
            Assert.That(shield.State, Is.EqualTo(ShieldState.Raised), "And it is immediately usable.");
        }

        [Test]
        public void Recovering_IsAUsableStateThatBecomesLoweredOnceStabilityIsFull()
        {
            Hold(1);
            health.ReceiveDamage(HitFrom(0f, 10f, 50f));
            Release(1);

            Assert.That(shield.State, Is.EqualTo(ShieldState.Recovering), "Down but not whole is Recovering.");
            Assert.That(shield.TryAbsorb(new DamageInfo(10f, Vector3.forward * 4f)), Is.False,
                "A shield that is down blocks nothing, whatever state it reports.");

            Release(300);
            Assert.That(shield.State, Is.EqualTo(ShieldState.Lowered), "Full again is Lowered.");
            Assert.That(shield.Stability, Is.EqualTo(100f));
        }

        [Test]
        public void RecoveryProgress_RunsOnceFromZeroToOneAcrossTheLockout()
        {
            shield.Break();
            Assert.That(shield.RecoveryProgress, Is.EqualTo(0f).Within(0.01f));

            // A couple of frames past the 2.5 second lockout: summing per-frame deltas leaves the
            // remainder a hair either side of zero, so the exact frame it ends on is not the point.
            var previous = shield.RecoveryProgress;
            for (var frame = 0; frame < 155; frame++)
            {
                shield.Tick(Frame, false, Vector2.zero);
                Assert.That(shield.RecoveryProgress, Is.GreaterThanOrEqualTo(previous - 0.0001f),
                    "The wait must never appear to go backwards at frame " + frame + ".");
                previous = shield.RecoveryProgress;
            }

            Assert.That(shield.IsUnavailable, Is.False, "A full bar means the guard is back.");
            Assert.That(shield.RecoveryProgress, Is.EqualTo(1f));
        }

        [Test]
        public void Regeneration_RunsFasterLoweredThanRaised()
        {
            Hold(1);
            health.ReceiveDamage(HitFrom(0f, 10f, 50f));
            Assert.That(shield.Stability, Is.EqualTo(50f));

            Hold(60);
            var raisedGain = shield.Stability - 50f;
            Assert.That(raisedGain, Is.EqualTo(4f).Within(0.1f), "Raised regeneration is 4 per second.");

            Release(60);
            var loweredGain = shield.Stability - 50f - raisedGain;
            Assert.That(loweredGain, Is.EqualTo(12f).Within(0.1f), "Lowered regeneration is 12 per second.");
        }

        [Test]
        public void RaisingTheShield_AppliesTheMovementPenaltyAndLoweringRemovesIt()
        {
            var motorObject = new GameObject("Motor");
            try
            {
                motorObject.AddComponent<CharacterController>();
                var motor = motorObject.AddComponent<PlayerMotor>();
                shield.Configure(null, null, actions, null, motor, health, null);

                shield.Tick(Frame, true, Vector2.zero);
                Assert.That(motor.SpeedMultiplier, Is.EqualTo(0.85f).Within(0.001f));
                Assert.That(motor.EffectiveSpeed, Is.LessThan(motor.MaximumSpeed));

                shield.Tick(Frame, false, Vector2.zero);
                Assert.That(motor.SpeedMultiplier, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(motorObject);
            }
        }

        [Test]
        public void Melee_SuppressesARaisedShieldAndItReturnsWhenTheSwingEnds()
        {
            Hold(1);
            Assert.That(shield.IsRaised, Is.True);

            Assert.That(actions.TryStart(PlayerAction.Melee), Is.True, "Melee outranks a raised shield.");
            shield.Tick(Frame, true, Vector2.zero);
            Assert.That(shield.IsRaised, Is.False, "The shield must drop on the frame melee takes the claim.");

            Hold(10);
            Assert.That(shield.IsRaised, Is.False, "It must stay down for the whole swing.");

            actions.Stop(PlayerAction.Melee);
            shield.Tick(Frame, true, Vector2.zero);
            Assert.That(shield.IsRaised, Is.True, "Still holding the button, so it comes back up.");
        }

        [Test]
        public void Dodge_CancelsARaisedShield()
        {
            Hold(1);
            Assert.That(actions.TryStart(PlayerAction.Dodge), Is.True);

            shield.Tick(Frame, true, Vector2.zero);

            Assert.That(shield.IsRaised, Is.False);
        }

        [Test]
        public void Death_DropsARaisedShieldButDoesNotRepairABrokenOne()
        {
            Hold(1);
            health.ReceiveDamage(HitFrom(180f, 100f));

            Assert.That(shield.IsRaised, Is.False, "Death cancels every active action.");

            health.ResetHealth();
            actions.ResetState();
            shield.Tick(Frame, true, Vector2.zero);
            Assert.That(shield.IsRaised, Is.True);

            shield.Break();
            health.ReceiveDamage(HitFrom(180f, 100f));
            Assert.That(shield.State, Is.EqualTo(ShieldState.Broken),
                "Dying must not be a way to skip the cost of a break.");
        }

        [Test]
        public void RevivingThePlayer_RestoresAWholeShield()
        {
            shield.Break();
            Assert.That(shield.Stability, Is.Zero);

            health.ResetHealth();

            Assert.That(shield.State, Is.EqualTo(ShieldState.Lowered));
            Assert.That(shield.Stability, Is.EqualTo(100f));
        }

        [Test]
        public void InvulnerablePlayer_DoesNotSpendStabilityBlockingHitsThatCouldNotLand()
        {
            Hold(1);
            health.SetInvulnerable(true);

            health.ReceiveDamage(HitFrom(0f));

            Assert.That(shield.Stability, Is.EqualTo(100f),
                "Invulnerability is checked before the shield, so no stability is wasted.");
        }

        [Test]
        public void ShieldFacing_StartsFromCombatFacingAndAnswersOnlyToAim()
        {
            shield.Tick(Frame, true, Vector2.zero);
            var raisedFacing = shield.ShieldFacing;

            // No aim: the shield holds its direction no matter how many frames pass.
            Hold(30);
            Assert.That(Vector3.Angle(shield.ShieldFacing, raisedFacing), Is.LessThan(0.01f),
                "Only deliberate aim may turn a raised shield; time alone must not.");

            // Aim below the dead zone is not deliberate.
            shield.Tick(Frame, true, new Vector2(0.1f, 0f));
            Assert.That(Vector3.Angle(shield.ShieldFacing, raisedFacing), Is.LessThan(0.01f));

            shield.Tick(Frame, true, new Vector2(1f, 0f));
            Assert.That(Vector3.Angle(shield.ShieldFacing, Vector3.right), Is.LessThan(0.01f),
                "Aim past the dead zone turns the shield.");
        }

        [Test]
        public void PrototypeBalance_ShipsTheDocumentedShieldValues()
        {
            var balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                Assert.That(balance.ShieldStability, Is.EqualTo(100f));
                Assert.That(balance.ShieldArcDegrees, Is.EqualTo(120f));
                Assert.That(balance.LightStabilityDamage, Is.EqualTo(10f));
                Assert.That(balance.HeavyStabilityDamage, Is.EqualTo(30f));
                Assert.That(balance.LoweredStabilityRegeneration, Is.EqualTo(12f));
                Assert.That(balance.RaisedStabilityRegeneration, Is.EqualTo(4f));
                Assert.That(balance.ShieldBreakLockout, Is.EqualTo(2.5f));
                Assert.That(balance.ShieldMoveMultiplier, Is.EqualTo(0.85f));
                Assert.That(balance.RaisedStabilityRegeneration, Is.LessThan(balance.LoweredStabilityRegeneration),
                    "Holding the shield up must be the slower way to recover, or lowering it is pointless.");

                Assert.That(balance.ShieldTotalRecoveryTime, Is.EqualTo(balance.ShieldBreakLockout),
                    "A break must cost one wait, not a lockout followed by a second invisible one.");
                Assert.That(balance.ShieldTotalRecoveryTime, Is.LessThanOrEqualTo(3f),
                    "Longer than about three seconds stops being a punish and starts being a pause.");
                Assert.That(balance.StabilityAfterBreak,
                    Is.GreaterThanOrEqualTo(balance.LightStabilityDamage * 2f),
                    "A returning shield must survive more than a single hit, or the wait bought nothing.");
            }
            finally
            {
                Object.DestroyImmediate(balance);
            }
        }

        [Test]
        public void ShieldController_TakesItsArcFromTheBalanceAssetAndNotItsFallback()
        {
            var balance = ScriptableObject.CreateInstance<PrototypeBalance>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(balance);
                serialized.FindProperty("shieldArc").floatValue = 60f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                shield.Configure(balance, null, actions, null, null, health, null);
                Assert.That(shield.ArcDegrees, Is.EqualTo(60f));

                shield.Tick(Frame, true, Vector2.zero);
                health.ReceiveDamage(HitFrom(45f));

                Assert.That(health.CurrentHealth, Is.EqualTo(90f),
                    "Forty-five degrees is outside a 60 degree arc, so the asset's value is the one in force.");
            }
            finally
            {
                Object.DestroyImmediate(balance);
            }
        }
    }
}
