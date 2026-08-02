using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Vaultbreakers.Equipment;

namespace Vaultbreakers.Tests.EditMode
{
    /// <summary>
    /// Guards the generated 3D foundation and the modular avatar contract documented in
    /// Docs/MODULAR_AVATAR_PIPELINE.md. These fail loudly if a Blender re-export or an
    /// editor-setup change breaks the skeleton, sockets, materials, or import settings.
    /// </summary>
    public sealed class GeneratedFoundationTests
    {
        private const string ModelPath = "Assets/Vaultbreakers/Art/Characters/Player/Vaultbreaker_Modular.fbx";
        private const string PrefabPath = "Assets/Vaultbreakers/Prefabs/Player/PF_Vaultbreaker_POC.prefab";
        private const string PipelinePath = "Assets/Vaultbreakers/Settings/VaultbreakersURP.asset";
        private const string PrototypeScenePath = "Assets/Vaultbreakers/Scenes/Prototype/Prototype_Arena.unity";
        private const string ShowcaseScenePath = "Assets/Vaultbreakers/Scenes/Test/Avatar_Showcase.unity";
        private const string LitShader = "Universal Render Pipeline/Lit";

        private static readonly string[] CanonicalBones =
        {
            "Root", "Hips", "Spine", "Chest", "Neck", "Head",
            "UpperArm_L", "LowerArm_L", "Hand_L",
            "UpperArm_R", "LowerArm_R", "Hand_R",
            "UpperLeg_L", "LowerLeg_L", "Foot_L",
            "UpperLeg_R", "LowerLeg_R", "Foot_R"
        };

        private static readonly string[] RequiredSocketNames =
        {
            "SOCKET_RightHand_Melee", "SOCKET_LeftArm_RangedShield", "SOCKET_Back", "SOCKET_PetAnchor",
            "ANCHOR_Muzzle", "ANCHOR_Shield", "ANCHOR_MeleeTrail", "ANCHOR_Hit", "ANCHOR_Feet"
        };

        private GameObject prefab;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Run Vaultbreakers > Setup > Build 3D Foundation and Modular Avatar before testing.");
        }

