using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;

namespace Vaultbreakers.Tests.PlayMode
{
    /// <summary>
    /// The half of ranged fire that needs real colliders: what a projectile sweeps into, that it
    /// resolves one hit and no more, and that sustained fire never allocates. The rig is built here so
    /// a failure points at the projectile rules rather than at scene content.
    /// </summary>
    public sealed class RangedPlayModeTests
    {
        private const float Frame = 1f / 60f;
        private const int PoolSize = 8;

        private readonly List<GameObject> spawned = new();
        private GameObject rig;
        private RangedController ranged;
        private ProjectilePool pool;
        private Health playerHealth;
        private PlayerActionCoordinator actions;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return IsolatedTestBed.Load();

            // An inactive template stands in for the generated prefab; Instantiate treats it the same.
            var template = Track(new GameObject("ProjectileTemplate") { layer = GameLayers.PlayerProjectile });
            var projectilePrefab = template.AddComponent<Projectile>();
            projectilePrefab.Configure(0.12f);
            template.SetActive(false);

            rig = Track(new GameObject("RangedRig") { layer = GameLayers.Player });
            playerHealth = rig.AddComponent<Health>();
            playerHealth.Configure(100f);
            actions = rig.AddComponent<PlayerActionCoordinator>();
            actions.Configure(playerHealth);

            pool = rig.AddComponent<ProjectilePool>();
            pool.Configure(projectilePrefab, PoolSize, 0.12f);
            pool.Initialize();

            ranged = rig.AddComponent<RangedController>();
            ranged.Configure(null, null, actions, null, playerHealth, null, pool);

            // Both loops are stepped by hand so travel distance per step is exact.
            pool.enabled = false;
            ranged.enabled = false;
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
        public IEnumerator ReducedFlashesHideExistingAndFutureEffectsWithoutChangingDamage()
        {
            var previous=Vaultbreakers.UI.FeedbackSettings.Flashes;
            try
            {
                Vaultbreakers.UI.FeedbackSettings.Flashes=true;
                rig.AddComponent<RangedPresentation>();
                var target=CreateTarget("Flash target",new Vector3(0,1,3));
                yield return SyncPhysics();
                var effects=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                    .Where(t=>t.name=="MuzzleFlash" || t.name.StartsWith("ImpactMark_")).ToArray();
                FireOneShot(Vector3.forward);StepUntilIdle();
                Assert.That(effects.Any(t=>t.name=="MuzzleFlash" && t.gameObject.activeSelf),Is.True);
                Assert.That(effects.Any(t=>t.name.StartsWith("ImpactMark_") && t.gameObject.activeSelf),Is.True);
                var damage=50-target.CurrentHealth;
                Vaultbreakers.UI.FeedbackSettings.Flashes=false;
                yield return null;yield return null;
                Assert.That(effects.All(t=>!t.gameObject.activeSelf),Is.True,"Turning flashes off must hide effects already on screen.");
                var before=target.CurrentHealth;
                FireOneShot(Vector3.forward);StepUntilIdle();
                Assert.That(effects.All(t=>!t.gameObject.activeSelf),Is.True,"New shots must respect reduced feedback.");
                Assert.That(before-target.CurrentHealth,Is.EqualTo(damage));
                Vaultbreakers.UI.FeedbackSettings.Flashes=true;
                FireOneShot(Vector3.forward);
                Assert.That(effects.Any(t=>t.name=="MuzzleFlash" && t.gameObject.activeSelf),Is.True);
            }
            finally{Vaultbreakers.UI.FeedbackSettings.Flashes=previous;}
        }

