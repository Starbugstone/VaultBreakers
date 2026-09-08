using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Vaultbreakers.Combat;
using Vaultbreakers.Enemies;
using Vaultbreakers.Zones;

namespace Vaultbreakers.Tests.PlayMode
{
    public sealed class WavePlayModeTests
    {
        private ZoneController zone;
        [UnitySetUp] public IEnumerator Load()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Prototype_Arena");
            yield return null;
            zone = Object.FindAnyObjectByType<ZoneController>();
            Assert.That(zone, Is.Not.Null);
            zone.Player.SetInvulnerable(true);
        }
        [UnityTearDown] public IEnumerator Cleanup() { Time.timeScale = 1; yield return IsolatedTestBed.Load(); }
        private static IEnumerator WaitForFight(ZoneController run)
        {
            var deadline = Time.realtimeSinceStartup + 5;
            while (run.State != ZoneState.Fighting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(run.State, Is.EqualTo(ZoneState.Fighting));
        }
        [UnityTest] public IEnumerator ThreeCompleteRunsDoNotSoftLockOrCarryMembership()
        {
            for (var run = 0; run < 3; run++)
            {
                zone.RestartRun();
                for (var wave = 1; wave <= 3; wave++)
                {
                    yield return WaitForFight(zone);
                    Assert.That(zone.WaveNumber, Is.EqualTo(wave));
                    Assert.That(zone.Spawner.Progress.LivingCount, Is.EqualTo(3));
                    foreach (var enemy in zone.Spawner.Roster.Instances)
                        if (enemy.gameObject.activeSelf) enemy.Health.ReceiveDamage(new DamageInfo(1000));
                }
                Assert.That(zone.State, Is.EqualTo(ZoneState.Complete));
                Assert.That(zone.Spawner.Progress.CompletedWaves, Is.EqualTo(3));
            }
        }
        [UnityTest] public IEnumerator LethalProjectileResetsDuringPoolTickWithoutStaleEnemiesOrShots()
        {
            zone.StartSelectedWave(1); yield return WaitForFight(zone);
            zone.Player.SetInvulnerable(false);
            var pool = zone.Spawner.Roster.Projectiles;
            pool.Fire(new Vector3(0, 1, -2), Vector3.forward, 15, 1000, 3, null);
            Physics.SyncTransforms(); pool.Tick(0.2f);
            Assert.That(zone.State, Is.EqualTo(ZoneState.Retry));
            Assert.That(pool.ActiveCount, Is.Zero);
            Assert.That(zone.Spawner.Progress.LivingCount, Is.Zero);
            yield return WaitForFight(zone);
            Assert.That(zone.WaveNumber, Is.EqualTo(2));
            Assert.That(zone.Player.CurrentHealth, Is.EqualTo(zone.Player.MaximumHealth));
            Assert.That(zone.Player.IsInvulnerable, Is.False);
            Assert.That(zone.Spawner.Progress.LivingCount, Is.EqualTo(3));
        }
        [UnityTest] public IEnumerator EnemyCannotHitBeforeWindupAndDoesNotTrackDuringCommittedAttack()
        {
            zone.EnterTraining(); zone.Player.SetInvulnerable(false);
            var enemy = zone.Spawner.Roster.Spawn(EnemyRole.Bruiser, new Vector3(0, 0, 2));
            enemy.enabled = false; enemy.Tick(.7f); enemy.Tick(.01f);
            var hp = zone.Player.CurrentHealth;
            enemy.Tick(enemy.Definition.windup - .02f);
            Assert.That(zone.Player.CurrentHealth, Is.EqualTo(hp));
            var controller = zone.Player.GetComponent<CharacterController>();controller.enabled = false;
            zone.Player.transform.position = new Vector3(0, 0, 4);controller.enabled = true;
            enemy.Tick(.03f);
            Assert.That(zone.Player.CurrentHealth, Is.EqualTo(hp), "Escaping behind the committed strike must work.");
            yield return null;
        }
        [UnityTest] public IEnumerator PauseFreezesWaveDelayAndResumesWithoutLosingTheRun()
        {
            zone.RestartWave(); zone.SetPaused(true); var remaining = zone.TransitionRemaining;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(zone.TransitionRemaining, Is.EqualTo(remaining));
            zone.SetPaused(false); yield return WaitForFight(zone);
        }
    }
}
