using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Debugging;
using Vaultbreakers.Equipment;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Regenerates the two authored scenes. Both are disposable: everything they contain is written
    /// here, so a scene can always be rebuilt from the tool rather than repaired by hand.
    /// </summary>
    internal static class VaultbreakersSceneBuilder
    {
        private const float ArenaHalfExtent = 10f;

        public static void BuildPrototypeArena(GameObject avatarPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Prototype_Arena";
            ConfigureAmbientLighting();

            BuildArenaGeometry();
            BuildGameplayMarkers();

            var zoneRoot = new GameObject("ZoneRoot");
            var player = InstantiateAvatar(avatarPrefab, "Player", Quaternion.identity);

            // Three dummies rather than one: a primary target, a second close enough to be caught by
            // the same swing, and a flanking target that has to be turned toward. Phase 5 cannot be
            // judged against a single practice target standing in one place.
            var dummy = BuildTargetDummy("TargetDummy", new Vector3(0f, 1f, 4f));
            BuildTargetDummy("TargetDummy_Pair", new Vector3(1.3f, 1f, 4.3f));
            BuildTargetDummy("TargetDummy_Flank", new Vector3(-4.5f, 1f, 1.5f));

            var combatOverlay = zoneRoot.AddComponent<CombatDebugOverlay>();
            combatOverlay.Configure(
                player != null ? player.GetComponent<Health>() : null,
                dummy.GetComponent<Health>(),
                player != null ? player.GetComponent<PlayerActionCoordinator>() : null,
                player != null ? player.GetComponent<MeleeController>() : null,
                player != null ? player.GetComponent<RangedController>() : null);

            CreateDirectionalLight();
            CreateCamera("Main Camera", new Vector3(10.5f, 13f, -10.5f), Vector3.zero, true, 11.5f);

            EditorSceneManager.SaveScene(scene, VaultbreakersSetupPaths.PrototypeScenePath);
        }

        public static void BuildShowcaseScene(GameObject avatarPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Avatar_Showcase";
            ConfigureAmbientLighting();

            CreatePrimitive("Turntable", PrimitiveType.Cylinder, null, new Vector3(0f, -0.08f, 0f),
                new Vector3(1.8f, 0.12f, 1.8f), VaultbreakersRenderSetup.LoadMaterial("VB_ArenaFloor"));

            var avatar = InstantiateAvatar(avatarPrefab, "Vaultbreaker_Modular_Showcase", Quaternion.Euler(0f, 180f, 0f));
            if (avatar == null)
            {
                throw new InvalidOperationException("Could not instantiate the modular avatar in the showcase scene.");
            }

            StripGameplayComponents(avatar);
            avatar.AddComponent<ModularAvatarShowcase>().Configure(avatar.GetComponent<ModularAvatar>());

            CreateDirectionalLight(2.2f);
            CreatePointLight("Cyan_Fill", new Vector3(2.5f, 2.2f, -1.5f), new Color(0.05f, 0.55f, 1f), 35f, 7f);
            CreatePointLight("Orange_Rim", new Vector3(-2.5f, 2.5f, 1.2f), new Color(1f, 0.16f, 0.02f), 45f, 7f);
            CreateCamera("Main Camera", new Vector3(3.4f, 2.55f, -5.2f), new Vector3(0f, 1.0f, 0f), false, 42f);

            // Hierarchy-visible reminder of the showcase controls; carries no behaviour.
            new GameObject("Instructions_Use_1_And_2_To_Swap_Loadouts");

            EditorSceneManager.SaveScene(scene, VaultbreakersSetupPaths.ShowcaseScenePath);
        }

        /// <summary>
        /// An empty, collider-free scene for the automated suite to start from. PlayMode tests share
        /// one physics world, so a test that builds its own rig has to be able to clear whatever the
        /// previous test loaded first.
        /// </summary>
        public static void BuildTestBedScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Empty_TestBed";
            ConfigureAmbientLighting();

            CreateDirectionalLight();
            CreateCamera("Main Camera", new Vector3(0f, 3f, -8f), Vector3.zero, false, 60f);

            EditorSceneManager.SaveScene(scene, VaultbreakersSetupPaths.TestBedScenePath);
        }

        public static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(VaultbreakersSetupPaths.PrototypeScenePath, true),
                new EditorBuildSettingsScene(VaultbreakersSetupPaths.ShowcaseScenePath, true),
                new EditorBuildSettingsScene(VaultbreakersSetupPaths.TestBedScenePath, true)
            };
        }

        /// <summary>
        /// The showcase is a visual test bench. Left intact, the prefab's locomotion would drive the
        /// avatar off the turntable on WASD and PlayerFacing would fight the turntable rotation, so
        /// the gameplay half of the prefab is removed here. Order follows the RequireComponent chain.
        /// </summary>
        private static void StripGameplayComponents(GameObject avatar)
        {
            RemoveComponent<InputDebugOverlay>(avatar);
            RemoveComponent<RangedPresentation>(avatar);
            RemoveComponent<RangedController>(avatar);
            RemoveComponent<ProjectilePool>(avatar);
            RemoveComponent<MeleePresentation>(avatar);
            RemoveComponent<MeleeController>(avatar);
            RemoveComponent<HitStop>(avatar);
            RemoveComponent<PlayerActionCoordinator>(avatar);
            RemoveComponent<Health>(avatar);
            RemoveComponent<PlayerFacing>(avatar);
            RemoveComponent<PlayerMotor>(avatar);
            RemoveComponent<CharacterController>(avatar);
            RemoveComponent<PlayerInputReader>(avatar);
            RemoveComponent<PlayerInput>(avatar);
        }

        private static void RemoveComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static GameObject InstantiateAvatar(GameObject avatarPrefab, string name, Quaternion rotation)
        {
            if (PrefabUtility.InstantiatePrefab(avatarPrefab) is not GameObject instance)
            {
                return null;
            }

            instance.name = name;
            instance.transform.SetPositionAndRotation(Vector3.zero, rotation);
            return instance;
        }

        private static void BuildArenaGeometry()
        {
            var environment = new GameObject("Environment") { layer = GameLayers.Environment };
            var floorMaterial = VaultbreakersRenderSetup.LoadMaterial("VB_ArenaFloor");
            var wallMaterial = VaultbreakersRenderSetup.LoadMaterial("VB_ArenaWall");

            const float wallCentre = ArenaHalfExtent + 0.25f;
            const float span = ArenaHalfExtent * 2f;
            const float wallSpan = span + 0.5f;

            CreatePrimitive("Floor", PrimitiveType.Cube, environment.transform, new Vector3(0f, -0.25f, 0f),
                new Vector3(span, 0.5f, span), floorMaterial, GameLayers.Environment);
            CreatePrimitive("Wall_North", PrimitiveType.Cube, environment.transform, new Vector3(0f, 0.75f, wallCentre),
                new Vector3(wallSpan, 2f, 0.5f), wallMaterial, GameLayers.Environment);
            CreatePrimitive("Wall_South", PrimitiveType.Cube, environment.transform, new Vector3(0f, 0.75f, -wallCentre),
                new Vector3(wallSpan, 2f, 0.5f), wallMaterial, GameLayers.Environment);
            CreatePrimitive("Wall_East", PrimitiveType.Cube, environment.transform, new Vector3(wallCentre, 0.75f, 0f),
                new Vector3(0.5f, 2f, wallSpan), wallMaterial, GameLayers.Environment);
            CreatePrimitive("Wall_West", PrimitiveType.Cube, environment.transform, new Vector3(-wallCentre, 0.75f, 0f),
                new Vector3(0.5f, 2f, wallSpan), wallMaterial, GameLayers.Environment);
        }

        private static void BuildGameplayMarkers()
        {
            var markers = new GameObject("GameplayMarkers");
            CreateMarker("PlayerStart", markers.transform, Vector3.zero);
            CreateMarker("SpawnPoint_A", markers.transform, new Vector3(-5f, 0.05f, 4f));
            CreateMarker("SpawnPoint_B", markers.transform, new Vector3(5f, 0.05f, 4f));
            CreateMarker("SpawnPoint_C", markers.transform, new Vector3(0f, 0.05f, -5f));
        }

        private static GameObject BuildTargetDummy(string name, Vector3 position)
        {
            var dummy = CreatePrimitive(name, PrimitiveType.Capsule, null, position,
                Vector3.one, VaultbreakersRenderSetup.LoadMaterial("VB_Ceramic"), GameLayers.Enemy);
            dummy.AddComponent<Health>().Configure(50f);
            dummy.AddComponent<TargetDummy>();
            return dummy;
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            int layer = 0)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.layer = layer;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static void CreateMarker(string name, Transform parent, Vector3 position)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;

            var icon = CreatePrimitive(name + "_Visual", PrimitiveType.Cylinder, marker.transform, position,
                new Vector3(0.55f, 0.025f, 0.55f), VaultbreakersRenderSetup.LoadMaterial("VB_ArenaAccent"), GameLayers.Debug);
            icon.GetComponent<Collider>().enabled = false;
        }

        private static void CreateDirectionalLight(float intensity = 1.35f)
        {
            var lightObject = new GameObject("Key Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.91f, 0.82f);
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
        }

        private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.position = position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void ConfigureAmbientLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.20f, 0.24f, 0.31f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.12f, 0.16f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.04f, 0.055f);
            RenderSettings.ambientIntensity = 1.15f;
            RenderSettings.reflectionIntensity = 0.55f;
        }

        private static void CreateCamera(string name, Vector3 position, Vector3 target, bool orthographic, float sizeOrFov)
        {
            var cameraObject = new GameObject(name) { tag = "MainCamera" };
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.LookRotation(target - position, Vector3.up);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = orthographic;
            if (orthographic)
            {
                camera.orthographicSize = sizeOrFov;
            }
            else
            {
                camera.fieldOfView = sizeOrFov;
            }

            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.016f, 0.025f);
            cameraObject.AddComponent<AudioListener>();
        }
    }
}