        [UnityTest]
        public IEnumerator OffsetArmBarrelConvergesOnTheAimedSurfaceAndStillStopsAtCover()
        {
            var muzzle=Track(new GameObject("Offset muzzle"));muzzle.transform.position=new Vector3(1.1f,1.2f,0);
            var sockets=rig.AddComponent<Vaultbreakers.Equipment.AvatarSocketRegistry>();
            sockets.Configure(new[]{new Vaultbreakers.Equipment.AvatarSocketBinding(Vaultbreakers.Equipment.AvatarSocketId.Muzzle,muzzle.transform)});
            ranged.Configure(null,null,actions,null,playerHealth,sockets,pool);
            var target=CreateTarget("Aimed target",new Vector3(0,1,6));yield return SyncPhysics();
            FireOneShot(Vector3.forward);StepUntilIdle();
            Assert.That(target.CurrentHealth,Is.EqualTo(50),"Baseline parallel barrel misses the centre aiming line.");
            ranged.SetMuzzleConvergence(true);
            FireOneShot(Vector3.forward);StepUntilIdle();
            Assert.That(target.CurrentHealth,Is.EqualTo(42),"The arm-offset correction must hit what the player aims at.");
            var wall=Track(GameObject.CreatePrimitive(PrimitiveType.Cube));wall.layer=GameLayers.Environment;
            wall.transform.position=new Vector3(0,1,3);wall.transform.localScale=new Vector3(3,3,.2f);yield return SyncPhysics();
            FireOneShot(Vector3.forward);StepUntilIdle();
            Assert.That(target.CurrentHealth,Is.EqualTo(42),"Barrel convergence must never shoot through cover.");
        }

        [UnityTest]
        public IEnumerator Projectile_DamagesATargetAndReturnsToThePool()
        {
            var target = CreateTarget("Target", new Vector3(0f, 1f, 3f));
            yield return SyncPhysics();

            FireOneShot(Vector3.forward);
            Assert.That(pool.ActiveCount, Is.EqualTo(1), "The shot produced no projectile.");

            StepUntilIdle();

            Assert.That(target.CurrentHealth, Is.EqualTo(42f), "The projectile dealt no damage.");
            Assert.That(pool.ActiveCount, Is.Zero, "The projectile never returned to the pool.");
            Assert.That(pool.FreeCount, Is.EqualTo(PoolSize));
        }

        [UnityTest]
        public IEnumerator Projectile_ResolvesOneHitAndDoesNotCarryOnThroughTheTargetBehindIt()
        {
            var near = CreateTarget("Near", new Vector3(0f, 1f, 3f));
            var far = CreateTarget("Far", new Vector3(0f, 1f, 6f));
            yield return SyncPhysics();

            FireOneShot(Vector3.forward);
            StepUntilIdle();

            Assert.That(near.CurrentHealth, Is.EqualTo(42f));
            Assert.That(far.CurrentHealth, Is.EqualTo(50f), "One projectile must not damage two bodies.");
        }

        [UnityTest]
        public IEnumerator Projectile_CountsAsASingleHitEvenAcrossManySteps()
        {
            var target = CreateTarget("Target", new Vector3(0f, 1f, 3f));
            var hits = 0;
            target.Damaged += (_, _) => hits++;
            yield return SyncPhysics();

            FireOneShot(Vector3.forward);
            StepUntilIdle();

            Assert.That(hits, Is.EqualTo(1));
        }

