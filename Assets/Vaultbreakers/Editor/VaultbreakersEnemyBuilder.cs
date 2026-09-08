using System.IO;
using UnityEditor;
using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Enemies;

namespace Vaultbreakers.Editor
{
    internal static class VaultbreakersEnemyBuilder
    {
        public static EnemyRoster Build(GameObject root, Health player)
        {
            Directory.CreateDirectory("Assets/Vaultbreakers/Data/Enemies");
            Directory.CreateDirectory("Assets/Vaultbreakers/Prefabs/Enemies");
            AssetDatabase.Refresh();
            var templates = new EnemyBrain[3];
            for (var i = 0; i < 3; i++) templates[i] = BuildEnemy((EnemyRole)i);
            var shotPath = "Assets/Vaultbreakers/Prefabs/Projectiles/PF_EnemyProjectile.prefab";
            var shot = GameObject.CreatePrimitive(PrimitiveType.Cube); shot.name = "PF_EnemyProjectile";
            Object.DestroyImmediate(shot.GetComponent<Collider>());
            shot.layer = GameLayers.EnemyProjectile;
            shot.transform.localScale = new Vector3(0.28f, 0.28f, 0.55f);
            shot.GetComponent<Renderer>().sharedMaterial = Material("EnemyShot", new Color(1, 0.22f, 0.1f), 2);
            shot.GetComponent<Renderer>().enabled=false;
            shot.transform.localScale=Vector3.one;
            VaultbreakersArtBuilder.Model("Assets/Vaultbreakers/Art/VFX/Enemy_Bolt.fbx",shot.transform);
            shot.AddComponent<Projectile>();
            var shotPrefab = PrefabUtility.SaveAsPrefabAsset(shot, shotPath); Object.DestroyImmediate(shot);
            var pool = root.AddComponent<ProjectilePool>();
            pool.Configure(shotPrefab.GetComponent<Projectile>(), 48, 0.14f, GameLayers.PlayerTargets | GameLayers.Blocking);
            var roster = root.AddComponent<EnemyRoster>(); roster.Configure(templates, player, pool);
            return roster;
        }
        public static Material Material(string name, Color color, float emission = 0)
        {
            var path = "Assets/Vaultbreakers/Art/Materials/VB_" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.color = color; mat.SetFloat("_Smoothness", 0.4f);
            if (emission > 0) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * emission); }
            EditorUtility.SetDirty(mat); return mat;
        }
        public static Material UnlitMaterial(string name, Color color, string shaderName="Universal Render Pipeline/Unlit")
        {
            var path="Assets/Vaultbreakers/Art/Materials/VB_"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find(shaderName));AssetDatabase.CreateAsset(material,path);}
            material.color=color;EditorUtility.SetDirty(material);return material;
        }
        private static EnemyBrain BuildEnemy(EnemyRole role)
        {
            var path = "Assets/Vaultbreakers/Data/Enemies/" + role + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyDefinition>(); data.role = role;
                if (role == EnemyRole.Shooter)
                { data.displayName = "Repo Shooter"; data.health = 24; data.speed = 2.4f; data.windup = 0.9f; data.recovery = 1.1f; data.damage = 8; data.accent = new Color(1, 0.23f, 0.35f); }
                else if (role == EnemyRole.Bruiser)
                { data.displayName = "Heavy Bruiser"; data.health = 75; data.speed = 1.6f; data.range = 3; data.windup = 1.1f; data.recovery = 1.2f; data.damage = 22; data.stabilityDamage = 60; data.arc = 150; data.accent = new Color(0.8f, 0.4f, 1); }
                AssetDatabase.CreateAsset(data, path);
            }
            var root = new GameObject("PF_" + role) { layer = GameLayers.Enemy };
            var capsule = root.AddComponent<CharacterController>(); capsule.radius = role == EnemyRole.Bruiser ? 0.65f : 0.4f;
            capsule.height = role == EnemyRole.Bruiser ? 2.4f : 1.8f; capsule.center = Vector3.up * capsule.height / 2; capsule.stepOffset = 0.15f;
            root.AddComponent<Health>(); var brain = root.AddComponent<EnemyBrain>(); brain.Configure(data);
            root.AddComponent<EnemyPresentation>().Configure(UnlitMaterial("EnemyTell", Color.white));
            var model = VaultbreakersArtBuilder.Model("Assets/Vaultbreakers/Art/Characters/Enemies/Enemy_" + role + ".fbx", root.transform);
            model.transform.localScale = Vector3.one * (role == EnemyRole.Bruiser ? 1.25f : role == EnemyRole.Shooter ? 0.92f : 1);
            VaultbreakersArtBuilder.Animate(root, model);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Vaultbreakers/Prefabs/Enemies/PF_" + role + ".prefab");
            Object.DestroyImmediate(root); return prefab.GetComponent<EnemyBrain>();
        }
    }
}
