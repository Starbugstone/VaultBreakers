using UnityEngine;
using Vaultbreakers.Zones;
using Vaultbreakers.Enemies;
using Vaultbreakers.Combat;
using Vaultbreakers.UI;
namespace Vaultbreakers.Dungeon
{
    public sealed class DungeonJourney : MonoBehaviour
    {
        [SerializeField] private GameObject[] exits, entrances;
        private ZoneController zone;
        private int lastRoom=-1,checkpointScore;
        private ZoneState lastState;
        private float comboUntil;
        private int chain;
        public int Score { get; private set; }
        public int Combo => Time.time<comboUntil?Mathf.Min(4,1+chain/3):1;
        public bool TreasureClaimed { get; private set; }
        public string Objective => TreasureClaimed?"RELIC RECOVERED  /  R OR START TO PLAY AGAIN":zone.State==ZoneState.Complete?"CLAIM THE VAULT CORE  •  APPROACH THE CHEST":zone.State==ZoneState.BetweenWaves?"GATE OPEN  •  FOLLOW THE GOLD INLAY":"DEFEAT THE GUARDIANS TO OPEN THE GATE";
        public void Configure(GameObject[] gateExits,GameObject[] gateEntrances){exits=gateExits;entrances=gateEntrances;}
        private void Start()
        {
            zone=GetComponent<ZoneController>();zone.Spawner.Roster.Initialize();
            foreach(var enemy in zone.Spawner.Roster.Instances)enemy.HitReceived+=OnHit;
        }
        private void OnHit(EnemyBrain enemy,DamageInfo info,DamageResult result)
        {
            if(!result.Killed)return;
            if(Time.time>comboUntil)chain=0;chain++;comboUntil=Time.time+4;
            Score+=(enemy.Definition.role==EnemyRole.Bruiser?300:100)*Combo;
            GetComponent<ArcadeFeedback>().Burst(enemy.transform.position+Vector3.up*.8f,new Color(1,.75f,.2f),0);
        }
        private void Update()
        {
            if(zone==null)return;
            var room=zone.WaveNumber-1;
            if(lastRoom!=room){lastRoom=room;checkpointScore=Score;}
            if(zone.State==ZoneState.Preparing && lastState!=ZoneState.Preparing)
            {
                if(room==0)checkpointScore=0;Score=checkpointScore;TreasureClaimed=false;chain=0;
            }
            if(zone.State==ZoneState.Retry && lastState!=ZoneState.Retry){Score=checkpointScore;chain=0;}
            lastState=zone.State;
            for(var i=0;i<exits.Length;i++)
            {
                exits[i].SetActive(i>room || (i==room && zone.State!=ZoneState.BetweenWaves && zone.State!=ZoneState.Complete));
                entrances[i].SetActive(i==room && zone.State==ZoneState.Fighting);
            }
            if(!zone.IsPaused && zone.State==ZoneState.Complete && !TreasureClaimed && Vector3.Distance(zone.Player.transform.position,new Vector3(0,0,44.1f))<2.3f)
            {TreasureClaimed=true;Score+=1000;GetComponent<ArcadeFeedback>().Burst(new Vector3(0,1.5f,44.1f),new Color(1,.85f,.15f),0);}
        }
        private void OnDestroy(){if(zone!=null && zone.Spawner!=null && zone.Spawner.Roster!=null)foreach(var enemy in zone.Spawner.Roster.Instances)if(enemy!=null)enemy.HitReceived-=OnHit;}
    }
}
