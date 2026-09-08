using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;

namespace Vaultbreakers.Tests.PlayMode
{
    /// <summary>
    /// The half of the melee slice that only real colliders can prove: what the physics query
    /// actually reaches, and that one swing damages each body exactly once. The rig is built here
    /// rather than loaded from a scene so a failure points at the melee rules, not at scene content.
    /// </summary>
    public sealed class MeleePlayModeTests
    {
        private readonly List<GameObject> spawned = new();
        private MeleeController melee;
        private Health playerHealth;
        private PlayerActionCoordinator actions;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return IsolatedTestBed.Load();

            var player = Track(new GameObject("MeleeRig") { layer = GameLayers.Player });
            playerHealth = player.AddComponent<Health>();
            playerHealth.Configure(100f);
            actions = player.AddComponent<PlayerActionCoordinator>();
            actions.Configure(playerHealth);

            melee = player.AddComponent<MeleeController>();
            melee.Configure(null, null, actions, null, playerHealth, null);

            // The phase machine is stepped by hand so the active window lands on a known frame
            // instead of on whatever delta the test runner happened to produce.
            melee.enabled = false;
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
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Swing_DamagesATargetInFrontAndLeavesOneBehindUntouched()
        {
            var front = CreateTarget("Front", new Vector3(0f, 0f, 2f));
            var behind = CreateTarget("Behind", new Vector3(0f, 0f, -2f));
            yield return SyncPhysics();

            SwingTowards(Vector3.forward);

            Assert.That(front.CurrentHealth, Is.EqualTo(40f), "The target in front was not hit.");
            Assert.That(behind.CurrentHealth, Is.EqualTo(50f), "A target behind the player must not be hit.");
        }

        [UnityTest]
        public IEnumerator Swing_DamagesEachTargetExactlyOnceForTheWholeActiveWindow()
        {
            var target = CreateTarget("Target", new Vector3(0f, 0f, 2f));
            var hits = 0;
            target.Damaged += (_, _) => hits++;
            yield return SyncPhysics();

            Assert.That(melee.TryStartSwing(), Is.True);
            melee.Tick(0.06f);

            // Stay inside the active window for several more frames; the swing must not re-hit.
            for (var frame = 0; frame < 4; frame++)
            {
                melee.Tick(0.01f);
            }

            Assert.That(hits, Is.EqualTo(1), "One swing must damage the same target exactly once.");
            Assert.That(melee.HitsThisSwing, Is.EqualTo(1));
            Assert.That(target.CurrentHealth, Is.EqualTo(40f));
        }

