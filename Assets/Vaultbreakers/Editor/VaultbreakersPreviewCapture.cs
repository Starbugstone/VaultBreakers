using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Vaultbreakers.Equipment;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Renders the documentation images from the showcase scene. Kept apart from the setup tool
    /// because it is the only step that needs a real graphics device.
    /// </summary>
    internal static class VaultbreakersPreviewCapture
    {
        private const int Resolution = 768;
        private const string DefaultImagePath = "Docs/Images/Vaultbreaker_Unity_Preview.png";
        private const string AlternateImagePath = "Docs/Images/Vaultbreaker_Unity_Alternate_Preview.png";

        public static void CaptureShowcase()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                throw new InvalidOperationException(
                    "Preview capture needs a real graphics device. Run the editor without -nographics, " +
                    "otherwise every capture silently overwrites the documentation images with a blank frame.");
            }

            EditorSceneManager.OpenScene(VaultbreakersSetupPaths.ShowcaseScenePath, OpenSceneMode.Single);
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException("The avatar showcase scene has no camera.");
            }

            var rig=UnityEngine.Object.FindAnyObjectByType<ModularAvatar>();
            var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/Vaultbreakers/Art/Animation/Vaultbreaker_Animations.fbx").OfType<AnimationClip>().First(c=>c.name=="Idle");
            idle.SampleAnimation(rig.transform.Find("ModelRoot").gameObject,0);
            CaptureCamera(camera, DefaultImagePath);

            var avatar = UnityEngine.Object.FindAnyObjectByType<ModularAvatar>();
            if (avatar == null)
            {
                return;
            }

            avatar.Equip(EquipmentSlot.Helmet, "Sentinel");
            avatar.Equip(EquipmentSlot.Armor, "Bulwark");
            avatar.Equip(EquipmentSlot.Melee, "PlasmaCutter");
            avatar.Equip(EquipmentSlot.Ranged, "ArcBlaster");
            avatar.Equip(EquipmentSlot.Shield, "PrismEmitter");
            avatar.Equip(EquipmentSlot.Rig, "Capacitor");
            CaptureCamera(camera, AlternateImagePath);
        }

        private static void CaptureCamera(Camera camera, string relativeOutputPath)
        {
            var renderTexture = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;

            try
            {
                camera.aspect = 1f;
                camera.targetTexture = renderTexture;
                // The first SRP render initializes material buffers in a freshly opened editor.
                // Discard that warm-up before reading the actual colored image.
                camera.Render();
                camera.Render();
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
                image.Apply(false, false);

                var outputPath = Path.GetFullPath(relativeOutputPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "Docs/Images");
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                UnityEngine.Debug.Log("Captured Unity avatar preview: " + outputPath);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
