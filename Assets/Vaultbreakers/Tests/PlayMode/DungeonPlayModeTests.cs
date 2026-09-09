using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Vaultbreakers.Combat;
using Vaultbreakers.Dungeon;
using Vaultbreakers.Zones;
namespace Vaultbreakers.Tests.PlayMode
{
    public sealed class DungeonPlayModeTests
    {
        private ZoneController zone;
        [UnitySetUp] public IEnumerator Load()
        {Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Dock9_Dungeon");yield return null;zone=Object.FindAnyObjectByType<ZoneController>();zone.Player.SetInvulnerable(true);}
        [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;yield return IsolatedTestBed.Load();}
        private IEnumerator Fight()
        {var until=Time.realtimeSinceStartup+5;while(zone.State!=ZoneState.Fighting && Time.realtimeSinceStartup<until)yield return null;Assert.That(zone.State,Is.EqualTo(ZoneState.Fighting));}
        private void Move(Vector3 position)
        {var controller=zone.Player.GetComponent<CharacterController>();controller.enabled=false;zone.Player.transform.position=position;controller.enabled=true;Physics.SyncTransforms();}
        private void Clear(){foreach(var e in zone.Spawner.Roster.Instances)if(e.gameObject.activeSelf && !e.Health.IsDead)e.Health.ReceiveDamage(new DamageInfo(10000));}
        [UnityTest] public IEnumerator GateRequiresClearAndWalkingAdvancesAllRoomsToRewardAndReplay()
        {
            for(var room=0;room<3;room++)
            {
                yield return Fight();Assert.That(zone.WaveNumber,Is.EqualTo(room+1));
                Assert.That(GameObject.Find("Dock9 exit "+room),Is.Not.Null);
                Clear();yield return null;
                Assert.That(GameObject.Find("Dock9 exit "+room),Is.Null,"The exit seal must be disabled after clear.");
                if(room<2)
                {
                    yield return new WaitForSeconds(.2f);Assert.That(zone.WaveNumber,Is.EqualTo(room+1),"Time alone must not skip a room.");
                    Move(new Vector3(0,0,zone.RoomOrigin.z+DungeonLayout.AdvanceOffset+.1f));yield return null;
                }
            }
            Assert.That(zone.State,Is.EqualTo(ZoneState.Complete));Move(DungeonLayout.CorePosition);yield return null;
            var journey=zone.GetComponent<DungeonJourney>();Assert.That(journey.TreasureClaimed,Is.True);var score=journey.Score;yield return null;Assert.That(journey.Score,Is.EqualTo(score),"Reward is awarded once.");
            zone.RestartRun();yield return Fight();Assert.That(journey.TreasureClaimed,Is.False);Assert.That(journey.Score,Is.Zero);Assert.That(zone.WaveNumber,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator DeathRetriesCurrentRoomAndClearsAllActiveShots()
        {
            zone.StartSelectedWave(1);yield return Fight();zone.Player.SetInvulnerable(false);
            zone.Player.ReceiveDamage(new DamageInfo(10000));Assert.That(zone.State,Is.EqualTo(ZoneState.Retry));
            Assert.That(zone.Spawner.Roster.Projectiles.ActiveCount,Is.Zero);Assert.That(zone.Player.GetComponent<ProjectilePool>().ActiveCount,Is.Zero);
            yield return Fight();Assert.That(zone.WaveNumber,Is.EqualTo(2));Assert.That(zone.Player.CurrentHealth,Is.EqualTo(100));
            Assert.That(zone.Player.transform.position.z,Is.EqualTo(DungeonLayout.RoomSpacing-3.5f).Within(.2f));
        }
        [UnityTest] public IEnumerator PropsBlockThePlayerAndInitialSpawnsAreSeparated()
        {
            yield return Fight();
            foreach(var enemy in zone.Spawner.Roster.Instances)if(enemy.gameObject.activeSelf)
                Assert.That(Vector3.Distance(enemy.transform.position,zone.Player.transform.position),Is.GreaterThan(.9f));
            Move(new Vector3(9.6f,0,2));zone.Player.GetComponent<CharacterController>().Move(Vector3.forward*2);
            Assert.That(zone.Player.transform.position.z,Is.LessThan(3.2f),"The visible crate at (9.6,3.6) must block movement.");
            Clear();yield return null;Move(new Vector3(3.36f,0,10));zone.Player.GetComponent<CharacterController>().Move(Vector3.forward*2);
            Assert.That(zone.Player.transform.position.z,Is.LessThan(11.5f),"An open gate's solid posts must still block movement.");
            zone.StartSelectedWave(2);yield return Fight();zone.EnterTraining();yield return null;
            Assert.That(zone.WaveNumber,Is.EqualTo(1));Assert.That(zone.Player.transform.position.z,Is.LessThan(0));
        }
        [UnityTest] public IEnumerator ClosedExitBlocksMovementAndLargerRoomCameraTracksWithoutRotating()
        {
            yield return Fight();yield return new WaitForSeconds(.3f);var camera=Camera.main;var before=camera.transform.position;var rotation=camera.transform.rotation;
            Move(new Vector3(0,0,10));var motor=zone.Player.GetComponent<CharacterController>();motor.Move(Vector3.forward*4);
            Assert.That(zone.Player.transform.position.z,Is.LessThan(12));
            Move(new Vector3(3,0,2));yield return new WaitForSeconds(.2f);
            Assert.That(Vector3.Distance(before,camera.transform.position),Is.GreaterThan(.5f));
            Assert.That(Quaternion.Angle(rotation,camera.transform.rotation),Is.LessThan(.05f));
        }
    }
}
