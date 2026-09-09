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
    /// The shield where it meets the rest of combat: a real projectile arriving at a real body, and
    /// the fire-versus-defence exclusion driven through the actual controllers rather than through
    /// the coordinator directly. The plan calls out "a front projectile drains shield, a rear
    /// projectile drains health" as the test that proves the shield is directional at all.
    /// </summary>
    public sealed class ShieldPlayModeTests
    {
        private const float Frame = 1f / 60f;
        private const float ProjectileDamage = 8f;

        private readonly List<GameObject> spawned = new();
        private ProjectilePool pool;
        private GameObject shooter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return IsolatedTestBed.Load();

            var template = Track(new GameObject("ProjectileTemplate") { layer = GameLayers.PlayerProjectile });
            var projectilePrefab = template.AddComponent<Projectile>();
            projectilePrefab.Configure(0.12f);
            template.SetActive(false);

            shooter = Track(new GameObject("Shooter") { layer = GameLayers.Player });
            pool = shooter.AddComponent<ProjectilePool>();
            pool.Configure(projectilePrefab, 8, 0.12f);
            pool.Initialize();
            pool.enabled = false;
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

        [UnityTest]
        public IEnumerator ShieldBandHasIndependentFiniteNormalsOnBothVisibleSides()
        {
            var defender=CreateDefender(Vector3.zero,Vector3.forward);
            defender.Shield.gameObject.AddComponent<ShieldPresentation>().Configure(defender.Shield,null,null,.15f);
            yield return null;
            var mesh=defender.Shield.transform.Find("ShieldArc/Band").GetComponent<MeshFilter>().sharedMesh;
            var normals=mesh.normals;var count=normals.Length/2;
            Assert.That(count,Is.GreaterThan(0));
            for(var i=0;i<count;i++)
            {
                Assert.That(normals[i].magnitude,Is.EqualTo(1).Within(.001f));
                Assert.That(normals[i+count].magnitude,Is.EqualTo(1).Within(.001f));
                Assert.That(Vector3.Dot(normals[i],normals[i+count]),Is.LessThan(-.99f));
            }
        }

        [UnityTest]
        public IEnumerator FrontProjectile_DrainsTheShieldAndLeavesHealthWhole()
        {
            var defender = CreateDefender(new Vector3(0f, 1f, 5f), facing: Vector3.back);
            yield return SyncPhysics();

            FireAtDefender();

            Assert.That(defender.Health.CurrentHealth, Is.EqualTo(100f), "A blocked shot must not reach health.");
            Assert.That(defender.Shield.Stability, Is.EqualTo(90f), "A blocked shot must cost stability.");
        }

        [UnityTest]
        public IEnumerator RearProjectile_DrainsHealthAndLeavesTheShieldWhole()
        {
            // Same shot, same raised shield: only the direction it is facing differs.
            var defender = CreateDefender(new Vector3(0f, 1f, 5f), facing: Vector3.forward);
            yield return SyncPhysics();

            FireAtDefender();

            Assert.That(defender.Health.CurrentHealth, Is.EqualTo(100f - ProjectileDamage),
                "A shot from behind must reach health.");
            Assert.That(defender.Shield.Stability, Is.EqualTo(100f), "A shot from behind must not cost stability.");
        }

        [UnityTest]
        public IEnumerator LoweredShield_LetsAFrontalProjectileThrough()
        {
            var defender = CreateDefender(new Vector3(0f, 1f, 5f), facing: Vector3.back, raise: false);
            yield return SyncPhysics();

            FireAtDefender();

            Assert.That(defender.Health.CurrentHealth, Is.EqualTo(100f - ProjectileDamage));
            Assert.That(defender.Shield.Stability, Is.EqualTo(100f));
        }

        [UnityTest]
        public IEnumerator RepeatedFrontalFire_BreaksTheShieldAndThenReachesHealth()
        {
            var defender = CreateDefender(new Vector3(0f, 1f, 5f), facing: Vector3.back);
            yield return SyncPhysics();

            // Ten light blocks is exactly a full bar, so the tenth breaks it. The shield is
            // deliberately not ticked between shots: any regeneration would blunt the arithmetic.
            for (var shot = 0; shot < 10; shot++)
            {
                FireAtDefender();
            }

            Assert.That(defender.Shield.State, Is.EqualTo(ShieldState.Broken));
            Assert.That(defender.Health.CurrentHealth, Is.EqualTo(100f), "Nothing may leak through while it holds.");

            FireAtDefender();
            Assert.That(defender.Health.CurrentHealth, Is.EqualTo(100f - ProjectileDamage),
                "Once broken, the next shot reaches health.");
        }

        /// <summary>
        /// The exclusion driven end to end: the real ranged controller, the real shield controller,
        /// and the coordinator between them. This is the mechanic the whole design turns on.
        /// </summary>
        [UnityTest]
        public IEnumerator RaisingTheShield_StopsAnActiveFiringSequence()
        {
            var rig = CreatePlayerRig();
            yield return null;

            rig.Ranged.Tick(Frame, true);
            Assert.That(rig.Ranged.ShotsFired, Is.EqualTo(1));
            Assert.That(rig.Ranged.IsFiring, Is.True);

            rig.Shield.Tick(Frame, true, Vector2.zero);
            Assert.That(rig.Shield.IsRaised, Is.True);

            for (var frame = 0; frame < 120; frame++)
            {
                rig.Ranged.Tick(Frame, true);
                rig.Shield.Tick(Frame, true, Vector2.zero);
            }

            Assert.That(rig.Ranged.IsFiring, Is.False, "Fire must not continue under a raised shield.");
            Assert.That(rig.Ranged.ShotsFired, Is.EqualTo(1), "Two seconds of held trigger produced no further shots.");
            Assert.That(rig.Shield.IsRaised, Is.True, "The shield keeps priority for as long as it is held.");
        }

        [UnityTest]
        public IEnumerator LoweringTheShield_LetsHeldFireResumeByItself()
        {
            var rig = CreatePlayerRig();
            yield return null;

            rig.Shield.Tick(Frame, true, Vector2.zero);
            for (var frame = 0; frame < 60; frame++)
            {
                rig.Ranged.Tick(Frame, true);
                rig.Shield.Tick(Frame, true, Vector2.zero);
            }

            Assert.That(rig.Ranged.ShotsFired, Is.Zero);

            rig.Shield.Tick(Frame, false, Vector2.zero);
            rig.Ranged.Tick(Frame, true);

            Assert.That(rig.Ranged.ShotsFired, Is.EqualTo(1), "The trigger was never released, so fire resumes.");
        }

        [UnityTest]
        public IEnumerator StartingMelee_DropsARaisedShieldAndItReturnsAfterTheSwing()
        {
            var rig = CreatePlayerRig();
            yield return null;

            rig.Shield.Tick(Frame, true, Vector2.zero);
            Assert.That(rig.Shield.IsRaised, Is.True);

            Assert.That(rig.Melee.TryStartSwing(), Is.True, "Melee must be usable while shielding.");
            rig.Shield.Tick(Frame, true, Vector2.zero);
            Assert.That(rig.Shield.IsRaised, Is.False, "The swing must visibly drop the shield.");

            // Run the swing out: startup, active, recovery.
            rig.Melee.Tick(0.06f);
            rig.Melee.Tick(0.08f);
            rig.Melee.Tick(0.16f);

            rig.Shield.Tick(Frame, true, Vector2.zero);
            Assert.That(rig.Shield.IsRaised, Is.True, "It comes back once the swing is over and the button is still held.");
        }

        [UnityTest]
        public IEnumerator RaisedShield_SlowsMovementWithoutTouchingTheAuthoredSpeed()
        {
            var rig = CreatePlayerRig();
            yield return null;

            var authoredSpeed = rig.Motor.MaximumSpeed;

            rig.Shield.Tick(Frame, true, Vector2.zero);
            Assert.That(rig.Motor.EffectiveSpeed, Is.LessThan(authoredSpeed));
            Assert.That(rig.Motor.MaximumSpeed, Is.EqualTo(authoredSpeed), "The tuning value must stay recoverable.");

            rig.Shield.Tick(Frame, false, Vector2.zero);
            Assert.That(rig.Motor.EffectiveSpeed, Is.EqualTo(authoredSpeed));
        }

        private void FireAtDefender()
        {
            var projectile = pool.Fire(new Vector3(0f, 1.2f, 0f), Vector3.forward, 20f, ProjectileDamage, 1.5f, shooter);
            Assert.That(projectile, Is.Not.Null, "The shot was dropped.");

            for (var step = 0; step < 240 && pool.ActiveCount > 0; step++)
            {
                pool.Tick(Frame);
            }

            Assert.That(pool.ActiveCount, Is.Zero, "The projectile never resolved.");
        }

        private Defender CreateDefender(Vector3 position, Vector3 facing, bool raise = true)
        {
            var body = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            body.name = "Defender";
            body.layer = GameLayers.Enemy;
            body.transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing, Vector3.up));

            var health = body.AddComponent<Health>();
            health.Configure(100f);

            var shield = body.AddComponent<ShieldController>();
            shield.Configure(null, null, null, null, null, health, null);
            shield.enabled = false;

            if (raise)
            {
                shield.Tick(Frame, true, Vector2.zero);
                Assert.That(shield.IsRaised, Is.True);
            }

            return new Defender(health, shield);
        }

        private PlayerRig CreatePlayerRig()
        {
            var rig = Track(new GameObject("PlayerRig") { layer = GameLayers.Player });
            rig.AddComponent<CharacterController>();

            var health = rig.AddComponent<Health>();
            health.Configure(100f);
            var actions = rig.AddComponent<PlayerActionCoordinator>();
            actions.Configure(health);
            var motor = rig.AddComponent<PlayerMotor>();

            var melee = rig.AddComponent<MeleeController>();
            melee.Configure(null, null, actions, null, health, null);

            var ranged = rig.AddComponent<RangedController>();
            ranged.Configure(null, null, actions, null, health, null, pool);

            var shield = rig.AddComponent<ShieldController>();
            shield.Configure(null, null, actions, null, motor, health, null);

            // Every loop is stepped by hand so the exclusion is observed on an exact frame.
            motor.enabled = false;
            melee.enabled = false;
            ranged.enabled = false;
            shield.enabled = false;

            return new PlayerRig(motor, melee, ranged, shield);
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

        private readonly struct Defender
        {
            public readonly Health Health;
            public readonly ShieldController Shield;

            public Defender(Health health, ShieldController shield)
            {
                Health = health;
                Shield = shield;
            }
        }

        private readonly struct PlayerRig
        {
            public readonly PlayerMotor Motor;
            public readonly MeleeController Melee;
            public readonly RangedController Ranged;
            public readonly ShieldController Shield;

            public PlayerRig(
                PlayerMotor motor,
                MeleeController melee,
                RangedController ranged,
                ShieldController shield)
            {
                Motor = motor;
                Melee = melee;
                Ranged = ranged;
                Shield = shield;
            }
        }
    }
}