        [UnityTest]
        public IEnumerator Swing_DamagesTwoTargetsStandingInsideTheSameVolume()
        {
            var left = CreateTarget("Left", new Vector3(-0.8f, 0f, 2f));
            var right = CreateTarget("Right", new Vector3(0.8f, 0f, 2f));
            yield return SyncPhysics();

            SwingTowards(Vector3.forward);

            Assert.That(left.CurrentHealth, Is.EqualTo(40f));
            Assert.That(right.CurrentHealth, Is.EqualTo(40f));
            Assert.That(melee.HitsThisSwing, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Swing_MissesATargetPastTheConfiguredRange()
        {
            var far = CreateTarget("Far", new Vector3(0f, 0f, 4.5f));
            yield return SyncPhysics();

            SwingTowards(Vector3.forward);

            Assert.That(far.CurrentHealth, Is.EqualTo(50f), "A target well past the range must be missed.");
            Assert.That(melee.HitsThisSwing, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Swing_SnapsOntoATargetJustOutsideTheFacingButInsideTheCone()
        {
            // Ten degrees off the facing: inside the 15 degree correction cone, so the swing should
            // commit to the target's direction rather than to raw facing.
            var offset = Quaternion.Euler(0f, 10f, 0f) * Vector3.forward * 2.4f;
            var target = CreateTarget("Offset", offset);
            yield return SyncPhysics();

            SwingTowards(Vector3.forward);

            Assert.That(Vector3.Angle(melee.SwingDirection, offset), Is.LessThan(1f),
                "The swing should have snapped onto the target inside the cone.");
            Assert.That(target.CurrentHealth, Is.EqualTo(40f));
        }

        [UnityTest]
        public IEnumerator Swing_DeliversKnockbackDirectedAwayFromTheAttacker()
        {
            var target = CreateTarget("Target", new Vector3(0f, 0f, 2f));
            var knockback = Vector3.zero;
            target.Damaged += (damage, _) => knockback = damage.Knockback;
            yield return SyncPhysics();

            SwingTowards(Vector3.forward);

            Assert.That(knockback.magnitude, Is.GreaterThan(0f), "The hit carried no knockback.");
            Assert.That(Vector3.Angle(knockback, Vector3.forward), Is.LessThan(1f));
            Assert.That(knockback.y, Is.EqualTo(0f).Within(0.0001f), "Knockback must stay on the ground plane.");
        }

        [UnityTest]
        public IEnumerator Swing_DoesNotHitThroughTheEnemyLayerMaskOntoEnvironment()
        {
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.name = "Wall";
            wall.layer = GameLayers.Environment;
            wall.transform.position = new Vector3(0f, 0f, 1.5f);
            var wallHealth = wall.AddComponent<Health>();
            wallHealth.Configure(50f);

            var behindWall = CreateTarget("BehindWall", new Vector3(0f, 0f, 2.6f));
            yield return SyncPhysics();

            SwingTowards(Vector3.forward);

            Assert.That(wallHealth.CurrentHealth, Is.EqualTo(50f), "Melee must only query the enemy layer.");
            Assert.That(behindWall.CurrentHealth, Is.EqualTo(40f),
                "The arena geometry does not shield an enemy inside melee reach; the query is a volume, not a ray.");
        }

        [UnityTest]
        public IEnumerator Swing_SpamCannotBeatTheCooldown()
        {
            var target = CreateTarget("Target", new Vector3(0f, 0f, 2f));
            yield return SyncPhysics();

            var swings = 0;
            // One second of 60 FPS frames, pressing every single frame.
            for (var frame = 0; frame < 60; frame++)
            {
                if (melee.TryStartSwing())
                {
                    swings++;
                }

                melee.Tick(1f / 60f);
            }

            Assert.That(swings, Is.EqualTo(4),
                "The cadence equals the 0.3 second swing, so mashing yields four swings in a second and no more.");
            Assert.That(target.CurrentHealth, Is.EqualTo(10f));
        }

        [UnityTest]
        public IEnumerator Swing_IsCancelledByDeathBeforeItCanDealDamage()
        {
            var target = CreateTarget("Target", new Vector3(0f, 0f, 2f));
            yield return SyncPhysics();

            melee.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            Assert.That(melee.TryStartSwing(), Is.True);

            playerHealth.ReceiveDamage(new DamageInfo(100f));
            melee.Tick(0.06f);

            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Ready));
            Assert.That(target.CurrentHealth, Is.EqualTo(50f), "A dead player must not land the swing already started.");
        }

        /// <summary>
        /// The coordinator, not this controller, decides who owns the attack. A higher priority
        /// action taking the claim mid-swing has to stop the damage, or the swing would land during
        /// an action that is supposed to have cancelled it.
        /// </summary>
        [UnityTest]
        public IEnumerator Swing_StopsDealingDamageWhenAHigherPriorityActionTakesTheClaim()
        {
            var target = CreateTarget("Target", new Vector3(0f, 0f, 2f));
            yield return SyncPhysics();

            melee.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            Assert.That(melee.TryStartSwing(), Is.True);

            Assert.That(actions.TryStart(PlayerAction.Dodge), Is.True, "Dodge outranks a running melee swing.");
            melee.Tick(0.06f);

            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Ready));
            Assert.That(target.CurrentHealth, Is.EqualTo(50f), "A cancelled swing must not reach its active window.");
        }

        [UnityTest]
        public IEnumerator HitStop_FreezesBrieflyAndAlwaysRestoresTimeScale()
        {
            var hitStop = melee.gameObject.AddComponent<HitStop>();
            hitStop.Request(0.05f);

            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(hitStop.IsActive, Is.True);

            var waited = 0f;
            while (hitStop.IsActive && waited < 1f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(hitStop.IsActive, Is.False, "Hit-stop never released.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Hit-stop must restore the timescale it captured.");
        }

        /// <summary>Drives one complete swing in the given direction and leaves it in recovery.</summary>
        private void SwingTowards(Vector3 direction)
        {
            melee.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            Assert.That(melee.TryStartSwing(), Is.True, "The swing was refused.");
            melee.Tick(0.06f);
            Assert.That(melee.Phase, Is.EqualTo(MeleePhase.Active));
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

        /// <summary>Colliders moved by script are invisible to queries until the transforms are synced.</summary>
        private static IEnumerator SyncPhysics()
        {
            Physics.SyncTransforms();
            yield return null;
        }
    }
}
