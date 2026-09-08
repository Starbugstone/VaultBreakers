using System;
using UnityEditor;
using UnityEngine;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Idempotent project and avatar setup, and the executable source of truth for every generated
    /// Unity asset. Each step lives in its own file; this type only owns the order they run in and
    /// the menu entry points, which are also the batch-mode entry points used by the build scripts.
    /// </summary>
    public static class VaultbreakersProjectSetup
    {
        [MenuItem("Vaultbreakers/Setup/Build 3D Foundation and Modular Avatar")]
        public static void BuildAll()
        {
            try
            {
                EnsureFolders();
                VaultbreakersPhysicsSetup.Apply();

                var pipeline = VaultbreakersRenderSetup.ConfigurePipeline();
                VaultbreakersRenderSetup.CreateMaterials();
                VaultbreakersArtBuilder.Prepare();
                var balance = VaultbreakersDataSetup.EnsureBalanceAsset();
                var projectile = VaultbreakersProjectileBuilder.BuildPlayerProjectile(balance);

                VaultbreakersAvatarBuilder.ConfigureModelImporter();
                var prefab = VaultbreakersAvatarBuilder.BuildPrefab(balance, projectile);

                VaultbreakersSceneBuilder.BuildPrototypeArena(prefab);
                VaultbreakersDungeonBuilder.Build();
                VaultbreakersSceneBuilder.BuildShowcaseScene(prefab);
                VaultbreakersSceneBuilder.BuildTestBedScene();
                VaultbreakersSceneBuilder.ConfigureBuildScenes();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                VaultbreakersSetupValidation.Validate(pipeline, prefab);
                Debug.Log("Vaultbreakers 3D foundation and modular avatar setup completed successfully.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem("Vaultbreakers/Setup/Capture Avatar Showcase Preview")]
        public static void CaptureShowcasePreview() => VaultbreakersPreviewCapture.CaptureShowcase();

        private static void EnsureFolders()
        {
            foreach (var folder in VaultbreakersSetupPaths.ProjectFolders)
            {
                EnsureFolder(folder);
            }
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Replace('\\', '/').Split('/');
            var current = segments[0];

            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
