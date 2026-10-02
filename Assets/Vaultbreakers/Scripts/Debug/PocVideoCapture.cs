using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Vaultbreakers.Debugging
{
    /// <summary>Explicit development-only image sequence for reproducible gameplay videos.</summary>
    public sealed class PocVideoCapture : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string folder;
        private int oldCaptureRate, oldVsync, oldTargetRate;
        private bool recording;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnableRequestedRecording()
        {
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"--poc-video");
            if(index<0 || index+1>=args.Length)return;
            var host=new GameObject("Opt-in gameplay recording");
            host.AddComponent<PocVideoCapture>().folder=args[index+1];
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(folder);
            oldCaptureRate=Time.captureFramerate;oldVsync=QualitySettings.vSyncCount;oldTargetRate=Application.targetFrameRate;
            Time.captureFramerate=30;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;recording=true;
            var end=new WaitForEndOfFrame();
            // Record actual rendered gameplay. Timings from this run are not performance evidence.
            for(var frame=0;frame<5400;frame++)
            {
                yield return end;
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,frame.ToString("D06")+".png"));
            }
            Restore();
        }
        private void Restore()
        {
            if(!recording)return;
            Time.captureFramerate=oldCaptureRate;QualitySettings.vSyncCount=oldVsync;Application.targetFrameRate=oldTargetRate;recording=false;
        }
        private void OnDestroy()=>Restore();
#endif
    }
}
