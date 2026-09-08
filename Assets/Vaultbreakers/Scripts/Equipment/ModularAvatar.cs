using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vaultbreakers.Equipment
{
    /// <summary>
    /// Switches visual modules without replacing the canonical skeleton or sockets.
    /// Gameplay equipment should call Equip after it has accepted a loadout change.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ModularAvatar : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private EquipmentModule[] modules = Array.Empty<EquipmentModule>();

        private readonly Dictionary<EquipmentSlot, string> equipped = new();
        private bool initialized;

        public Transform ModelRoot => modelRoot;
        public IReadOnlyList<EquipmentModule> Modules => modules;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            equipped.Clear();

            // Explicit null check rather than ?.: the null-conditional operator skips Unity's
            // lifetime check, so a destroyed module would read as alive and throw here.
            for (var index = 0; index < modules.Length; index++)
            {
                var module = modules[index];
                if (module != null)
                {
                    module.SetVisible(false);
                }
            }

            for (var index = 0; index < modules.Length; index++)
            {
                var module = modules[index];
                if (module == null || !module.EquippedByDefault || equipped.ContainsKey(module.Slot))
                {
                    continue;
                }

                module.SetVisible(true);
                equipped.Add(module.Slot, module.VariantId);
            }
        }

        public bool Equip(EquipmentSlot slot, string variantId)
        {
            Initialize();

            if (string.IsNullOrWhiteSpace(variantId))
            {
                return false;
            }

            EquipmentModule requested = null;
            for (var index = 0; index < modules.Length; index++)
            {
                var module = modules[index];
                if (module != null && module.Slot == slot &&
                    string.Equals(module.VariantId, variantId, StringComparison.Ordinal))
                {
                    requested = module;
                    break;
                }
            }

            if (requested == null)
            {
                return false;
            }

            for (var index = 0; index < modules.Length; index++)
            {
                var module = modules[index];
                if (module != null && module.Slot == slot)
                {
                    module.SetVisible(ReferenceEquals(module, requested));
                }
            }

            equipped[slot] = requested.VariantId;
            return true;
        }

        public bool Unequip(EquipmentSlot slot)
        {
            Initialize();
            var changed = false;

            for (var index = 0; index < modules.Length; index++)
            {
                var module = modules[index];
                if (module == null || module.Slot != slot)
                {
                    continue;
                }

                module.SetVisible(false);
                changed = true;
            }

            equipped.Remove(slot);
            return changed;
        }

        public bool TryGetEquipped(EquipmentSlot slot, out string variantId)
        {
            Initialize();
            return equipped.TryGetValue(slot, out variantId);
        }

        public void Configure(Transform newModelRoot, EquipmentModule[] newModules)
        {
            modelRoot = newModelRoot;
            modules = newModules ?? Array.Empty<EquipmentModule>();
            initialized = false;
        }
    }
}
