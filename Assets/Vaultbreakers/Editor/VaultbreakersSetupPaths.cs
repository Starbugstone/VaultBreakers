using System.Collections.Generic;
using Vaultbreakers.Equipment;

namespace Vaultbreakers.Editor
{
    /// <summary>
    /// Every generated asset location and the avatar contract the setup steps share. Keeping these
    /// in one place stops the builder, the scene generator, and the validator from drifting apart.
    /// </summary>
    internal static class VaultbreakersSetupPaths
    {
        public const string Root = "Assets/Vaultbreakers";
        public const string SettingsFolder = Root + "/Settings";
        public const string MaterialsFolder = Root + "/Art/Materials";
        public const string PlayerFolder = Root + "/Art/Characters/Player";
        public const string PrefabFolder = Root + "/Prefabs/Player";
        public const string PrototypeSceneFolder = Root + "/Scenes/Prototype";
        public const string TestSceneFolder = Root + "/Scenes/Test";
        public const string BalanceFolder = Root + "/Data/Balance";
        public const string ProjectilePrefabFolder = Root + "/Prefabs/Projectiles";

        public const string BalancePath = BalanceFolder + "/PrototypeBalance.asset";
        public const string ProjectilePrefabPath = ProjectilePrefabFolder + "/PF_PlayerProjectile.prefab";

        public const string RendererPath = SettingsFolder + "/VaultbreakersUniversalRenderer.asset";
        public const string PipelinePath = SettingsFolder + "/VaultbreakersURP.asset";
        public const string ModelPath = PlayerFolder + "/Vaultbreaker_Modular.fbx";
        public const string InputActionsPath = Root + "/Input/VaultbreakersInputActions.inputactions";
        public const string PrefabPath = PrefabFolder + "/PF_Vaultbreaker_POC.prefab";
        public const string PrototypeScenePath = PrototypeSceneFolder + "/Prototype_Arena.unity";
        public const string ShowcaseScenePath = TestSceneFolder + "/Avatar_Showcase.unity";

        /// <summary>
        /// A deliberately empty scene the PlayMode suite loads before building its own rigs. Without
        /// it, whichever generated scene a previous test left loaded keeps its colliders in the
        /// physics world and silently answers the next test's queries.
        /// </summary>
        public const string TestBedScenePath = TestSceneFolder + "/Empty_TestBed.unity";

        public const string LitShaderName = "Universal Render Pipeline/Lit";

        /// <summary>The default loadout the generated prefab ships with.</summary>
        public static readonly IReadOnlyDictionary<EquipmentSlot, string> DefaultVariants =
            new Dictionary<EquipmentSlot, string>
            {
                { EquipmentSlot.Helmet, "Scrapper" },
                { EquipmentSlot.Armor, "Scrapper" },
                { EquipmentSlot.Melee, "ScrapHammer" },
                { EquipmentSlot.Ranged, "PulseCaster" },
                { EquipmentSlot.Shield, "AegisEmitter" },
                { EquipmentSlot.Rig, "Reclaimer" }
            };

        /// <summary>
        /// The socket names exported by the Blender generator, in the order documented in
        /// Docs/MODULAR_AVATAR_PIPELINE.md. These names are versioned API.
        /// </summary>
        public static readonly (AvatarSocketId Id, string Name)[] RequiredSockets =
        {
            (AvatarSocketId.RightHandMelee, "SOCKET_RightHand_Melee"),
            (AvatarSocketId.LeftArmRangedShield, "SOCKET_LeftArm_RangedShield"),
            (AvatarSocketId.Back, "SOCKET_Back"),
            (AvatarSocketId.PetAnchor, "SOCKET_PetAnchor"),
            (AvatarSocketId.Muzzle, "ANCHOR_Muzzle"),
            (AvatarSocketId.Shield, "ANCHOR_Shield"),
            (AvatarSocketId.MeleeTrail, "ANCHOR_MeleeTrail"),
            (AvatarSocketId.Hit, "ANCHOR_Hit"),
            (AvatarSocketId.Feet, "ANCHOR_Feet")
        };

        /// <summary>Folders the project layout in COMBAT_POC_PLAN.md section 5 requires.</summary>
        public static readonly string[] ProjectFolders =
        {
            Root + "/Art/Animation",
            Root + "/Art/Characters/Enemies",
            PlayerFolder,
            Root + "/Art/Environments",
            MaterialsFolder,
            Root + "/Art/UI",
            Root + "/Art/VFX",
            Root + "/Audio",
            BalanceFolder,
            Root + "/Data/Enemies",
            Root + "/Data/Waves",
            Root + "/Input",
            PrefabFolder,
            Root + "/Prefabs/Enemies",
            ProjectilePrefabFolder,
            Root + "/Prefabs/UI",
            Root + "/Prefabs/Zones",
            PrototypeSceneFolder,
            TestSceneFolder,
            Root + "/Scripts/Camera",
            Root + "/Scripts/Combat",
            Root + "/Scripts/Core",
            Root + "/Scripts/Debug",
            Root + "/Scripts/Enemies",
            Root + "/Scripts/Equipment",
            Root + "/Scripts/Input",
            Root + "/Scripts/Player",
            Root + "/Scripts/UI",
            Root + "/Scripts/Zones",
            SettingsFolder,
            Root + "/Tests/EditMode",
            Root + "/Tests/PlayMode"
        };
    }
}
