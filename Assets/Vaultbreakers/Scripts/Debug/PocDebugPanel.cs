using UnityEngine;
using UnityEngine.InputSystem;
using Vaultbreakers.Combat;
using Vaultbreakers.Enemies;
using Vaultbreakers.Zones;

namespace Vaultbreakers.Debugging
{
    public sealed class PocDebugPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private ZoneController zone;
        private PocDebugView view;
        private void Start()
        {
            zone=GetComponent<ZoneController>();
            view=gameObject.AddComponent<PocDebugView>();view.Owner=this;view.enabled=false;
            var inputOverlay=zone.Player.GetComponent<InputDebugOverlay>();if(inputOverlay!=null)inputOverlay.enabled=false;
            var combatOverlay=GetComponent<CombatDebugOverlay>();if(combatOverlay!=null)combatOverlay.enabled=false;
        }
        private void Update(){if(Keyboard.current!=null && Keyboard.current.f1Key.wasPressedThisFrame)view.enabled=!view.enabled;}
        private void OnDisable(){if(view!=null)view.enabled=false;}
        internal void DrawPanel()
        {
            if(zone==null)return;
            GUILayout.BeginArea(new Rect(20,130,310,600),GUI.skin.box);
            GUILayout.Label("COMBAT LAB  /  F1 TO CLOSE");
            var invulnerable=GUILayout.Toggle(zone.Player.IsInvulnerable,"Invulnerable");zone.Player.SetInvulnerable(invulnerable);
            if(GUILayout.Button("Refill health and guard")){zone.Player.ResetHealth();zone.Player.GetComponent<ShieldController>().ResetShield();}
            if(GUILayout.Button("Break guard"))zone.Player.GetComponent<ShieldController>().Break();
            if(GUILayout.Button("Practice dummies"))zone.EnterTraining();
            if(GUILayout.Button("Restart active wave"))zone.RestartWave();
            if(GUILayout.Button("Return to wave one"))zone.RestartRun();
            for(var i=0;i<3;i++)if(GUILayout.Button("Start wave "+(i+1)))zone.StartSelectedWave(i);
            foreach(var role in new[]{EnemyRole.Grunt,EnemyRole.Shooter,EnemyRole.Bruiser})
                if(GUILayout.Button("Spawn "+role))zone.Spawner.Roster.Spawn(role,zone.RoomOrigin+new Vector3(4,0,4));
            if(GUILayout.Button("Kill all enemies"))foreach(var enemy in zone.Spawner.Roster.Instances)
                if(enemy.gameObject.activeSelf)enemy.Health.ReceiveDamage(new DamageInfo(1000));
            if(GUILayout.Button("Stress encounter (24 enemies)"))
            {
                zone.EnterTraining();
                for(var i=0;i<24;i++)zone.Spawner.Roster.Spawn((EnemyRole)(i%3),new Vector3(Mathf.Sin(i*2.4f)*7,0,Mathf.Cos(i*2.4f)*7));
            }
            var ranged=zone.Player.GetComponent<RangedController>();
            GUILayout.Label("Player pool: "+ranged.Pool.ActiveCount+" / "+ranged.Pool.Capacity);
            GUILayout.Label("Enemy pool: "+zone.Spawner.Roster.Projectiles.ActiveCount);
            GUILayout.Label("State: "+zone.Player.GetComponent<PlayerActionCoordinator>().State);
            GUILayout.Label("Time scale: "+Time.timeScale);
            if(GUILayout.Button("Detailed input and combat overlays"))
            {zone.Player.GetComponent<InputDebugOverlay>().enabled=!zone.Player.GetComponent<InputDebugOverlay>().enabled;GetComponent<CombatDebugOverlay>().enabled=!GetComponent<CombatDebugOverlay>().enabled;}
            GUILayout.EndArea();
        }
#endif
    }
}
