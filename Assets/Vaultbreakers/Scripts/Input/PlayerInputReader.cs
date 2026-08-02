using UnityEngine;
using UnityEngine.InputSystem;

namespace Vaultbreakers.Input
{
    /// <summary>
    /// Captures player intent and nothing else. This reader never moves a transform, resolves a
    /// target, or touches health; gameplay translation belongs to the motor, facing, and combat
    /// controllers that read these values.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private PlayerInput playerInput;
        private InputAction move;
        private InputAction aim;
        private InputAction melee;
        private InputAction ranged;
        private InputAction shield;
        private InputAction dodge;
        private InputAction pause;
        private InputAction restart;
        private bool actionsCached;

        public Vector2 Move => ReadVector(move);
        public Vector2 Aim => ReadVector(aim);
        public bool MeleeHeld => IsHeld(melee);
        public bool RangedHeld => IsHeld(ranged);
        public bool ShieldHeld => IsHeld(shield);
        public bool DodgeHeld => IsHeld(dodge);
        public bool MeleePressedThisFrame => WasPressed(melee);
        public bool DodgePressedThisFrame => WasPressed(dodge);
        public bool PausePressedThisFrame => WasPressed(pause);
        public bool RestartPressedThisFrame => WasPressed(restart);

        public string CurrentDevice => playerInput != null && playerInput.devices.Count > 0
            ? playerInput.devices[0].displayName
            : "No paired device";

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            CacheActions();
        }

        private void OnEnable()
        {
            CacheActions();
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        /// <summary>
        /// Enabling and disabling action maps belongs to PlayerInput. This only resolves the action
        /// handles once, and retries on enable in case the asset was assigned after Awake.
        /// </summary>
        private void CacheActions()
        {
            if (actionsCached)
            {
                return;
            }

            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInput>();
            }

            var actions = playerInput != null ? playerInput.actions : null;
            if (actions == null)
            {
                return;
            }

            move = Require(actions, "Move");
            aim = Require(actions, "Aim");
            melee = Require(actions, "Melee");
            ranged = Require(actions, "Ranged");
            shield = Require(actions, "Shield");
            dodge = Require(actions, "Dodge");
            pause = Require(actions, "Pause");
            restart = Require(actions, "Restart");
            actionsCached = true;
        }

        /// <summary>
        /// A controller unplugged mid-hold leaves its buttons latched in the device state, which
        /// would read as a held trigger forever. Resetting the device clears that without disabling
        /// action maps the reader does not own.
        /// </summary>
        private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device == null || !device.added)
            {
                return;
            }

            if (change is InputDeviceChange.Disconnected or InputDeviceChange.Disabled)
            {
                InputSystem.ResetDevice(device);
            }
        }

        private static InputAction Require(InputActionAsset actions, string name) =>
            actions.FindAction("Player/" + name, true);

        private static Vector2 ReadVector(InputAction action) => action?.ReadValue<Vector2>() ?? Vector2.zero;

        private static bool IsHeld(InputAction action) => action != null && action.IsPressed();

        private static bool WasPressed(InputAction action) => action != null && action.WasPressedThisFrame();
    }
}
