using UnityEngine;

namespace Vaultbreakers.Debugging
{
    // Keep the IMGUI component disabled while the lab is hidden. The separate
    // controller still listens for F1 without paying OnGUI setup cost each frame.
    public sealed class PocDebugView : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal PocDebugPanel Owner;
        private void OnGUI(){if(Owner!=null)Owner.DrawPanel();}
#endif
    }
}
