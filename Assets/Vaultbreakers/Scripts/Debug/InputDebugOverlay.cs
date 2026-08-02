using UnityEngine;
using Vaultbreakers.Input;

namespace Vaultbreakers.Debugging
{
    [DisallowMultipleComponent]
    public sealed class InputDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        public void Configure(PlayerInputReader reader) => input = reader;

        private void OnGUI()
        {
            if (!UnityEngine.Debug.isDebugBuild || input == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(16f, 16f, 430f, 165f), GUI.skin.box);
            GUILayout.Label("INPUT INTENT (development)");
            GUILayout.Label("Device: " + input.CurrentDevice);
            GUILayout.Label($"Move: {input.Move:F2}   Aim: {input.Aim:F2}");
            GUILayout.Label($"Melee: {input.MeleeHeld}   Ranged: {input.RangedHeld}   Shield: {input.ShieldHeld}   Dodge: {input.DodgeHeld}");
            GUILayout.Label("Keyboard: WASD move, arrows aim, LMB melee, E ranged,");
            GUILayout.Label("RMB shield, Space dodge, Esc pause, R restart.");
            GUILayout.EndArea();
        }
    }
}
