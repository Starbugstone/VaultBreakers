using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Equipment;

namespace Vaultbreakers.Tests.EditMode
{
    /// <summary>
    /// Deterministic equipment rules. These run without any generated asset so a broken
    /// FBX export cannot mask a logic regression.
    /// </summary>
    public sealed class ModularAvatarTests
    {
        private readonly List<GameObject> spawned = new();

        [TearDown]
        public void TearDown()
        {
            for (var index = 0; index < spawned.Count; index++)
            {
                if (spawned[index] != null)
                {
                    Object.DestroyImmediate(spawned[index]);
                }
            }

            spawned.Clear();
        }

        [Test]
        public void Initialize_ShowsOnlyTheDefaultVariantOfEachSlot()
        {
            var avatar = CreateAvatar(out var parts);
            avatar.Initialize();

            AssertVisible(parts, "Melee/ScrapHammer", true);
            AssertVisible(parts, "Melee/PlasmaCutter", false);
            AssertVisible(parts, "Ranged/PulseCaster", true);
            AssertVisible(parts, "Ranged/ArcBlaster", false);
        }

        [Test]
        public void Initialize_IsIdempotent()
        {
            var avatar = CreateAvatar(out var parts);
            avatar.Initialize();
            avatar.Equip(EquipmentSlot.Melee, "PlasmaCutter");
            avatar.Initialize();

            Assert.IsTrue(avatar.TryGetEquipped(EquipmentSlot.Melee, out var variant));
            Assert.AreEqual("PlasmaCutter", variant, "A second Initialize call must not silently revert a deliberate equip.");
            AssertVisible(parts, "Melee/PlasmaCutter", true);
        }

        [Test]
        public void Equip_SwapsVisibilityWithinTheSlotOnly()
        {
            var avatar = CreateAvatar(out var parts);

            Assert.IsTrue(avatar.Equip(EquipmentSlot.Melee, "PlasmaCutter"));

            AssertVisible(parts, "Melee/PlasmaCutter", true);
            AssertVisible(parts, "Melee/ScrapHammer", false);
            AssertVisible(parts, "Ranged/PulseCaster", true);
            AssertVisible(parts, "Ranged/ArcBlaster", false);
        }

