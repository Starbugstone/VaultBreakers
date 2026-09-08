using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Debugging;
using Vaultbreakers.Equipment;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Imports the Blender FBX and rebuilds the playable avatar prefab from it. The exported object
    /// names are the contract: geometry is grouped into equipment modules by name, and sockets are
    /// bound by name once here so nothing searches the hierarchy at runtime.
    /// </summary>
    internal static class VaultbreakersAvatarBuilder
    {
        public static void ConfigureModelImporter()
        {
            AssetDatabase.ImportAsset(
                VaultbreakersSetupPaths.ModelPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(VaultbreakersSetupPaths.ModelPath) is not ModelImporter importer)
            {
                throw new FileNotFoundException(
                    "The generated Blender FBX was not imported.", VaultbreakersSetupPaths.ModelPath);
            }

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.isReadable = false;
            importer.addCollider = false;
            importer.importBlendShapes = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;

            // The remap has to run against an imported model, so the settings are applied first and
            // the remapped materials are committed by the second reimport.
            importer.SaveAndReimport();
            VaultbreakersArtBuilder.RemapMaterials(importer);
        }

        public static GameObject BuildPrefab(PrototypeBalance balance, GameObject projectilePrefab)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(VaultbreakersSetupPaths.ModelPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException("Vaultbreaker model asset is unavailable after import.");
            }

            var prefabRoot = new GameObject("PF_Vaultbreaker_POC");
            try
            {
                var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                if (modelInstance == null)
                {
                    throw new InvalidOperationException("Could not instantiate the imported Vaultbreaker model.");
                }

                modelInstance.name = "ModelRoot";
                modelInstance.transform.SetParent(prefabRoot.transform, false);

                var descendants = modelInstance.GetComponentsInChildren<Transform>(true);

                var avatar = prefabRoot.AddComponent<ModularAvatar>();
                avatar.Configure(modelInstance.transform, BuildModules(descendants));

                var registry = prefabRoot.AddComponent<AvatarSocketRegistry>();
                registry.Configure(BuildSocketBindings(descendants));

                AddGameplayComponents(prefabRoot, modelInstance.transform, registry, balance, projectilePrefab);
                VaultbreakersArtBuilder.Animate(prefabRoot, modelInstance);
                avatar.Initialize();

                return PrefabUtility.SaveAsPrefabAsset(prefabRoot, VaultbreakersSetupPaths.PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabRoot);
            }
        }

        /// <summary>
        /// Groups every exported <c>VAR_Slot_Variant_Part</c> object into one module per slot and
        /// variant, ordered deterministically so a re-export produces an identical prefab.
        /// </summary>
        private static EquipmentModule[] BuildModules(IEnumerable<Transform> descendants)
        {
            var groupedParts = new Dictionary<(EquipmentSlot Slot, string Variant), List<GameObject>>();

            foreach (var descendant in descendants)
            {
                if (!TryParseVariant(descendant.name, out var slot, out var variant))
                {
                    continue;
                }

                var key = (slot, variant);
                if (!groupedParts.TryGetValue(key, out var parts))
                {
                    parts = new List<GameObject>();
                    groupedParts.Add(key, parts);
                }

                parts.Add(descendant.gameObject);
            }

            return groupedParts
                .OrderBy(pair => pair.Key.Slot)
                .ThenBy(pair => pair.Key.Variant, StringComparer.Ordinal)
                .Select(pair => new EquipmentModule(
                    pair.Key.Slot,
                    pair.Key.Variant,
                    VaultbreakersSetupPaths.DefaultVariants.TryGetValue(pair.Key.Slot, out var defaultId) &&
                    defaultId == pair.Key.Variant,
                    pair.Value.OrderBy(part => part.name, StringComparer.Ordinal).ToArray()))
                .ToArray();
        }

        private static AvatarSocketBinding[] BuildSocketBindings(Transform[] descendants)
        {
            var bindings = new List<AvatarSocketBinding>();
            foreach (var requiredSocket in VaultbreakersSetupPaths.RequiredSockets)
            {
                var socket = descendants.FirstOrDefault(candidate => candidate.name == requiredSocket.Name);
                if (socket == null)
                {
                    throw new InvalidOperationException("Required avatar socket is missing: " + requiredSocket.Name);
                }

                bindings.Add(new AvatarSocketBinding(requiredSocket.Id, socket));
            }

            return bindings.ToArray();
        }

        private static void AddGameplayComponents(
            GameObject prefabRoot,
            Transform modelRoot,
            AvatarSocketRegistry registry,
            PrototypeBalance balance,
            GameObject projectilePrefab)
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(VaultbreakersSetupPaths.InputActionsPath);
            if (actions == null)
            {
                throw new InvalidOperationException(
                    "Input actions asset is missing: " + VaultbreakersSetupPaths.InputActionsPath);
            }

            var playerInput = prefabRoot.AddComponent<PlayerInput>();
            playerInput.actions = actions;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            var inputReader = prefabRoot.AddComponent<PlayerInputReader>();
            prefabRoot.AddComponent<InputDebugOverlay>().Configure(inputReader);

            prefabRoot.layer = GameLayers.Player;
            var characterController = prefabRoot.AddComponent<CharacterController>();
            characterController.radius = 0.42f;
            characterController.height = 1.8f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.skinWidth = 0.04f;
            characterController.stepOffset = 0.25f;

            // The camera is resolved at runtime from the scene the prefab is dropped into.
            var motor = prefabRoot.AddComponent<PlayerMotor>();
            motor.Configure(inputReader, null, balance);
            var facing = prefabRoot.AddComponent<PlayerFacing>();
            facing.Configure(inputReader, motor, modelRoot, null, balance);

            var health = prefabRoot.AddComponent<Health>();
            health.Configure(balance != null ? balance.PlayerMaximumHealth : 100f);
            var coordinator = prefabRoot.AddComponent<PlayerActionCoordinator>();
            coordinator.Configure(health);

            var hitStop = prefabRoot.AddComponent<HitStop>();
            var melee = prefabRoot.AddComponent<MeleeController>();
            melee.Configure(balance, inputReader, coordinator, facing, health, hitStop);
            prefabRoot.AddComponent<MeleePresentation>().Configure(
                melee,
                registry,
                VaultbreakersRenderSetup.LoadMaterial("VB_MeleeArc"),
                balance != null ? balance.MeleeAttackHeight : 1f);

            var pool = prefabRoot.AddComponent<ProjectilePool>();
            pool.Configure(
                projectilePrefab != null ? projectilePrefab.GetComponent<Projectile>() : null,
                balance != null ? balance.ProjectilePoolSize : 32,
                balance != null ? balance.ProjectileRadius : 0.12f);

            var ranged = prefabRoot.AddComponent<RangedController>();
            ranged.Configure(balance, inputReader, coordinator, facing, health, registry, pool);
            prefabRoot.AddComponent<RangedPresentation>().Configure(
                ranged,
                registry,
                VaultbreakersRenderSetup.LoadMaterial("VB_ProjectileCore"),
                0.22f,
                0.28f);

            // The camera is resolved at runtime, like the motor's, because shield facing is aimed in
            // camera-relative space.
            var shield = prefabRoot.AddComponent<ShieldController>();
            shield.Configure(balance, inputReader, coordinator, facing, motor, health, null);
            prefabRoot.AddComponent<ShieldPresentation>().Configure(
                shield,
                VaultbreakersRenderSetup.LoadMaterial("VB_ShieldArc"),
                VaultbreakersRenderSetup.LoadMaterial("VB_ShieldMarker"),
                0.15f);

            // The dodge is given the melee controller because it refuses to interrupt a swing that is
            // already dealing damage, which is a question only the melee phase can answer.
            var dodge = prefabRoot.AddComponent<DodgeController>();
            dodge.Configure(balance, inputReader, coordinator, facing, motor, health, melee);
            prefabRoot.AddComponent<DodgePresentation>().Configure(
                dodge,
                modelRoot,
                VaultbreakersRenderSetup.LoadMaterial("VB_DodgeStreak"),
                0.04f);
        }

        /// <summary>
        /// Parses the module naming contract from Docs/MODULAR_AVATAR_PIPELINE.md:
        /// <c>VAR_&lt;Slot&gt;_&lt;VariantId&gt;_&lt;PartName&gt;</c>. Part names may contain
        /// underscores; variant IDs may not in schema version 1.
        /// </summary>
        public static bool TryParseVariant(string objectName, out EquipmentSlot slot, out string variant)
        {
            slot = default;
            variant = string.Empty;

            if (!objectName.StartsWith("VAR_", StringComparison.Ordinal))
            {
                return false;
            }

            var segments = objectName.Split('_');
            if (segments.Length < 4 || !Enum.TryParse(segments[1], false, out slot))
            {
                return false;
            }

            variant = segments[2];
            return !string.IsNullOrWhiteSpace(variant);
        }
    }
}
