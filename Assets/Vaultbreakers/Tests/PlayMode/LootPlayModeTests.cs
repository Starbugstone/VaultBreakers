using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Dungeon;
using Vaultbreakers.Gems;
using Vaultbreakers.Player;
using Vaultbreakers.Zones;

namespace Vaultbreakers.Tests.PlayMode
{
    public sealed class LootPlayModeTests
    {
        private ZoneController zone;
        private DungeonLoot loot;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale = 1; yield return SceneManager.LoadSceneAsync("Dock9_Dungeon"); yield return null;
            zone = Object.FindAnyObjectByType<ZoneController>(); loot = zone.GetComponent<DungeonLoot>();
            zone.Player.SetInvulnerable(true); yield return Fight();
        }
        [UnityTearDown] public IEnumerator Cleanup() { Time.timeScale = 1; yield return IsolatedTestBed.Load(); }
        private IEnumerator Fight()
        {
            var until = Time.realtimeSinceStartup + 8;
            while (zone.State != ZoneState.Fighting && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(zone.State, Is.EqualTo(ZoneState.Fighting));
        }
        private void Move(Vector3 target)
        {
            var body = zone.Player.GetComponent<CharacterController>(); body.enabled = false;
            zone.Player.transform.position = target; body.enabled = true; Physics.SyncTransforms();
        }
        private void ClearEnemies()
        {
            foreach (var enemy in zone.Spawner.Roster.Instances)
                if (enemy.gameObject.activeSelf && !enemy.Health.IsDead) enemy.Health.ReceiveDamage(new DamageInfo(1000, zone.Player.gameObject));
        }
        [UnityTest] public IEnumerator EnemyDeathDropsOnceAndDespawnDoesNotProduceLoot()
        {
            loot.enabled = false;
            var enemy = zone.Spawner.Roster.Instances.First(e => e.gameObject.activeSelf);
            enemy.Health.ReceiveDamage(new DamageInfo(1000, zone.Player.gameObject));
            Assert.That(loot.Pool.ActiveValue, Is.EqualTo(loot.Balance.gruntGems));
            enemy.Health.ReceiveDamage(new DamageInfo(1000, zone.Player.gameObject)); zone.Spawner.Roster.Clear();
            Assert.That(loot.Pool.ActiveValue, Is.EqualTo(loot.Balance.gruntGems)); yield return null;
        }
        [UnityTest] public IEnumerator HammerAndProjectilesBreakContainersWithoutDuplicateRewards()
        {
            loot.enabled = false;
            var crate = loot.Containers.First(c => c.Room == 0 && c.Source == LootSource.Crate);
            Move(crate.transform.position + Vector3.back * 1.3f);
            zone.Player.GetComponent<PlayerFacing>().ApplyAttackFacing(Vector3.forward);
            var melee = zone.Player.GetComponent<MeleeController>(); melee.enabled = false;
            Assert.That(melee.TryStartSwing(), Is.True); melee.Tick(.08f);
            Assert.That(crate.IsBroken, Is.True); Assert.That(crate.GetComponent<BoxCollider>().enabled, Is.False);
            var units = loot.Pool.SpawnedUnits; melee.Tick(.03f);
            Assert.That(loot.Pool.SpawnedUnits, Is.EqualTo(units)); Assert.That(units, Is.EqualTo(loot.Balance.crateGems));
            var chest = loot.Containers.First(c => c.Room == 0 && c.Source == LootSource.Chest);
            var shots = zone.Player.GetComponent<ProjectilePool>();
            for (var i = 0; i < 3; i++)
            {
                shots.Fire(chest.transform.position + Vector3.up * .6f + Vector3.back * 2, Vector3.forward, 20, 10, 2, zone.Player.gameObject);
                Physics.SyncTransforms(); shots.Tick(.12f);
            }
            Assert.That(chest.IsBroken, Is.True); Assert.That(loot.Pool.SpawnedUnits - units, Is.EqualTo(loot.Balance.chestGems));
            Assert.That(chest.GetComponent<BoxCollider>().enabled, Is.False); yield return null;
        }
        [UnityTest] public IEnumerator GemsAreNonBlockingAndCannotCollectThroughSolidContainers()
        {
            loot.enabled = false;
            var crate = loot.Containers.First(c => c.Room == 0 && c.Source == LootSource.Crate);
            var origin = crate.transform.position;
            Move(origin + Vector3.forward * .9f);
            loot.Pool.Spawn(origin + Vector3.back * .9f, GemKind.Gold, 3);
            for (var i = 0; i < 40; i++) loot.Pool.Tick(.02f, zone.Player.transform.position, true);
            Assert.That(loot.Wallet.Total, Is.Zero, "The solid cache must occlude the pickup magnet.");
            Assert.That(loot.Pool.GetComponentsInChildren<Collider>(true).Where(c => c.gameObject.layer == GameLayers.Gem), Is.Empty);
            Assert.That(loot.Pool.GetComponentsInChildren<Rigidbody>(true).Where(c => c.gameObject.layer == GameLayers.Gem), Is.Empty);
            Assert.That(loot.Pool.TryNearest(zone.Player.transform.position, out var gem), Is.True);
            Move(gem); for (var i = 0; i < 60; i++) loot.Pool.Tick(.02f, zone.Player.transform.position, true);
            Assert.That(loot.Wallet.Total, Is.EqualTo(3)); Assert.That(loot.Pool.ActiveCount, Is.Zero); yield return null;
        }
        [UnityTest] public IEnumerator ActualCasterMuzzleCanBreakBothCacheTypesFromEveryCardinalDirection()
        {
            loot.enabled = false; ClearEnemies(); loot.Pool.Clear();
            var ranged = zone.Player.GetComponent<RangedController>(); ranged.enabled = false;
            var lastOrigin = Vector3.zero; ranged.Fired += (origin, direction) => lastOrigin = origin;
            var facing = zone.Player.GetComponent<PlayerFacing>();
            var shots = zone.Player.GetComponent<ProjectilePool>();
            foreach (var kind in new[] { LootSource.Crate, LootSource.Chest })
            foreach (var direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                var cache = loot.Containers.First(c => c.Room == 0 && c.Source == kind); cache.ResetLoot(true);
                Move(cache.transform.position - direction * 3);
                facing.ApplyAttackFacing(direction); yield return new WaitForSeconds(.25f);
                for (var i = 0; i < 6 && !cache.IsBroken; i++)
                { ranged.Tick(.25f, true); Physics.SyncTransforms(); shots.Tick(.18f); yield return new WaitForSeconds(.16f); }
                ranged.StopFiring(); shots.ReleaseAll();
                Assert.That(cache.IsBroken, Is.True, kind + " cannot be shot from " + direction + " with the animated caster muzzle at " + lastOrigin + ", cache top " + cache.GetComponent<BoxCollider>().bounds.max.y);
            }
        }
        [UnityTest] public IEnumerator PauseDeathAndRetryResetLootWithoutFarmingOrGhostColliders()
        {
            loot.enabled = false; var crate = loot.Containers.First(c => c.Room == 0);
            crate.Health.ReceiveDamage(new DamageInfo(1000, zone.Player.gameObject));
            loot.Pool.Spawn(zone.Player.transform.position, GemKind.Gold, 5);
            for (var i = 0; i < 60; i++) loot.Pool.Tick(.02f, zone.Player.transform.position, true);
            Assert.That(loot.Wallet.Total, Is.GreaterThanOrEqualTo(5));
            loot.Pool.Spawn(zone.Player.transform.position, GemKind.Relic, 1); loot.enabled = true;
            zone.SetPaused(true); var before = loot.Pool.ActiveValue;
            yield return new WaitForSecondsRealtime(.25f); Assert.That(loot.Pool.ActiveValue, Is.EqualTo(before));
            zone.SetPaused(false); zone.Player.SetInvulnerable(false); zone.Player.ReceiveDamage(new DamageInfo(1000));
            Assert.That(loot.Pool.ActiveCount, Is.Zero); Assert.That(loot.Wallet.Total, Is.Zero);
            Assert.That(crate.IsBroken, Is.False); Assert.That(crate.GetComponent<BoxCollider>().enabled, Is.True);
            yield return Fight(); Assert.That(loot.Wallet.Total, Is.Zero); Assert.That(loot.Pool.ActiveCount, Is.Zero);
        }
        [UnityTest] public IEnumerator ContainerDebrisShrinksFadesPausesAndDisappearsWithinOneSecond()
        {
            loot.enabled = false;
            foreach (var kind in new[] { LootSource.Crate, LootSource.Chest })
            {
                var cache = loot.Containers.First(c => c.Room == 0 && c.Source == kind);
                var broken = cache.transform.Find("Broken cache");
                var pieces = broken.GetComponentsInChildren<Renderer>(true);
                Assert.That(pieces.Length, Is.EqualTo(6), "Each cache should fracture into six individually animated chunks.");
                var originalScale = pieces[0].transform.localScale;
                cache.Health.ReceiveDamage(new DamageInfo(1000, zone.Player.gameObject));
                Assert.That(broken.gameObject.activeSelf, Is.True);
                yield return new WaitForSeconds(.6f);
                Assert.That(pieces[0].transform.localScale.magnitude, Is.LessThan(originalScale.magnitude * .8f));
                var tint = new MaterialPropertyBlock(); pieces[0].GetPropertyBlock(tint, 0);
                Assert.That(tint.GetColor("_BaseColor").a, Is.InRange(.1f, .9f));
                var pausedScale = pieces[0].transform.localScale;
                zone.SetPaused(true); yield return new WaitForSecondsRealtime(.2f);
                Assert.That(pieces[0].transform.localScale, Is.EqualTo(pausedScale));
                Assert.That(broken.gameObject.activeSelf, Is.True);
                zone.SetPaused(false); yield return new WaitForSeconds(.5f);
                Assert.That(broken.gameObject.activeSelf, Is.False);
                Assert.That(cache.GetComponent<BoxCollider>().enabled, Is.False);
                cache.ResetLoot(true);
                Assert.That(pieces[0].transform.localScale, Is.EqualTo(originalScale));
                cache.Health.ReceiveDamage(new DamageInfo(1000, zone.Player.gameObject));
                Assert.That(broken.gameObject.activeSelf, Is.True);
            }
        }
        [UnityTest] public IEnumerator ClearAllowsCollectionBeforeGateOpensAndBanksOnlyAtTheNextRoom()
        {
            loot.enabled = false; loot.Pool.Spawn(zone.Player.transform.position, GemKind.Ranged, 7);
            for (var i = 0; i < 60; i++) loot.Pool.Tick(.02f, zone.Player.transform.position, true);
            ClearEnemies(); yield return null;
            Assert.That(zone.IsCollecting, Is.True); Assert.That(GameObject.Find("Dock9 exit 0"), Is.Not.Null);
            var timer = zone.TransitionRemaining; zone.SetPaused(true); yield return new WaitForSecondsRealtime(.15f);
            Assert.That(zone.TransitionRemaining, Is.EqualTo(timer)); zone.SetPaused(false);
            yield return new WaitForSeconds(loot.Balance.collectionWindow + .1f);
            Assert.That(GameObject.Find("Dock9 exit 0"), Is.Null);
            Move(Vector3.forward * (DungeonLayout.AdvanceOffset + .1f)); yield return null;
            Assert.That(zone.WaveNumber, Is.EqualTo(2)); Assert.That(loot.Wallet.Total, Is.EqualTo(7)); Assert.That(loot.Pool.ActiveCount, Is.Zero);
            loot.Pool.Spawn(zone.Player.transform.position, GemKind.Gold, 2);
            // Bring a room-local pickup into reach, then retry; only the previous room survives.
            Move(zone.RoomOrigin); loot.Pool.Spawn(zone.Player.transform.position, GemKind.Gold, 2);
            for (var i = 0; i < 60; i++) loot.Pool.Tick(.02f, zone.Player.transform.position, true);
            zone.RestartWave(); Assert.That(loot.Wallet.Total, Is.EqualTo(7));
            zone.RestartRun(); Assert.That(loot.Wallet.Total, Is.Zero); Assert.That(loot.Pool.ActiveCount, Is.Zero);
        }
    }
}