        [Test]
        public void Equip_UnknownVariant_FailsSafelyAndKeepsTheCurrentModule()
        {
            var avatar = CreateAvatar(out var parts);
            avatar.Equip(EquipmentSlot.Melee, "PlasmaCutter");

            Assert.IsFalse(avatar.Equip(EquipmentSlot.Melee, "__NOT_A_VARIANT__"));

            Assert.IsTrue(avatar.TryGetEquipped(EquipmentSlot.Melee, out var variant));
            Assert.AreEqual("PlasmaCutter", variant);
            AssertVisible(parts, "Melee/PlasmaCutter", true);
            AssertVisible(parts, "Melee/ScrapHammer", false);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Equip_EmptyVariantId_IsRejected(string variantId)
        {
            var avatar = CreateAvatar(out var parts);

            Assert.IsFalse(avatar.Equip(EquipmentSlot.Melee, variantId));
            AssertVisible(parts, "Melee/ScrapHammer", true);
        }

        [Test]
        public void Equip_IsCaseSensitiveSoDataIdsStayStable()
        {
            var avatar = CreateAvatar(out _);

            Assert.IsFalse(avatar.Equip(EquipmentSlot.Melee, "plasmacutter"));
        }

        [Test]
        public void Unequip_HidesEveryModuleInTheSlotAndClearsState()
        {
            var avatar = CreateAvatar(out var parts);

            Assert.IsTrue(avatar.Unequip(EquipmentSlot.Melee));

            AssertVisible(parts, "Melee/ScrapHammer", false);
            AssertVisible(parts, "Melee/PlasmaCutter", false);
            Assert.IsFalse(avatar.TryGetEquipped(EquipmentSlot.Melee, out _));
            AssertVisible(parts, "Ranged/PulseCaster", true);
        }

        [Test]
        public void Unequip_ThenEquip_RestoresTheModule()
        {
            var avatar = CreateAvatar(out var parts);
            avatar.Unequip(EquipmentSlot.Melee);

            Assert.IsTrue(avatar.Equip(EquipmentSlot.Melee, "ScrapHammer"));
            AssertVisible(parts, "Melee/ScrapHammer", true);
        }

        [Test]
        public void TryGetEquipped_ReportsDefaultsWithoutAnExplicitEquip()
        {
            var avatar = CreateAvatar(out _);

            Assert.IsTrue(avatar.TryGetEquipped(EquipmentSlot.Ranged, out var variant));
            Assert.AreEqual("PulseCaster", variant);
        }

        [Test]
        public void TryGetEquipped_ReturnsFalseForASlotWithNoDefault()
        {
            var avatar = CreateAvatar(out _);

            Assert.IsFalse(avatar.TryGetEquipped(EquipmentSlot.Rig, out _));
        }

        [Test]
        public void Configure_ReplacesModulesAndReinitializes()
        {
            var avatar = CreateAvatar(out var parts);
            avatar.Initialize();
            avatar.Equip(EquipmentSlot.Melee, "PlasmaCutter");

            var replacement = new[]
            {
                new EquipmentModule(EquipmentSlot.Melee, "ScrapHammer", true, new[] { parts["Melee/ScrapHammer"] })
            };
            avatar.Configure(avatar.ModelRoot, replacement);

            Assert.IsTrue(avatar.TryGetEquipped(EquipmentSlot.Melee, out var variant));
            Assert.AreEqual("ScrapHammer", variant);
            AssertVisible(parts, "Melee/ScrapHammer", true);
        }

        [Test]
        public void Modules_WithNullPartsDoNotThrow()
        {
            var root = new GameObject("AvatarWithHoles");
            spawned.Add(root);
            var avatar = root.AddComponent<ModularAvatar>();
            avatar.Configure(root.transform, new[]
            {
                new EquipmentModule(EquipmentSlot.Armor, "Scrapper", true, new GameObject[] { null }),
                new EquipmentModule(EquipmentSlot.Armor, "Bulwark", false, null)
            });

            Assert.DoesNotThrow(() => avatar.Initialize());
            Assert.IsTrue(avatar.Equip(EquipmentSlot.Armor, "Bulwark"));
        }

        private ModularAvatar CreateAvatar(out Dictionary<string, GameObject> parts)
        {
            var root = new GameObject("TestAvatar");
            spawned.Add(root);
            var modelRoot = new GameObject("ModelRoot");
            modelRoot.transform.SetParent(root.transform, false);

            parts = new Dictionary<string, GameObject>();
            var modules = new List<EquipmentModule>();

            modules.Add(CreateModule(modelRoot.transform, parts, EquipmentSlot.Melee, "ScrapHammer", true, 2));
            modules.Add(CreateModule(modelRoot.transform, parts, EquipmentSlot.Melee, "PlasmaCutter", false, 2));
            modules.Add(CreateModule(modelRoot.transform, parts, EquipmentSlot.Ranged, "PulseCaster", true, 1));
            modules.Add(CreateModule(modelRoot.transform, parts, EquipmentSlot.Ranged, "ArcBlaster", false, 1));
            modules.Add(CreateModule(modelRoot.transform, parts, EquipmentSlot.Rig, "Reclaimer", false, 1));

            var avatar = root.AddComponent<ModularAvatar>();
            avatar.Configure(modelRoot.transform, modules.ToArray());
            return avatar;
        }

        private EquipmentModule CreateModule(
            Transform parent,
            IDictionary<string, GameObject> parts,
            EquipmentSlot slot,
            string variantId,
            bool equippedByDefault,
            int partCount)
        {
            var partObjects = new GameObject[partCount];
            for (var index = 0; index < partCount; index++)
            {
                var part = new GameObject($"VAR_{slot}_{variantId}_Part{index}");
                part.transform.SetParent(parent, false);
                partObjects[index] = part;
            }

            parts[slot + "/" + variantId] = partObjects[0];
            return new EquipmentModule(slot, variantId, equippedByDefault, partObjects);
        }

        private static void AssertVisible(IReadOnlyDictionary<string, GameObject> parts, string key, bool expected)
        {
            Assert.IsTrue(parts.ContainsKey(key), "Test fixture is missing module " + key + ".");
            Assert.AreEqual(expected, parts[key].activeSelf, "Unexpected visibility for " + key + ".");
        }
    }
}
