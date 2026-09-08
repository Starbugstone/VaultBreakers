using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Player;

namespace Vaultbreakers.Tests.PlayMode
{
    /// <summary>
    /// The dodge where it meets real geometry. "Dodge never crosses an arena wall" is the first item
    /// on the Phase 8 exit gate and the named risk for the phase, and it cannot be answered by the
    /// EditMode suite: it is a claim about a <see cref="CharacterController"/> resolving against real
    /// colliders, so it is asserted against real colliders here.
    /// </summary>
    public sealed class DodgePlayModeTests
    {
        private const float Frame = 1f / 60f;
        private const float Distance = 3f;
        private const float Duration = 0.2f;

        private readonly List<GameObject> spawned = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return IsolatedTestBed.Load();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in spawned)
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }

            spawned.Clear();
        }

        // --- The wall ---------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Dodge_IsStoppedByAWallInsteadOfCrossingIt()
        {
            var rig = CreateRig(new Vector3(0f, 1f, 0f));

            // A wall one unit ahead of a three unit dodge: the burst must not reach the far side.
            CreateWall(new Vector3(0f, 1f, 1.5f), new Vector3(12f, 4f, 0.5f));
            yield return SyncPhysics();

            rig.Transform.forward = Vector3.forward;
            Assert.That(rig.Dodge.TryStartDodge(), Is.True);
            Step(rig, Duration + Frame);

            Assert.That(rig.Transform.position.z, Is.LessThan(1.5f),
                "The dodge crossed a wall it should have been stopped by.");
            Assert.That(rig.Dodge.DistanceTravelled, Is.LessThan(Distance),
                "A dodge stopped by geometry must report the distance it actually covered.");
            Assert.That(rig.Dodge.IsDodging, Is.False, "The dodge must still end on schedule at a wall.");
        }

        /// <summary>
        /// Two walls meeting at a right angle. A corner is where a displacement that merely checked
        /// the straight line to its destination would slip through, so it is worth its own test.
        /// </summary>
        [UnityTest]
        public IEnumerator Dodge_IsContainedByACorner()
        {
            var rig = CreateRig(new Vector3(0f, 1f, 0f));
            CreateWall(new Vector3(0f, 1f, 2f), new Vector3(12f, 4f, 0.5f));
            CreateWall(new Vector3(2f, 1f, 0f), new Vector3(0.5f, 4f, 12f));
            yield return SyncPhysics();

            // Straight into the inside of the corner.
            rig.Transform.forward = new Vector3(1f, 0f, 1f).normalized;
            rig.Dodge.TryStartDodge();
            Step(rig, Duration + Frame);

            var position = rig.Transform.position;
            Assert.That(position.z, Is.LessThan(2f), "The dodge escaped through the north wall of the corner.");
            Assert.That(position.x, Is.LessThan(2f), "The dodge escaped through the east wall of the corner.");
        }

        [UnityTest]
        public IEnumerator Dodge_CoversItsFullDistanceWhenNothingIsInTheWay()
        {
            var rig = CreateRig(new Vector3(0f, 1f, 0f));
            yield return SyncPhysics();

            rig.Transform.forward = Vector3.forward;
            rig.Dodge.TryStartDodge();
            Step(rig, Duration + Frame);

            Assert.That(rig.Transform.position.z, Is.EqualTo(Distance).Within(0.05f),
                "An unobstructed dodge must land on the authored distance through the character controller too.");
        }

        // --- Interaction with the other verbs ----------------------------------------------------

        /// <summary>
        /// The exit gate's "LT is no longer active after a dodge starts", driven through the real
        /// controllers rather than through the coordinator, and with the button still held down.
        /// </summary>
        [UnityTest]
        public IEnumerator Dodge_DropsARaisedShieldWithTheButtonStillHeld()
        {
            var rig = CreateRig(new Vector3(0f, 1f, 0f));
            yield return SyncPhysics();

            rig.Shield.Tick(Frame, true, Vector2.zero);
            Assert.That(rig.Shield.IsRaised, Is.True, "The rig refused to raise its shield.");

            rig.Dodge.TryStartDodge();
            rig.Shield.Tick(Frame, true, Vector2.zero);

            Assert.That(rig.Shield.IsRaised, Is.False, "A dodge must drop the guard even while LT is held.");
            Assert.That(rig.Motor.SpeedMultiplier, Is.EqualTo(1f),
                "The shield's movement penalty must be given back when the guard drops.");
        }

        [UnityTest]
        public IEnumerator Dodge_StopsAnActiveFiringSequenceAndLetsItResumeAfterwards()
        {
            var rig = CreateRig(new Vector3(0f, 1f, 0f));
            yield return SyncPhysics();

            rig.Ranged.Tick(Frame, true);
            Assert.That(rig.Ranged.IsFiring, Is.True, "The rig never started firing.");

            rig.Dodge.TryStartDodge();
            rig.Ranged.Tick(Frame, true);
            Assert.That(rig.Ranged.IsFiring, Is.False, "Fire must stop on the frame a dodge takes the claim.");

            Step(rig, Duration + Frame);
            rig.Ranged.Tick(Frame, true);
            Assert.That(rig.Ranged.IsFiring, Is.True,
                "A trigger still held must resume by itself once the dodge is over.");
        }

        /// <summary>
        /// The dodge owns displacement outright while it runs. Without this the burst would travel
        /// further when the player happened to be running into it, and the authored distance would
        /// only be true from a standstill.
        /// </summary>
        [UnityTest]
        public IEnumerator Dodge_SuspendsOrdinaryMovementForItsDurationAndGivesItBack()
        {
            var rig = CreateRig(new Vector3(0f, 1f, 0f));
            yield return SyncPhysics();

            Assert.That(rig.Motor.IsMovementSuspended, Is.False);

            rig.Dodge.TryStartDodge();
            Assert.That(rig.Motor.IsMovementSuspended, Is.True, "The motor must hand movement over for the burst.");

            Step(rig, Duration + Frame);
            Assert.That(rig.Motor.IsMovementSuspended, Is.False,
                "A finished dodge must give movement back, or the player is frozen for good.");
        }

        [UnityTest]
        public IEnumerator Dodge_IgnoresDamageForTheWholeBurstAndNotAfterIt()
        {
            var rig = CreateRig(new Vector3(0f, 1f, 0f));
            yield return SyncPhysics();

            rig.Dodge.TryStartDodge();

            // A hit delivered through the same API every attack in the game uses.
            rig.Health.ReceiveDamage(new DamageInfo(25f, new Vector3(0f, 1f, 6f)));
            Assert.That(rig.Health.CurrentHealth, Is.EqualTo(100f), "A dodging player must not be hit.");

            Step(rig, Duration + Frame);
            rig.Health.ReceiveDamage(new DamageInfo(25f, new Vector3(0f, 1f, 6f)));
            Assert.That(rig.Health.CurrentHealth, Is.EqualTo(75f),
                "The window must close when the burst does, or the dodge is a permanent answer.");
        }

        // --- Rig ----------------------------------------------------------------------------------

        private static void Step(Rig rig, float seconds)
        {
            var frames = Mathf.RoundToInt(seconds / Frame);
            for (var frame = 0; frame < frames; frame++)
            {
                rig.Dodge.Tick(Frame);
            }
        }

        private Rig CreateRig(Vector3 position)
        {
            var gameObject = Track(new GameObject("DodgeRig") { layer = GameLayers.Player });
            gameObject.transform.position = position;

            var controller = gameObject.AddComponent<CharacterController>();
            controller.radius = 0.42f;
            controller.height = 1.8f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.skinWidth = 0.04f;

            var health = gameObject.AddComponent<Health>();
            health.Configure(100f);
            var actions = gameObject.AddComponent<PlayerActionCoordinator>();
            actions.Configure(health);
            var motor = gameObject.AddComponent<PlayerMotor>();

            var ranged = gameObject.AddComponent<RangedController>();
            ranged.Configure(null, null, actions, null, health, null, null);

            var shield = gameObject.AddComponent<ShieldController>();
            shield.Configure(null, null, actions, null, motor, health, null);

            var dodge = gameObject.AddComponent<DodgeController>();
            dodge.Configure(null, null, actions, null, motor, health, null);

            // Every loop is stepped by hand so the burst is observed on exact frames rather than on
            // whatever the editor rendered at.
            motor.enabled = false;
            ranged.enabled = false;
            shield.enabled = false;
            dodge.enabled = false;

            return new Rig(gameObject.transform, health, motor, ranged, shield, dodge);
        }

        private void CreateWall(Vector3 position, Vector3 size)
        {
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.name = "Wall";
            wall.layer = GameLayers.Environment;
            wall.transform.position = position;
            wall.transform.localScale = size;
        }

        private GameObject Track(GameObject gameObject)
        {
            spawned.Add(gameObject);
            return gameObject;
        }

        private static IEnumerator SyncPhysics()
        {
            Physics.SyncTransforms();
            yield return null;
        }

        private readonly struct Rig
        {
            public readonly Transform Transform;
            public readonly Health Health;
            public readonly PlayerMotor Motor;
            public readonly RangedController Ranged;
            public readonly ShieldController Shield;
            public readonly DodgeController Dodge;

            public Rig(
                Transform transform,
                Health health,
                PlayerMotor motor,
                RangedController ranged,
                ShieldController shield,
                DodgeController dodge)
            {
                Transform = transform;
                Health = health;
                Motor = motor;
                Ranged = ranged;
                Shield = shield;
                Dodge = dodge;
            }
        }
    }
}
