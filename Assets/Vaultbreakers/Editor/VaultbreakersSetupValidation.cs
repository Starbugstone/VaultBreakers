using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Equipment;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Post-build gate for the generated assets. The EditMode suite covers the same contract from the
    /// outside; this exists so a broken re-export fails at the moment it is generated instead of
    /// leaving plausible-looking assets on disk until someone runs the tests.
    /// </summary>
    internal static class VaultbreakersSetupValidation
    {
        public static void Validate(UniversalRenderPipelineAsset pipeline, GameObject prefab)
        {
            var failures = new List<string>();

            ValidatePipeline(pipeline, failures);
            ValidateLayers(failures);
            ValidateInputAsset(failures);
            ValidateBalanceAsset(failures);
            ValidateProjectilePrefab(failures);
            ValidatePrefabComponents(prefab, failures);

            if (prefab != null)
            {
                ValidateModulesAndSockets(prefab, failures);
                ValidateMaterials(prefab, failures);
                ValidateRuntimeAvatar(prefab, failures);
            }

            if (!File.Exists(VaultbreakersSetupPaths.PrototypeScenePath) ||
                !File.Exists(VaultbreakersSetupPaths.ShowcaseScenePath))
            {
                failures.Add("One or more generated scenes are missing.");
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Vaultbreakers setup validation failed:\n- " + string.Join("\n- ", failures.Distinct()));
            }
        }

        private static void ValidatePipeline(UniversalRenderPipelineAsset pipeline, ICollection<string> failures)
        {
            if (GraphicsSettings.defaultRenderPipeline != pipeline)
            {
                failures.Add("The new URP asset is not assigned in Graphics Settings.");
            }

            if (pipeline.scriptableRenderer is not UniversalRenderer)
            {
                failures.Add("The active pipeline does not use the 3D Universal Renderer.");
            }
        }

        private static void ValidateLayers(ICollection<string> failures)
        {
            foreach (var definition in GameLayers.All)
            {
                var actual = LayerMask.LayerToName(definition.Index);
                if (actual != definition.Name)
                {
                    failures.Add("Physics layer " + definition.Index + " should be \"" + definition.Name +
                                 "\" but is \"" + actual + "\".");
                }
            }
        }

        private static void ValidateInputAsset(ICollection<string> failures)
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(VaultbreakersSetupPaths.InputActionsPath);
            if (inputActions == null ||
                inputActions.FindActionMap("Player") == null ||
                inputActions.FindActionMap("UI") == null)
            {
                failures.Add("The game-specific Player and UI input maps are missing.");
            }
        }

        private static void ValidatePrefabComponents(GameObject prefab, ICollection<string> failures)
        {
            if (prefab == null)
            {
                failures.Add("The player prefab was not created.");
                return;
            }

            if (prefab.GetComponent<ModularAvatar>() == null)
            {
                failures.Add("The modular avatar prefab is missing its ModularAvatar component.");
            }

            if (prefab.GetComponent<AvatarSocketRegistry>() == null)
            {
                failures.Add("The modular avatar prefab is missing its socket registry.");
            }

            if (prefab.GetComponent<PlayerInput>() == null || prefab.GetComponent<PlayerInputReader>() == null)
            {
                failures.Add("The player prefab is missing its PlayerInput or PlayerInputReader component.");
            }

            if (prefab.layer != GameLayers.Player || prefab.GetComponent<CharacterController>() == null ||
                prefab.GetComponent<PlayerMotor>() == null || prefab.GetComponent<PlayerFacing>() == null)
            {
                failures.Add("The player prefab is missing its controller, motor, facing, or Player layer assignment.");
            }

            if (prefab.GetComponent<Health>() == null || prefab.GetComponent<PlayerActionCoordinator>() == null)
            {
                failures.Add("The player prefab is missing its health or action coordinator.");
            }

            var melee = prefab.GetComponent<MeleeController>();
            if (melee == null || prefab.GetComponent<MeleePresentation>() == null ||
                prefab.GetComponent<HitStop>() == null)
            {
                failures.Add("The player prefab is missing its melee controller, presentation, or hit-stop.");
                return;
            }

            var pool = prefab.GetComponent<ProjectilePool>();
            if (pool == null || prefab.GetComponent<RangedController>() == null ||
                prefab.GetComponent<RangedPresentation>() == null)
            {
                failures.Add("The player prefab is missing its projectile pool, ranged controller, or presentation.");
                return;
            }

            // A prefab that kept the component defaults instead of the balance asset would still play,
            // just with tuning nobody can find. Assert the wiring actually took.
            var balance = AssetDatabase.LoadAssetAtPath<PrototypeBalance>(VaultbreakersSetupPaths.BalancePath);
            if (balance != null && !Mathf.Approximately(melee.Range, balance.MeleeRange))
            {
                failures.Add("The melee controller did not take its range from PrototypeBalance.asset.");
            }
        }

        private static void ValidateBalanceAsset(ICollection<string> failures)
        {
            if (AssetDatabase.LoadAssetAtPath<PrototypeBalance>(VaultbreakersSetupPaths.BalancePath) == null)
            {
                failures.Add("PrototypeBalance.asset is missing; tuning has nowhere central to live.");
            }
        }

        /// <summary>
        /// The projectile deliberately carries no collider, so the usual "does it have physics" check
        /// would be wrong here. What must hold is the component, the layer, and a visible tracer.
        /// </summary>
        private static void ValidateProjectilePrefab(ICollection<string> failures)
        {
            var projectile = AssetDatabase.LoadAssetAtPath<GameObject>(VaultbreakersSetupPaths.ProjectilePrefabPath);
            if (projectile == null)
            {
                failures.Add("The pooled player projectile prefab was not created.");
                return;
            }

            if (projectile.GetComponent<Projectile>() == null)
            {
                failures.Add("The player projectile prefab is missing its Projectile component.");
            }

            if (projectile.layer != GameLayers.PlayerProjectile)
            {
                failures.Add("The player projectile prefab is not on the PlayerProjectile layer.");
            }

            if (projectile.GetComponentInChildren<Renderer>(true) == null)
            {
                failures.Add("The player projectile prefab has no visible tracer.");
            }
        }

        private static void ValidateModulesAndSockets(GameObject prefab, ICollection<string> failures)
        {
            var avatar = prefab.GetComponent<ModularAvatar>();
            if (avatar != null)
            {
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    if (!avatar.Modules.Any(module => module.Slot == slot))
                    {
                        failures.Add("No imported equipment variants were found for slot " + slot + ".");
                    }
                }
            }

            var registry = prefab.GetComponent<AvatarSocketRegistry>();
            if (registry == null)
            {
                return;
            }

            foreach (var requiredSocket in VaultbreakersSetupPaths.RequiredSockets)
            {
                if (!registry.TryGet(requiredSocket.Id, out _))
                {
                    failures.Add("Socket registry is missing " + requiredSocket.Id + ".");
                }
            }
        }

        private static void ValidateMaterials(GameObject prefab, ICollection<string> failures)
        {
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null ||
                        material.shader.name != VaultbreakersSetupPaths.LitShaderName)
                    {
                        failures.Add("Renderer has a missing or non-URP material: " + renderer.name + ".");
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates the prefab and drives every equipment swap, because module visibility is only
        /// observable once the avatar is live.
        /// </summary>
        private static void ValidateRuntimeAvatar(GameObject prefab, ICollection<string> failures)
        {
            if (PrefabUtility.InstantiatePrefab(prefab) is not GameObject instance)
            {
                failures.Add("Could not instantiate the avatar prefab for module-switch validation.");
                return;
            }

            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                ValidateSocketPlacement(instance, failures);

                var avatar = instance.GetComponent<ModularAvatar>();
                if (avatar == null)
                {
                    return;
                }

                avatar.Initialize();

                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    ValidateSlotSwaps(avatar, slot, failures);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void ValidateSlotSwaps(ModularAvatar avatar, EquipmentSlot slot, ICollection<string> failures)
        {
            var slotModules = avatar.Modules.Where(module => module.Slot == slot).ToArray();
            if (slotModules.Length < 2)
            {
                failures.Add("Slot " + slot + " must contain at least two variants for swap validation.");
                return;
            }

            foreach (var requested in slotModules)
            {
                if (!avatar.Equip(slot, requested.VariantId))
                {
                    failures.Add("Equip rejected imported variant " + slot + "/" + requested.VariantId + ".");
                    continue;
                }

                foreach (var module in slotModules)
                {
                    var expectedActive = ReferenceEquals(module, requested);
                    foreach (var part in module.Parts)
                    {
                        if (part != null && part.activeSelf != expectedActive)
                        {
                            failures.Add("Module visibility mismatch after equipping " + slot + "/" +
                                         requested.VariantId + ": " + part.name + ".");
                        }
                    }
                }
            }

            if (avatar.Equip(slot, "__INVALID_VARIANT__"))
            {
                failures.Add("Slot " + slot + " accepted an unknown variant ID.");
            }
        }

        /// <summary>
        /// Sockets are the versioned equipment API. A stale Blender matrix silently collapses every
        /// empty onto the model origin, which still imports, still swaps modules, and still renders,
        /// so it has to be checked explicitly rather than inferred from a successful import.
        /// </summary>
        private static void ValidateSocketPlacement(GameObject instance, ICollection<string> failures)
        {
            var registry = instance.GetComponent<AvatarSocketRegistry>();
            if (registry == null)
            {
                return;
            }

            var placed = new List<(AvatarSocketId Id, Vector3 Position)>();
            foreach (var requiredSocket in VaultbreakersSetupPaths.RequiredSockets)
            {
                if (!registry.TryGet(requiredSocket.Id, out var socket))
                {
                    continue;
                }

                if (Vector3.Dot(socket.forward, instance.transform.forward) < 0.99f ||
                    Vector3.Dot(socket.up, instance.transform.up) < 0.99f)
                {
                    failures.Add(requiredSocket.Name + " is not aligned with gameplay facing (forward " +
                                 socket.forward + ").");
                }

                var offset = socket.position - instance.transform.position;
                if (requiredSocket.Id != AvatarSocketId.Feet && offset.magnitude < 0.5f)
                {
                    failures.Add(requiredSocket.Name + " collapsed onto the model origin; the export lost its world matrix.");
                }

                foreach (var other in placed)
                {
                    if (Vector3.Distance(other.Position, socket.position) < 0.001f)
                    {
                        failures.Add(requiredSocket.Name + " shares a position with " + other.Id + ".");
                    }
                }

                placed.Add((requiredSocket.Id, socket.position));
            }
        }
    }
}
