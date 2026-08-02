using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Creates the 3D Universal Renderer and the graybox material set. The project was created from
    /// the 2D URP template, so the renderer type is asserted rather than assumed.
    /// </summary>
    internal static class VaultbreakersRenderSetup
    {
        public static UniversalRenderPipelineAsset ConfigurePipeline()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(VaultbreakersSetupPaths.RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "VaultbreakersUniversalRenderer";
                AssetDatabase.CreateAsset(renderer, VaultbreakersSetupPaths.RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(VaultbreakersSetupPaths.PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "VaultbreakersURP";
                AssetDatabase.CreateAsset(pipeline, VaultbreakersSetupPaths.PipelinePath);
            }

            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.renderScale = 1f;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            AssignToEveryQualityLevel(pipeline);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            return pipeline;
        }

        /// <summary>
        /// A quality level left on the template pipeline renders the arena with the 2D renderer, so
        /// every level is pointed at the Vaultbreakers asset explicitly.
        /// </summary>
        private static void AssignToEveryQualityLevel(UniversalRenderPipelineAsset pipeline)
        {
            var previousQuality = QualitySettings.GetQualityLevel();
            for (var qualityIndex = 0; qualityIndex < QualitySettings.names.Length; qualityIndex++)
            {
                QualitySettings.SetQualityLevel(qualityIndex, false);
                QualitySettings.renderPipeline = pipeline;
            }

            QualitySettings.SetQualityLevel(previousQuality, false);
        }

        public static void CreateMaterials()
        {
            var shader = Shader.Find(VaultbreakersSetupPaths.LitShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "URP Lit shader is unavailable; the 3D pipeline cannot be configured safely.");
            }

            CreateOrUpdate(shader, "VB_Undersuit", new Color(0.035f, 0.045f, 0.055f), 0.15f, 0.38f);
            CreateOrUpdate(shader, "VB_SalvageMetal", new Color(0.18f, 0.22f, 0.24f), 0.72f, 0.28f);
            CreateOrUpdate(shader, "VB_DarkMetal", new Color(0.045f, 0.055f, 0.065f), 0.82f, 0.22f);
            CreateOrUpdate(shader, "VB_HazardOrange", new Color(0.88f, 0.24f, 0.045f), 0.32f, 0.30f);
            CreateOrUpdate(shader, "VB_Ceramic", new Color(0.63f, 0.64f, 0.57f), 0.25f, 0.36f);
            CreateOrUpdate(shader, "VB_EnergyCyan", new Color(0.02f, 0.48f, 0.68f), 0.15f, 0.20f, new Color(0f, 0.72f, 1f) * 3.5f);
            CreateOrUpdate(shader, "VB_EnergyViolet", new Color(0.40f, 0.07f, 0.62f), 0.12f, 0.22f, new Color(0.65f, 0.08f, 1f) * 3f);
            CreateOrUpdate(shader, "VB_Visor", new Color(0.015f, 0.16f, 0.20f), 0.55f, 0.08f, new Color(0f, 0.62f, 0.78f) * 2.5f);
            CreateOrUpdate(shader, "VB_ArenaFloor", new Color(0.055f, 0.065f, 0.075f), 0.25f, 0.62f);
            CreateOrUpdate(shader, "VB_ArenaWall", new Color(0.14f, 0.16f, 0.17f), 0.45f, 0.48f);
            CreateOrUpdate(shader, "VB_ArenaAccent", new Color(0.72f, 0.16f, 0.035f), 0.25f, 0.38f, new Color(0.55f, 0.055f, 0.005f));
            CreateOrUpdate(shader, "VB_MeleeArc", new Color(0.95f, 0.62f, 0.12f), 0f, 0.6f, new Color(1f, 0.5f, 0.08f) * 4f);

            // Player ranged fire reads cyan. Phase 9 gives enemy projectiles a different palette and
            // silhouette, so this colour is the player's half of that contrast.
            CreateOrUpdate(shader, "VB_ProjectileCore", new Color(0.2f, 0.85f, 1f), 0f, 0.7f, new Color(0.1f, 0.8f, 1f) * 5f);
        }

        public static Material LoadMaterial(string materialName) =>
            AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(materialName));

        private static string MaterialPath(string materialName) =>
            VaultbreakersSetupPaths.MaterialsFolder + "/" + materialName + ".mat";

        private static void CreateOrUpdate(
            Shader shader,
            string materialName,
            Color baseColor,
            float metallic,
            float smoothness,
            Color? emission = null)
        {
            var path = MaterialPath(materialName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
        }
    }
}
