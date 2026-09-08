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

            renderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if(renderer.postProcessData==null)throw new InvalidOperationException("URP post-process resources are missing.");
            EditorUtility.SetDirty(renderer);

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
            var shadowSettings = new SerializedObject(pipeline);
            shadowSettings.FindProperty("m_SoftShadowsSupported").boolValue = true;
            shadowSettings.ApplyModifiedPropertiesWithoutUndo();
            pipeline.shadowNormalBias = .35f;
            pipeline.shadowDepthBias = .6f;
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

            CreateOrUpdate(shader,"DG_BlackGlass",new Color(.025f,.07f,.105f),.2f,.4f);
            CreateOrUpdate(shader,"DG_VaultTile",new Color(.12f,.23f,.28f),.15f,.3f);
            CreateOrUpdate(shader,"DG_CoreWhite",new Color(.68f,.82f,.85f),.15f,.3f);
            CreateOrUpdate(shader, "DG_Skin", new Color(0.91f,0.56f,0.32f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Hair", new Color(0.18f,0.075f,0.035f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Cloth", new Color(0.035f,0.28f,0.48f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Cape", new Color(0.9f,0.22f,0.09f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Leather", new Color(0.17f,0.075f,0.035f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Gold", new Color(0.95f,0.6f,0.13f), .35f, .3f);
            CreateOrUpdate(shader, "DG_Steel", new Color(0.59f,0.8f,0.87f), .35f, .3f);
            CreateOrUpdate(shader, "DG_Ink", new Color(0.015f,0.025f,0.038f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Eye", new Color(0.92f,0.98f,1.0f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Goblin", new Color(.38f,.43f,.43f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Hood", new Color(.13f,.24f,.34f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Stone", new Color(0.25f,0.35f,0.37f), 0f, .3f);
            CreateOrUpdate(shader, "DG_StoneLight", new Color(0.4f,0.49f,0.46f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Tile", new Color(0.3f,0.39f,0.34f), 0f, .3f);
            CreateOrUpdate(shader, "DG_TileLight", new Color(0.4f,0.47f,0.38f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Earth", new Color(0.095f,0.16f,0.15f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Moss", new Color(0.21f,0.38f,0.16f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Leaf", new Color(0.095f,0.31f,0.2f), 0f, .3f);
            CreateOrUpdate(shader, "DG_LeafLight", new Color(0.24f,0.49f,0.22f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Bark", new Color(0.18f,0.12f,0.085f), 0f, .3f);
            CreateOrUpdate(shader, "DG_Rune", new Color(0.1f,0.85f,0.91f), 0f, .3f, new Color(0.1f,0.85f,0.91f) * 2f);
            CreateOrUpdate(shader, "DG_Flame", new Color(1.0f,0.52f,0.1f), 0f, .3f, new Color(1.0f,0.52f,0.1f) * 2f);
            CreateOrUpdate(shader, "DG_Crystal", new Color(0.45f,0.2f,0.8f), 0f, .3f, new Color(0.45f,0.2f,0.8f) * 2f);
            CreateOrUpdate(shader, "DG_Petal", new Color(0.81f,0.31f,0.6f), 0f, .3f);
            CreateOrUpdate(shader, "VB_StageFloor", new Color(.018f,.032f,.068f), .3f, .5f);
            CreateOrUpdate(shader, "VB_StageTile", new Color(.06f,.09f,.15f), .4f, .4f);
            CreateOrUpdate(shader, "VB_Undersuit", new Color(0.035f, 0.045f, 0.055f), 0.15f, 0.38f);
            CreateOrUpdate(shader, "VB_SalvageMetal", new Color(0.08f, 0.27f, 0.38f), 0.45f, 0.28f);
            CreateOrUpdate(shader, "VB_DarkMetal", new Color(0.045f, 0.055f, 0.065f), 0.82f, 0.22f);
            CreateOrUpdate(shader, "VB_HazardOrange", new Color(0.88f, 0.24f, 0.045f), 0.32f, 0.30f);
            CreateOrUpdate(shader, "VB_Ceramic", new Color(0.72f, 0.85f, 0.9f), 0.2f, 0.36f);
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

            // The shield band has to be seen through, or it hides the threat it is protecting from.
            CreateOrUpdate(shader, "VB_ShieldArc", new Color(0.35f, 0.75f, 1f, 0.32f), 0f, 0.85f,
                new Color(0.15f, 0.6f, 1f) * 2.2f);
            CreateOrUpdate(shader, "VB_ShieldMarker", new Color(0.85f, 0.93f, 1f), 0f, 0.7f,
                new Color(0.5f, 0.8f, 1f) * 4f);

            // The dodge streak is a ground trail the player moves out of, so it has to be seen
            // through: an opaque smear would hide whatever they just rolled away from.
            CreateOrUpdate(shader, "VB_DodgeStreak", new Color(0.92f, 0.95f, 1f, 0.28f), 0f, 0.75f,
                new Color(0.6f, 0.75f, 1f) * 2f);
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
            ConfigureSurface(material, baseColor.a);

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

        /// <summary>
        /// URP Lit needs its blend mode, depth write, and keyword set together; assigning an alpha
        /// below one to an opaque material silently does nothing, which is a confusing way to lose an
        /// effect. The alpha of the base colour is the single switch.
        /// </summary>
        private static void ConfigureSurface(Material material, float alpha)
        {
            var transparent = alpha < 1f;

            material.SetFloat("_Surface", transparent ? 1f : 0f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            material.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            material.SetFloat("_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            material.SetShaderPassEnabled("ShadowCaster", !transparent);

            if (transparent)
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            // -1 hands the queue back to the shader. Writing an explicit 2000 instead is functionally
            // identical but rewrites every opaque material on disk for no reason, which turns a
            // regenerated foundation into a diff nobody can skim.
            material.renderQueue = transparent ? (int)RenderQueue.Transparent : -1;
        }
    }
}
