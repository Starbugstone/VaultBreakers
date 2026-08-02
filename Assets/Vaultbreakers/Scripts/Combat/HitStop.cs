using UnityEngine;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Optional short freeze on a connecting hit. It drives <see cref="Time.timeScale"/> from an
    /// unscaled timer and restores the exact value it captured, so gameplay timers that run on scaled
    /// time simply pause with the rest of the simulation instead of drifting. Overlapping requests
    /// extend the freeze rather than nesting, which is what stops a fast combo from stacking into a
    /// visible stall.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitStop : MonoBehaviour
    {
        private float remaining;
        private float restoreScale = 1f;

        public bool IsActive { get; private set; }

        public void Request(float unscaledDuration)
        {
            if (unscaledDuration <= 0f)
            {
                return;
            }

            if (!IsActive)
            {
                restoreScale = Time.timeScale;
                IsActive = true;
            }

            remaining = Mathf.Max(remaining, unscaledDuration);
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f)
            {
                Release();
            }
        }

        /// <summary>A disabled or destroyed freeze must never leave the game stopped.</summary>
        private void OnDisable() => Release();

        public void Release()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            remaining = 0f;
            Time.timeScale = restoreScale;
        }
    }
}