        [Test]
        public void Pipeline_IsAssignedAndUsesTheThreeDimensionalUniversalRenderer()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            Assert.IsNotNull(pipeline, "The Vaultbreakers URP asset is missing.");
            Assert.AreSame(pipeline, GraphicsSettings.defaultRenderPipeline, "The Vaultbreakers URP asset is not the default pipeline.");
            Assert.IsInstanceOf<UniversalRenderer>(pipeline.scriptableRenderer, "The pipeline must use the 3D Universal Renderer, not the 2D renderer.");
            Assert.IsTrue(pipeline.supportsCameraDepthTexture, "Depth texture is required for 3D gameplay effects.");
            Assert.AreEqual(ColorSpace.Linear, PlayerSettings.colorSpace);
        }

        [Test]
        public void Pipeline_IsAssignedToEveryQualityLevel()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var previous = QualitySettings.GetQualityLevel();

            try
            {
                for (var level = 0; level < QualitySettings.names.Length; level++)
                {
                    QualitySettings.SetQualityLevel(level, false);
                    Assert.AreSame(pipeline, QualitySettings.renderPipeline,
                        "Quality level " + QualitySettings.names[level] + " does not use the Vaultbreakers pipeline.");
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(previous, false);
            }
        }

        [Test]
        public void ModelImporter_MatchesTheDocumentedImportContract()
        {
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            Assert.IsNotNull(importer, "The modular avatar FBX is not imported.");

            Assert.AreEqual(1f, importer.globalScale, "One Unity unit must equal one metre.");
            Assert.IsTrue(importer.useFileScale);
            Assert.IsFalse(importer.importCameras);
            Assert.IsFalse(importer.importLights);
            Assert.IsFalse(importer.importAnimation, "Animation clips must ship from a dedicated export.");
            Assert.AreEqual(ModelImporterAnimationType.Generic, importer.animationType);
            Assert.IsFalse(importer.isReadable);
            Assert.IsFalse(importer.addCollider, "Visual geometry must not generate gameplay colliders.");
            Assert.IsFalse(importer.importBlendShapes);
            Assert.AreEqual(ModelImporterMeshCompression.Off, importer.meshCompression);
        }

        [Test]
        public void Scenes_ExistAndAreEnabledInBuildSettings()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeScenePath), "Prototype_Arena is missing.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(ShowcaseScenePath), "Avatar_Showcase is missing.");

            var enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            CollectionAssert.Contains(enabled, PrototypeScenePath);
            CollectionAssert.Contains(enabled, ShowcaseScenePath);
        }

        [Test]
        public void Skeleton_ContainsEachCanonicalBoneExactlyOnce()
        {
            var counts = new Dictionary<string, int>();
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
            {
                counts.TryGetValue(child.name, out var count);
                counts[child.name] = count + 1;
            }

            foreach (var bone in CanonicalBones)
            {
                Assert.IsTrue(counts.TryGetValue(bone, out var count), "Canonical bone is missing: " + bone + ".");
                Assert.AreEqual(1, count, "Canonical bone " + bone + " appears " + count + " times; a duplicate armature breaks retargeting.");
            }
        }

        [Test]
        public void Prefab_ContainsAtMostOneAnimator()
        {
            var animators = prefab.GetComponentsInChildren<Animator>(true);
            Assert.LessOrEqual(animators.Length, 1, "The player visual must not contain a second armature or Animator.");
        }

        [Test]
        public void Sockets_AreAllRegisteredAndNamedCanonically()
        {
            var registry = prefab.GetComponent<AvatarSocketRegistry>();
            Assert.IsNotNull(registry, "The avatar prefab is missing its socket registry.");

            foreach (AvatarSocketId id in System.Enum.GetValues(typeof(AvatarSocketId)))
            {
                Assert.IsTrue(registry.TryGet(id, out var socket), "Socket registry is missing " + id + ".");
                Assert.IsTrue(socket.IsChildOf(prefab.transform), "Socket " + id + " is not part of the avatar hierarchy.");
            }

            var names = prefab.GetComponentsInChildren<Transform>(true).Select(child => child.name).ToArray();
            foreach (var socketName in RequiredSocketNames)
            {
                Assert.AreEqual(1, names.Count(name => name == socketName), "Expected exactly one " + socketName + ".");
            }
        }

        [Test]
        public void Sockets_AreNeverOwnedByAnEquipmentModule()
        {
            var registry = prefab.GetComponent<AvatarSocketRegistry>();
            var avatar = prefab.GetComponent<ModularAvatar>();
            var moduleParts = avatar.Modules
                .Where(module => module != null)
                .SelectMany(module => module.Parts)
                .Where(part => part != null)
                .Select(part => part.transform)
                .ToArray();

            foreach (AvatarSocketId id in System.Enum.GetValues(typeof(AvatarSocketId)))
            {
                Assert.IsTrue(registry.TryGet(id, out var socket));
                foreach (var part in moduleParts)
                {
                    Assert.IsFalse(socket.IsChildOf(part),
                        "Socket " + id + " lives under equipment " + part.name + ", so an equipment swap would disable it.");
                }
            }
        }

        [Test]
        public void Modules_CoverEverySlotWithAtLeastTwoVariants()
        {
            var avatar = prefab.GetComponent<ModularAvatar>();
            Assert.IsNotNull(avatar, "The avatar prefab is missing its ModularAvatar component.");

            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                var variants = avatar.Modules.Where(module => module.Slot == slot).ToArray();
                Assert.GreaterOrEqual(variants.Length, 2, "Slot " + slot + " needs at least two variants for swap validation.");
                Assert.AreEqual(1, variants.Count(module => module.EquippedByDefault),
                    "Slot " + slot + " must declare exactly one default variant.");
                CollectionAssert.AllItemsAreUnique(variants.Select(module => module.VariantId).ToArray());

                foreach (var module in variants)
                {
                    Assert.IsFalse(module.VariantId.Contains("_"),
                        "Schema version 1 forbids underscores in variant IDs: " + module.VariantId + ".");
                    Assert.IsNotEmpty(module.Parts, "Module " + slot + "/" + module.VariantId + " has no geometry.");
                }
            }
        }

        [Test]
        public void Modules_ClaimEveryExportedVariantObject()
        {
            var avatar = prefab.GetComponent<ModularAvatar>();
            var claimed = new HashSet<Transform>(avatar.Modules
                .SelectMany(module => module.Parts)
                .Where(part => part != null)
                .Select(part => part.transform));

            var orphans = prefab.GetComponentsInChildren<Transform>(true)
                .Where(child => child.name.StartsWith("VAR_") && !claimed.Contains(child))
                .Select(child => child.name)
                .ToArray();

            CollectionAssert.IsEmpty(orphans, "Exported equipment geometry was not grouped into a module: " + string.Join(", ", orphans));
        }

        [Test]
        public void Prefab_SavedStateMatchesTheDeclaredDefaults()
        {
            var avatar = prefab.GetComponent<ModularAvatar>();

            foreach (var module in avatar.Modules)
            {
                foreach (var part in module.Parts)
                {
                    Assert.IsNotNull(part, "Module " + module.Slot + "/" + module.VariantId + " references a missing part.");
                    Assert.AreEqual(module.EquippedByDefault, part.activeSelf,
                        "Saved prefab state disagrees with the default loadout for " + part.name + ".");
                }
            }
        }

        [Test]
        public void Renderers_UseUrpLitMaterialsAndHaveValidBounds()
        {
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                Assert.IsNotEmpty(renderer.sharedMaterials, "Renderer without a material: " + renderer.name + ".");
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.IsNotNull(material, "Missing material on " + renderer.name + " would render pink.");
                    Assert.IsNotNull(material.shader, "Missing shader on " + renderer.name + ".");
                    Assert.AreEqual(LitShader, material.shader.name, "Non-URP material on " + renderer.name + ".");
                }

                var meshFilter = renderer.GetComponent<MeshFilter>();
                Assert.IsNotNull(meshFilter, "Renderer without a mesh: " + renderer.name + ".");
                Assert.IsNotNull(meshFilter.sharedMesh, "Renderer with an unassigned mesh: " + renderer.name + ".");
                Assert.Greater(meshFilter.sharedMesh.bounds.size.sqrMagnitude, 0f, "Degenerate bounds on " + renderer.name + ".");
            }
        }

        [Test]
        public void Avatar_FacesUnityForwardWithUnmirroredHandedness()
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                var visor = Find(instance, "VAR_Helmet_Scrapper_Visor");
                Assert.Greater(visor.position.z, 0f, "The avatar does not face Unity +Z; the export axis preset changed.");

                var melee = Find(instance, "SOCKET_RightHand_Melee");
                var rangedShield = Find(instance, "SOCKET_LeftArm_RangedShield");
                var muzzle = Find(instance, "ANCHOR_Muzzle");
                var shield = Find(instance, "ANCHOR_Shield");

                Assert.Greater(melee.position.x, 0f,
                    "The melee socket is not on the avatar's right; the model was mirrored on export. Position: " + melee.position + ".");
                Assert.Less(rangedShield.position.x, 0f,
                    "The ranged/shield socket is not on the avatar's left; the model was mirrored on export. Position: " + rangedShield.position + ".");
                Assert.Less(muzzle.position.x, 0f, "The muzzle anchor left the left-arm module standard. Position: " + muzzle.position + ".");
                Assert.Less(shield.position.x, 0f, "The shield anchor left the left-arm module standard. Position: " + shield.position + ".");

                var feet = Find(instance, "ANCHOR_Feet");
                Assert.Less(Mathf.Abs(feet.position.y), 0.1f, "The feet anchor is not on the ground plane.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Sockets_ShareTheGameplayFacingOrientation()
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                foreach (var socketName in RequiredSocketNames)
                {
                    var socket = Find(instance, socketName);
                    Assert.Greater(Vector3.Dot(socket.forward, Vector3.forward), 0.9f,
                        socketName + " does not point along the avatar's gameplay facing (" + socket.forward + ").");
                    Assert.Greater(Vector3.Dot(socket.up, Vector3.up), 0.9f,
                        socketName + " is not upright (" + socket.up + ").");
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Sockets_StayWithTheirOwningBody()
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                AssertNear(instance, "SOCKET_RightHand_Melee", "Hand_R", 0.35f);
                AssertNear(instance, "ANCHOR_MeleeTrail", "Hand_R", 0.75f);
                AssertNear(instance, "SOCKET_LeftArm_RangedShield", "LowerArm_L", 0.35f);
                AssertNear(instance, "ANCHOR_Muzzle", "LowerArm_L", 0.7f);
                AssertNear(instance, "ANCHOR_Shield", "LowerArm_L", 0.5f);
                AssertNear(instance, "SOCKET_Back", "Chest", 0.5f);
                AssertNear(instance, "ANCHOR_Hit", "Chest", 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Hierarchy_HasNoNegativeScale()
        {
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
            {
                var scale = child.localScale;
                Assert.Greater(scale.x, 0f, "Negative scale flips normals on " + child.name + ".");
                Assert.Greater(scale.y, 0f, "Negative scale flips normals on " + child.name + ".");
                Assert.Greater(scale.z, 0f, "Negative scale flips normals on " + child.name + ".");
            }
        }

        [Test]
        public void Avatar_StandsOnTheGroundAtHumanScale()
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                var bounds = CalculateBounds(instance);

                Assert.Greater(bounds.size.y, 1.5f, "The avatar is shorter than a readable humanoid.");
                Assert.Less(bounds.size.y, 2.6f, "The avatar is taller than the documented human scale.");
                Assert.Less(Mathf.Abs(bounds.min.y), 0.15f, "The avatar does not stand on the prefab origin plane.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Transform Find(GameObject root, string name)
        {
            var match = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
            Assert.IsNotNull(match, "The avatar hierarchy has no transform named " + name + ".");
            return match;
        }

        private static void AssertNear(GameObject root, string socketName, string boneName, float maxDistance)
        {
            var socket = Find(root, socketName);
            var bone = Find(root, boneName);
            var distance = Vector3.Distance(socket.position, bone.position);
            Assert.LessOrEqual(distance, maxDistance,
                socketName + " drifted " + distance.ToString("F3") + " units from " + boneName + ".");
        }

        private static Bounds CalculateBounds(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(false);
            Assert.IsNotEmpty(renderers, "The avatar prefab has no active renderers.");

            var bounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }
    }
}
