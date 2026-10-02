using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vaultbreakers.Editor
{
    // Original, deterministic surface maps. No downloaded textures or runtime generation.
    internal static class VaultbreakersSurfaceBuilder
    {
        private const string Folder = "Assets/Vaultbreakers/Art/Textures/Dock9";
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            const int size = 256;
            var color = new Color[size * size];
            var normal = new Color[color.Length];
            var mask = new Color[color.Length];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                // Periodic machining grain and restrained scuffs, intentionally low contrast.
                var grain = Mathf.Sin(x * 2.1f) * .002f;
                var mottling = Mathf.PerlinNoise(x / 31f, y / 31f) * .025f;
                var seam = x < 2 || y < 2 ? .015f : 0;
                var value = .88f + grain + mottling - seam;
                var index = y * size + x;
                color[index] = new Color(value, value, value, 1);
                normal[index] = new Color(.5f + grain * .2f, .5f, 1, 1);
                mask[index] = new Color(.3f, 1, 0, .28f + mottling);
            }
            Write("MachinedColor", color, size, true, false);
            Write("MachinedNormal", normal, size, false, true);
            Write("MachinedMask", mask, size, false, false);
            foreach (var name in new[] { "DG_Tile", "DG_TileLight", "DG_VaultTile", "DG_Stone", "DG_Steel", "DG_BlackGlass" })
            {
                var material = VaultbreakersRenderSetup.LoadMaterial(name);
                material.SetTexture("_BaseMap", Load("MachinedColor"));
                material.SetTexture("_BumpMap", Load("MachinedNormal"));
                material.SetFloat("_BumpScale", .1f);
                material.EnableKeyword("_NORMALMAP");
                // Only decking uses the packed mask; armor retains its individual metal finish.
                if (name is "DG_Tile" or "DG_VaultTile")
                {
                    material.SetTexture("_MetallicGlossMap", Load("MachinedMask"));
                    material.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                EditorUtility.SetDirty(material);
            }
        }
        private static Texture2D Load(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + name + ".png");
        private static void Write(string name, Color[] pixels, int size, bool srgb, bool normal)
        {
            var path = Folder + "/" + name + ".png";
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, !srgb);
            texture.SetPixels(pixels); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb; importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true; importer.anisoLevel = 4;
            importer.SaveAndReimport();
        }
    }
}
