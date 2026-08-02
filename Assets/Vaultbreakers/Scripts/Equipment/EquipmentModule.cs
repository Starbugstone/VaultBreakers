using System;
using UnityEngine;

namespace Vaultbreakers.Equipment
{
    [Serializable]
    public sealed class EquipmentModule
    {
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private string variantId = string.Empty;
        [SerializeField] private bool equippedByDefault;
        [SerializeField] private GameObject[] parts = Array.Empty<GameObject>();

        public EquipmentSlot Slot => slot;
        public string VariantId => variantId;
        public bool EquippedByDefault => equippedByDefault;
        public GameObject[] Parts => parts;

        public EquipmentModule(
            EquipmentSlot slot,
            string variantId,
            bool equippedByDefault,
            GameObject[] parts)
        {
            this.slot = slot;
            this.variantId = variantId;
            this.equippedByDefault = equippedByDefault;
            this.parts = parts ?? Array.Empty<GameObject>();
        }

        public void SetVisible(bool visible)
        {
            for (var index = 0; index < parts.Length; index++)
            {
                if (parts[index] != null)
                {
                    parts[index].SetActive(visible);
                }
            }
        }
    }
}
