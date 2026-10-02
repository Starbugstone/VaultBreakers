using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Dungeon;
using Vaultbreakers.Gems;
using Vaultbreakers.Zones;

namespace Vaultbreakers.Editor
{
    internal static class VaultbreakersLootBuilder
    {
        private const string Folder = "Assets/Vaultbreakers/Art/Loot/";
        private const string BalancePath = "Assets/Vaultbreakers/Data/Balance/GemBalance.asset";
        private static readonly string[] Names = { "Melee", "Ranged", "Shield", "Relic", "Gold" };
        public static void PrepareMaterials()
        {
            var names = new[] { "Ink", "Steel", "Crate", "Chest", "Brass", "Indicator", "Melee", "Ranged", "Shield", "Relic", "Gold" };
            var colors = new[] { new Color(.035f,.055f,.07f),new Color(.27f,.36f,.4f),new Color(.72f,.22f,.045f),
                new Color(.045f,.3f,.32f),new Color(.83f,.55f,.12f),new Color(1,.77f,.2f),
                new Color(1,.22f,.06f),new Color(.04f,.62f,1),new Color(.54f,1,.85f),new Color(.65f,.17f,1),new Color(1,.68f,.055f) };
            for (var i = 0; i < names.Length; i++)
            {
                var path = "Assets/Vaultbreakers/Art/Materials/Loot_" + names[i] + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null) continue; // Artist-owned tuning survives setup.
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", colors[i]); material.SetFloat("_Metallic", .45f); material.SetFloat("_Smoothness", .48f);
                if (i >= 5) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", colors[i] * .65f); }
                AssetDatabase.CreateAsset(material, path);
            }
        }
        public static void Build(ZoneController zone)
        {
            var balance = AssetDatabase.LoadAssetAtPath<GemBalance>(BalancePath);
            if (balance == null) { balance = ScriptableObject.CreateInstance<GemBalance>(); AssetDatabase.CreateAsset(balance, BalancePath); }
            zone.ConfigureCollectionWindow(balance.collectionWindow);
            var meshes = new Mesh[GemRules.KindCount]; var materials = new Material[GemRules.KindCount];
            for (var i = 0; i < Names.Length; i++)
            {
                var path = Folder + "Gem_" + Names[i] + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer == null) throw new FileNotFoundException("Generate loot art with Tools/Blender/generate_loot_assets.py", path);
                importer.isReadable = true; importer.SaveAndReimport();
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var parts = model.GetComponentsInChildren<MeshFilter>(true);
                var combined = new Mesh { name = "Gem_" + Names[i] + "_Unity" };
                combined.CombineMeshes(parts.Select(p => new CombineInstance { mesh = p.sharedMesh, transform = p.transform.localToWorldMatrix }).ToArray(), true, true);
                combined.RecalculateBounds();
                var meshPath = Folder + combined.name + ".asset";
                meshes[i] = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (meshes[i] == null) { AssetDatabase.CreateAsset(combined, meshPath); meshes[i] = combined; }
                else { EditorUtility.CopySerialized(combined, meshes[i]); UnityEngine.Object.DestroyImmediate(combined); }
                materials[i] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Vaultbreakers/Art/Materials/Loot_" + Names[i] + ".mat");
            }
            var pool = zone.gameObject.AddComponent<GemPool>(); pool.Configure(balance, meshes, materials);
            var loot = zone.gameObject.AddComponent<DungeonLoot>();
            var containers = new BreakableLoot[9];
            for (var room = 0; room < 3; room++)
            {
                // Keep the centre lane and all enemy spawn points free; caches sit within camera reach.
                containers[room * 3] = Container(loot, LootSource.Crate, room, new Vector3(-3.6f, 0, -2), balance);
                containers[room * 3 + 1] = Container(loot, LootSource.Crate, room, new Vector3(3.5f, 0, 1.5f), balance);
                containers[room * 3 + 2] = Container(loot, LootSource.Chest, room, new Vector3(-3.5f, 0, 5.8f), balance);
            }
            loot.Configure(balance, containers);
        }
        private static BreakableLoot Container(DungeonLoot loot, LootSource kind, int room, Vector3 local, GemBalance balance)
        {
            var name = kind == LootSource.Chest ? "Loot_Chest" : "Loot_Crate";
            var root = new GameObject(name + "_Room" + (room + 1)) { layer = GameLayers.Interactable };
            root.transform.position = local + Vector3.forward * (room * DungeonLayout.RoomSpacing);
            var health = root.AddComponent<Health>(); health.Configure(kind == LootSource.Chest ? balance.chestHealth : balance.crateHealth);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(kind == LootSource.Chest ? 1.9f : 1.3f, kind == LootSource.Chest ? 1.52f : 1.42f, 1.04f);
            collider.center = Vector3.up * collider.size.y * .5f;
            var intact = VaultbreakersArtBuilder.Model(Folder + name + ".fbx", root.transform, "Intact cache");
            var broken = VaultbreakersArtBuilder.Model(Folder + name + "_Broken.fbx", root.transform, "Broken cache");
            foreach (var renderer in broken.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(DebrisMaterial).ToArray();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            broken.SetActive(false);
            var result = root.AddComponent<BreakableLoot>(); result.Configure(loot, kind, room, intact, broken);
            return result;
        }
        private static Material DebrisMaterial(Material original)
        {
            var path = "Assets/Vaultbreakers/Art/Materials/" + original.name + "_Debris.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(original);
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_BlendModePreserveSpecular", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0); material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
            AssetDatabase.CreateAsset(material, path); return material;
        }
    }
}
