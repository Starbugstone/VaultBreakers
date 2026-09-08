using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vaultbreakers.Core;
using Vaultbreakers.Combat;
using Vaultbreakers.Dungeon;
using Vaultbreakers.Enemies;
using Vaultbreakers.Zones;
using Vaultbreakers.Player;
namespace Vaultbreakers.Editor
{
    internal static class VaultbreakersDungeonBuilder
    {
        public const string ScenePath="Assets/Vaultbreakers/Scenes/Prototype/Dock9_Dungeon.unity";
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene(VaultbreakersSetupPaths.PrototypeScenePath);
            var zone=Object.FindAnyObjectByType<ZoneController>();var player=zone.Player.gameObject;
            Object.DestroyImmediate(GameObject.Find("ArenaArt"));
            var environment=GameObject.Find("Environment");
            foreach(var name in new[]{"Floor","Wall_North","Wall_South","Wall_East","Wall_West"})Object.DestroyImmediate(GameObject.Find(name));
            VaultbreakersArtBuilder.Model("Assets/Vaultbreakers/Art/Environments/Dock9_Dungeon.fbx",environment.transform,"Dock9 Ruins");
            var exit=new GameObject[3];var entrance=new GameObject[3];
            var gateMaterial=VaultbreakersEnemyBuilder.Material("Dock9Gate",new Color(.25f,.65f,.7f),.6f);
            for(var room=0;room<3;room++)
            {
                var z=room*28f;
                ColliderBox("Room floor",new Vector3(0,-.25f,z),new Vector3(20,.5f,20),environment.transform);
                foreach(var x in new[]{-10.3f,10.3f})ColliderBox("Side wall",new Vector3(x,1,z),new Vector3(.6f,3,20.6f),environment.transform);
                foreach(var end in new[]{-10.3f,10.3f})foreach(var x in new[]{-6.5f,6.5f})ColliderBox("Door wall",new Vector3(x,1,z+end),new Vector3(7,.8f,.6f),environment.transform);
                foreach(var x in new[]{-8f,8f})foreach(var y in new[]{-8f,8f})
                    ColliderBox("Pillar collision",new Vector3(x,1,z+y),new Vector3(.85f,2.8f,.85f),environment.transform);
                foreach(var x in new[]{-5.44f,5.44f})foreach(var y in new[]{-8f,8f})
                    ColliderBox("Lamp collision",new Vector3(x,.5f,z+y),new Vector3(.65f,1,.65f),environment.transform);
                foreach(var point in new[]{new Vector2(-8,3),new Vector2(8,-3),new Vector2(-6,-8)})
                    ColliderBox("Crate collision",new Vector3(point.x,.48f,z-point.y),new Vector3(1.2f,.96f,1),environment.transform);
                if(room==2)ColliderBox("Core cache collision",new Vector3(0,.65f,z+7),new Vector3(1.95f,1.3f,1.05f),environment.transform);
                foreach(var x in new[]{-2.8f,2.8f})
                    ColliderBox("Gate post collision",new Vector3(x,1.5f,z+10),new Vector3(.85f,3,.85f),environment.transform);
                exit[room]=Gate("Dock9 exit "+room,z+9.7f,environment.transform,gateMaterial);
                entrance[room]=Gate("Dock9 entrance "+room,z-9.7f,environment.transform,gateMaterial);
                if(room<2)
                {
                    ColliderBox("Bridge floor",new Vector3(0,-.25f,z+14),new Vector3(5.6f,.5f,8),environment.transform);
                    foreach(var x in new[]{-2.95f,2.95f})ColliderBox("Bridge wall",new Vector3(x,.7f,z+14),new Vector3(.4f,2,8),environment.transform);
                }
                foreach(var x in new[]{-5.44f,5.44f})foreach(var side in new[]{-8,8})
                {
                    var go=new GameObject("Torch glow");go.transform.position=new Vector3(x*.7f,1.7f,(z+side)*.7f);
                    var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.48f,.16f);light.intensity=5;light.range=5;
                }
            }
            ColliderBox("South boundary",new Vector3(0,1,-10.3f),new Vector3(6,3,.6f),environment.transform);
            ColliderBox("Vault boundary",new Vector3(0,1,66.3f),new Vector3(6,3,.6f),environment.transform);
            environment.transform.localScale=new Vector3(.7f,1,.7f);
            foreach(var marker in GameObject.Find("GameplayMarkers").GetComponentsInChildren<Transform>())
                if(marker.name.StartsWith("SpawnPoint_") && !marker.name.EndsWith("Visual"))marker.position=new Vector3(marker.position.x*.7f,marker.position.y,marker.position.z*.7f);
            GameObject.Find("SpawnPoint_C").transform.position=new Vector3(0,.05f,2.8f);
            zone.ConfigureDungeon(Encounters());zone.Spawner.SetRoomSpacing(19.6f);
            zone.gameObject.AddComponent<DungeonVitals>();
            zone.gameObject.AddComponent<DungeonJourney>().Configure(exit,entrance);
            player.AddComponent<DungeonCombat>().Configure(VaultbreakersEnemyBuilder.UnlitMaterial("DungeonEffects",Color.white));
            player.GetComponent<PlayerFacing>().EnableMouseAim();
            // The large diagnostic melee disc is replaced by the directional sword crescent.
            player.GetComponent<MeleePresentation>().SetArcVisible(false);
            TunePlayer(player);
            var camera=Camera.main;camera.orthographicSize=8.4f;camera.backgroundColor=new Color(.055f,.12f,.13f);
            camera.gameObject.AddComponent<DungeonCamera>().Configure(player.transform);
            RenderSettings.ambientSkyColor=new Color(.48f,.60f,.65f);RenderSettings.ambientEquatorColor=new Color(.24f,.34f,.32f);RenderSettings.ambientGroundColor=new Color(.13f,.20f,.19f);
            RenderSettings.fog=false;
            var sun=GameObject.Find("Key Directional Light").GetComponent<Light>();sun.intensity=1.6f;sun.color=new Color(1,.90f,.72f);sun.transform.rotation=Quaternion.Euler(48,-35,0);
            EditorSceneManager.SaveScene(scene,ScenePath);
        }
        private static void TunePlayer(GameObject player)
        {
            const string path="Assets/Vaultbreakers/Data/Balance/DungeonBalance.asset";
            var balance=AssetDatabase.LoadAssetAtPath<PrototypeBalance>(path);
            if(balance==null)
            {
                balance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<PrototypeBalance>(VaultbreakersSetupPaths.BalancePath));balance.name="DungeonBalance";AssetDatabase.CreateAsset(balance,path);
                var so=new SerializedObject(balance);
                Set(so,"moveSpeed",6.2f);Set(so,"moveAcceleration",50);Set(so,"moveDeceleration",60);Set(so,"visualTurnSpeed",1400);
                Set(so,"meleeDamage",14);Set(so,"meleeCooldown",.27f);Set(so,"meleeStartupDuration",.035f);Set(so,"meleeActiveDuration",.09f);Set(so,"meleeRecoveryDuration",.145f);Set(so,"meleeTargetCorrection",40);Set(so,"meleeKnockback",5.5f);
                Set(so,"fireCooldown",.23f);Set(so,"projectileDamage",10);Set(so,"dodgeCooldown",.75f);Set(so,"hitStopDuration",.035f);so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var component in player.GetComponents<MonoBehaviour>())
            {
                var so=new SerializedObject(component);var field=so.FindProperty("balance");if(field==null)continue;field.objectReferenceValue=balance;so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        private static void Set(SerializedObject so,string name,float value)=>so.FindProperty(name).floatValue=value;
        private static WaveDefinition[] Encounters()
        {
            var entries=new[]{new[]{new EnemySpawnEntry(EnemyRole.Grunt,3)},new[]{new EnemySpawnEntry(EnemyRole.Grunt,5),new EnemySpawnEntry(EnemyRole.Shooter,3)},new[]{new EnemySpawnEntry(EnemyRole.Grunt,4),new EnemySpawnEntry(EnemyRole.Shooter,2),new EnemySpawnEntry(EnemyRole.Bruiser,1)}};
            var names=new[]{"DOCK 9 / SALVAGE INTAKE","REPO TRANSFER COURT","BLACK-GLASS VAULT"};var waves=new WaveDefinition[3];
            for(var i=0;i<3;i++)
            {
                var path="Assets/Vaultbreakers/Data/Waves/Dungeon_"+i+".asset";waves[i]=AssetDatabase.LoadAssetAtPath<WaveDefinition>(path);
                if(waves[i]==null){waves[i]=ScriptableObject.CreateInstance<WaveDefinition>();waves[i].title=names[i];waves[i].spawns=entries[i];AssetDatabase.CreateAsset(waves[i],path);}
            }
            return waves;
        }
        private static GameObject ColliderBox(string name,Vector3 position,Vector3 size,Transform parent)
        {var go=new GameObject(name){layer=GameLayers.Environment};go.transform.SetParent(parent);go.transform.position=position;go.AddComponent<BoxCollider>().size=size;return go;}
        private static GameObject Gate(string name,float z,Transform parent,Material mat)
        {
            var go=ColliderBox(name,new Vector3(0,0,z),new Vector3(5.4f,4,.35f),parent);go.GetComponent<BoxCollider>().center=Vector3.up*1.5f;
            // The gate is a magical lattice; the structural stone arch is an authored Blender mesh.
            for(var i=-2;i<=2;i++)
            {
                var child=new GameObject("Seal strand");child.transform.SetParent(go.transform,false);var line=child.AddComponent<LineRenderer>();line.sharedMaterial=mat;line.useWorldSpace=false;line.positionCount=2;line.startWidth=line.endWidth=.045f;
                line.SetPosition(0,new Vector3(i,0,0));line.SetPosition(1,new Vector3(i,2.8f,0));
            }
            return go;
        }
    }
}
