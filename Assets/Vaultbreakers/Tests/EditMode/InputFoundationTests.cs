using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Vaultbreakers.Combat;
using Vaultbreakers.Input;
using Vaultbreakers.Player;

namespace Vaultbreakers.Tests.EditMode
{
    public sealed class InputFoundationTests
    {
        private const string ActionsPath = "Assets/Vaultbreakers/Input/VaultbreakersInputActions.inputactions";
        private const string PrefabPath = "Assets/Vaultbreakers/Prefabs/Player/PF_Vaultbreaker_POC.prefab";

        [Test]
        public void InputAsset_HasTheDocumentedMapsAndActions()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.FindActionMap("UI", false), Is.Not.Null);

            var player = asset.FindActionMap("Player", true);
            var expected = new[] { "Move", "Aim", "Melee", "Ranged", "Shield", "Dodge", "Pause", "Restart" };
            Assert.That(player.actions.Select(action => action.name), Is.EquivalentTo(expected));
            Assert.That(player.FindAction("Move", true).expectedControlType, Is.EqualTo("Vector2"));
            Assert.That(player.FindAction("Aim", true).expectedControlType, Is.EqualTo("Vector2"));
        }

        [Test]
        public void InputAsset_HasControllerAndDevelopmentBindings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            var player = asset.FindActionMap("Player", true);

            AssertBinding(player, "Move", "<Gamepad>/leftStick");
            AssertBinding(player, "Aim", "<Gamepad>/rightStick");
            AssertBinding(player, "Melee", "<Gamepad>/buttonWest");
            AssertBinding(player, "Ranged", "<Gamepad>/rightTrigger");
            AssertBinding(player, "Shield", "<Gamepad>/leftTrigger");
            AssertBinding(player, "Dodge", "<Gamepad>/buttonSouth");
            AssertBinding(player, "Pause", "<Gamepad>/start");
            AssertBinding(player, "Restart", "<Keyboard>/r");
            Assert.That(player.FindAction("Move", true).bindings.Any(binding => binding.isComposite && binding.name == "WASD"), Is.True);
        }

        /// <summary>
        /// Keyboard aim has to behave like a stick. A pointer-delta binding only produces a value
        /// while the mouse is physically moving, and its magnitude depends on how fast it moved,
        /// which is not something the facing dead zone can be reasoned about against.
        /// </summary>
        [Test]
        public void InputAsset_KeyboardAimIsADirectionalCompositeNotPointerDelta()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            var aim = asset.FindActionMap("Player", true).FindAction("Aim", true);

            Assert.That(aim.bindings.Any(binding => binding.isComposite && binding.name == "Arrows"), Is.True,
                "Aim has no keyboard 2DVector composite.");

            foreach (var path in new[] { "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow" })
            {
                Assert.That(aim.bindings.Any(binding => binding.path == path), Is.True, "Aim is missing " + path + ".");
            }

            Assert.That(aim.bindings.Any(binding => binding.path.Contains("delta")), Is.False,
                "Pointer delta is a relative motion value and cannot drive absolute facing.");
        }

        /// <summary>
        /// A StickDeadzone on the action rescales the remaining range, so an asset dead zone plus the
        /// PlayerFacing threshold would silently push the real aim dead zone well past the documented
        /// 0.25. The gameplay threshold is the only one, because it is the testable one.
        /// </summary>
        [Test]
        public void InputAsset_AppliesTheAimDeadZoneOnlyOnceInGameplay()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            var player = asset.FindActionMap("Player", true);

            Assert.That(player.FindAction("Aim", true).processors, Is.Empty,
                "The aim dead zone belongs to PlayerFacing, not the input asset.");
            Assert.That(player.FindAction("Move", true).processors, Does.Contain("StickDeadzone"),
                "Move still needs a hardware dead zone; no gameplay code applies one.");
        }

        [Test]
        public void PlayerPrefab_CarriesTheInputLocomotionAndCombatComponents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, "Run Vaultbreakers > Setup > Build 3D Foundation and Modular Avatar first.");

            var playerInput = prefab.GetComponent<PlayerInput>();
            Assert.That(playerInput, Is.Not.Null);
            Assert.That(playerInput.actions, Is.Not.Null);
            Assert.That(playerInput.defaultActionMap, Is.EqualTo("Player"));
            Assert.That(prefab.GetComponent<PlayerInputReader>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerMotor>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerFacing>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Health>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerActionCoordinator>(), Is.Not.Null);
        }

        private static void AssertBinding(InputActionMap map, string actionName, string path)
        {
            Assert.That(map.FindAction(actionName, true).bindings.Any(binding => binding.path == path), Is.True,
                actionName + " should bind " + path + ".");
        }
    }
}
