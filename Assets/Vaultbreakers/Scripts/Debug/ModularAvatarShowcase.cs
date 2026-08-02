using Vaultbreakers.Equipment;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vaultbreakers.Debugging
{
    /// <summary>
    /// Development-scene helper for checking every equipment combination in motion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ModularAvatarShowcase : MonoBehaviour
    {
        [SerializeField] private ModularAvatar avatar;
        [SerializeField, Min(0f)] private float turntableSpeed = 18f;

        private static readonly (EquipmentSlot Slot, string Variant)[] DefaultSet =
        {
            (EquipmentSlot.Helmet, "Scrapper"),
            (EquipmentSlot.Armor, "Scrapper"),
            (EquipmentSlot.Melee, "ScrapHammer"),
            (EquipmentSlot.Ranged, "PulseCaster"),
            (EquipmentSlot.Shield, "AegisEmitter"),
            (EquipmentSlot.Rig, "Reclaimer")
        };

        private static readonly (EquipmentSlot Slot, string Variant)[] AlternateSet =
        {
            (EquipmentSlot.Helmet, "Sentinel"),
            (EquipmentSlot.Armor, "Bulwark"),
            (EquipmentSlot.Melee, "PlasmaCutter"),
            (EquipmentSlot.Ranged, "ArcBlaster"),
            (EquipmentSlot.Shield, "PrismEmitter"),
            (EquipmentSlot.Rig, "Capacitor")
        };

        private void Update()
        {
            transform.Rotate(0f, turntableSpeed * Time.deltaTime, 0f, Space.World);

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                ApplySet(DefaultSet);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                ApplySet(AlternateSet);
            }
        }

        public void Configure(ModularAvatar modularAvatar)
        {
            avatar = modularAvatar;
        }

        private void ApplySet((EquipmentSlot Slot, string Variant)[] set)
        {
            if (avatar == null)
            {
                return;
            }

            for (var index = 0; index < set.Length; index++)
            {
                avatar.Equip(set[index].Slot, set[index].Variant);
            }
        }
    }
}