        /// <summary>
        /// The whole reason a projectile sweeps instead of relying on trigger callbacks. One long step
        /// travels straight past the target; a swept query still catches it.
        /// </summary>
        [UnityTest]
        public IEnumerator Projectile_DoesNotTunnelThroughATargetInOneLongStep()
        {
            var target = CreateTarget("Target", new Vector3(0f, 1f, 5f));
            yield return SyncPhysics();

            FireOneShot(Vector3.forward);
            pool.Tick(0.5f); // 10 units of travel in a single step, well past the target.

            Assert.That(target.CurrentHealth, Is.EqualTo(42f), "The projectile tunnelled through the target.");
            Assert.That(pool.ActiveCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Projectile_IsStoppedByArenaGeometryBeforeReachingATargetBehindIt()
        {
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.name = "Wall";
            wall.layer = GameLayers.Environment;
            wall.transform.position = new Vector3(0f, 1f, 3f);
            wall.transform.localScale = new Vector3(4f, 2f, 0.5f);

            var target = CreateTarget("BehindWall", new Vector3(0f, 1f, 5f));
            yield return SyncPhysics();

            FireOneShot(Vector3.forward);
            StepUntilIdle();

            Assert.That(target.CurrentHealth, Is.EqualTo(50f), "A wall must stop the shot.");
            Assert.That(pool.ActiveCount, Is.Zero, "The projectile must recycle when it hits geometry.");
        }

        [UnityTest]
        public IEnumerator Projectile_ExpiresAndReturnsToThePoolWhenItHitsNothing()
        {
            yield return SyncPhysics();

            FireOneShot(Vector3.forward);
            StepUntilIdle();

            Assert.That(pool.ActiveCount, Is.Zero, "A missed shot must not stay alive forever.");
            Assert.That(pool.FreeCount, Is.EqualTo(PoolSize));
        }

        [UnityTest]
        public IEnumerator SustainedFire_NeverAllocatesBeyondTheFixedPool()
        {
            var target = CreateTarget("Target", new Vector3(0f, 1f, 6f));
            target.Configure(100000f);
            yield return SyncPhysics();

            rig.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            for (var frame = 0; frame < 600; frame++)
            {
                ranged.Tick(Frame, true);
                pool.Tick(Frame);

                Assert.That(pool.ActiveCount + pool.FreeCount, Is.EqualTo(pool.Capacity),
                    "The pool changed size at frame " + frame + ".");
            }

            Assert.That(ranged.ShotsFired, Is.EqualTo(34));
            Assert.That(pool.RecycledUnderPressure, Is.Zero,
                "Ten seconds of held fire should never exhaust a pool of " + PoolSize + ".");
        }

        /// <summary>
        /// Dropping the player's input is the worse failure, so exhaustion steals the oldest shot in
        /// flight and says so, rather than silently refusing to fire.
        /// </summary>
        [UnityTest]
        public IEnumerator PoolExhaustion_RecyclesTheOldestShotRatherThanDroppingTheNewOne()
        {
            yield return SyncPhysics();

            for (var shot = 0; shot < PoolSize + 2; shot++)
            {
                Assert.That(pool.Fire(Vector3.zero, Vector3.forward, 20f, 8f, 1.5f, rig), Is.Not.Null,
                    "Shot " + shot + " was dropped.");
            }

            Assert.That(pool.ActiveCount, Is.EqualTo(PoolSize), "The pool must never exceed its capacity.");
            Assert.That(pool.RecycledUnderPressure, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Shots_FollowTheFacingAtTheMomentEachOneLeaves()
        {
            var ahead = CreateTarget("Ahead", new Vector3(0f, 1f, 3f));
            var toTheRight = CreateTarget("Right", new Vector3(3f, 1f, 0f));
            yield return SyncPhysics();

            FireOneShot(Vector3.forward);
            StepUntilIdle();
            Assert.That(ahead.CurrentHealth, Is.EqualTo(42f));

            // Turn between shots. The second projectile must follow the new facing, not the old one.
            FireOneShot(Vector3.right);
            StepUntilIdle();

            Assert.That(toTheRight.CurrentHealth, Is.EqualTo(42f), "The second shot did not follow the new facing.");
            Assert.That(ahead.CurrentHealth, Is.EqualTo(42f), "The second shot went the old way.");
        }

        [UnityTest]
        public IEnumerator ReleaseAll_RecallsEverythingInFlight()
        {
            yield return SyncPhysics();

            for (var shot = 0; shot < 3; shot++)
            {
                pool.Fire(Vector3.zero, Vector3.forward, 20f, 8f, 1.5f, rig);
            }

            Assert.That(pool.ActiveCount, Is.EqualTo(3));

            pool.ReleaseAll();

            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(pool.FreeCount, Is.EqualTo(PoolSize), "Recalled projectiles must be reusable.");
        }

        /// <summary>Waits out any cadence still owing, then fires exactly one shot in the given direction.</summary>
        private void FireOneShot(Vector3 direction)
        {
            for (var guard = 0; guard < 120 && ranged.CooldownRemaining > 0f; guard++)
            {
                ranged.Tick(Frame, false);
            }

            rig.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            var before = ranged.ShotsFired;

            ranged.Tick(Frame, true);
            Assert.That(ranged.ShotsFired, Is.EqualTo(before + 1), "The controller refused to fire.");

            ranged.Tick(Frame, false);
        }

        /// <summary>Steps the pool until nothing is in flight, with a bound so a stuck projectile fails.</summary>
        private void StepUntilIdle()
        {
            for (var step = 0; step < 240 && pool.ActiveCount > 0; step++)
            {
                pool.Tick(Frame);
            }

            Assert.That(pool.ActiveCount, Is.Zero, "A projectile never resolved or expired.");
        }

        private Health CreateTarget(string name, Vector3 position)
        {
            var target = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
            target.name = name;
            target.layer = GameLayers.Enemy;
            target.transform.position = position;

            var health = target.AddComponent<Health>();
            health.Configure(50f);
            return health;
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
    }
}
