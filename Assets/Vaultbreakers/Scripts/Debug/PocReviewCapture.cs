using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Profiling;
using Vaultbreakers.Combat;
using Vaultbreakers.Zones;
using Vaultbreakers.UI;

namespace Vaultbreakers.Debugging
{
    // Opt-in development-build review. It is never enabled in a normal play session.
    public sealed class PocReviewCapture : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private ZoneController zone;
        private string output;
        private Gamepad pad;
        private bool review, journeyRun;
        private Vaultbreakers.Dungeon.DungeonJourney journey;
        private int capturedRoom;
        private int exitRoom;
        private bool exitCentered;
        private float started;
        private ProfilerRecorder gc;
        private long maxAllocation,totalAllocation;
        private float totalFrame,maxFrame;
        private int samples;
        private bool captureConfigured,oldRunInBackground;
        private InputSettings.BackgroundBehavior oldBackground;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
#endif
        private float duration=12;
        private readonly float[] frameSamples=new float[100000];
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"--poc-review");
            if(index<0 || index+1>=args.Length){enabled=false;yield break;}
            oldRunInBackground=Application.runInBackground;oldBackground=InputSystem.settings.backgroundBehavior;
            Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            captureConfigured=true;
            output=args[index+1];Directory.CreateDirectory(output);journeyRun=Array.IndexOf(args,"--poc-journey")>=0;
            var durationIndex=Array.IndexOf(args,"--poc-seconds");if(durationIndex>=0 && durationIndex+1<args.Length && float.TryParse(args[durationIndex+1],out var seconds))duration=Mathf.Clamp(seconds,12,600);
            zone=GetComponent<ZoneController>();journey=GetComponent<Vaultbreakers.Dungeon.DungeonJourney>();
            yield return null;yield return null;
            pad=InputSystem.AddDevice<Gamepad>("ReviewController");
            var playerInput=zone.Player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if(!playerInput.user.valid){playerInput.enabled=false;playerInput.enabled=true;}
            playerInput.SwitchCurrentControlScheme("Gamepad",pad);
            zone.Player.SetInvulnerable(true);zone.StartSelectedWave(journeyRun?0:2);
            yield return new WaitForSeconds(1);
            // Keep the three-role workload alive throughout the synthetic performance sample.
            if(!journeyRun)foreach(var enemy in zone.Spawner.Roster.Instances)if(enemy.gameObject.activeSelf)enemy.Health.Configure(1000000);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-arena.png"));
            yield return new WaitForSeconds(.5f);
            started=Time.unscaledTime;review=true;
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            if(journeyRun)
            {
                var deadline=Time.unscaledTime+120;
                while(journey!=null && !journey.TreasureClaimed && Time.unscaledTime<deadline)
                {
                    if(capturedRoom!=zone.WaveNumber){capturedRoom=zone.WaveNumber;yield return new WaitForSecondsRealtime(.6f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"zone-"+capturedRoom+".png"));}
                    yield return null;
                }
            }
            else yield return new WaitForSecondsRealtime(duration);
            review=false;
            if(Array.IndexOf(args,"--poc-stress-flashes")>=0)
            {
                var feedback=GetComponent<ArcadeFeedback>();
                for(var i=0;i<8;i++)feedback.Burst(zone.Player.transform.position+Vector3.up*(.4f+i*.1f),Color.white,10);
                yield return null;
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"02-combat.png"));
            yield return new WaitForSecondsRealtime(1);
            review=false;InputSystem.QueueStateEvent(pad,new GamepadState());
            FeedbackSettings.Grayscale=true;FeedbackSettings.Bloom=false;FeedbackSettings.Flashes=false;
            yield return new WaitForSecondsRealtime(1);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"03-grayscale.png"));
            yield return new WaitForSecondsRealtime(1);
            zone.SetPaused(true);
            yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"04-pause.png"));
            yield return new WaitForSecondsRealtime(1);
            var stored=Mathf.Min(samples,frameSamples.Length);Array.Sort(frameSamples,0,stored);
            var p95=stored>0?frameSamples[Mathf.Min(stored-1,Mathf.FloorToInt(stored*.95f))]*1000:0;
            File.WriteAllText(Path.Combine(output,"metrics.json"),"{\"durationSeconds\":"+(Time.unscaledTime-started).ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"p95FrameMs\":"+p95.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"journeyRun\":"+(journeyRun?"true":"false")+",\"completed\":"+(journey!=null && journey.TreasureClaimed?"true":"false")+",\"frames\":"+samples+",\"meanFrameMs\":"+(samples>0?totalFrame/samples*1000:0).ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"maxFrameMs\":"+(maxFrame*1000).ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"meanGcBytes\":"+(samples>0?totalAllocation/samples:0)+",\"maxGcBytes\":"+maxAllocation+",\"gpu\":\""+SystemInfo.graphicsDeviceName+"\"}");
            gc.Dispose();InputSystem.RemoveDevice(pad);pad=null;zone.SetPaused(false);RestoreCaptureSettings();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit(0);
#endif
        }
        private void Update()
        {
            if(!review || pad==null)return;
            var elapsed=Time.unscaledTime-started;
            var state=new GamepadState {leftStick=new Vector2(Mathf.Sin(elapsed*.8f),Mathf.Cos(elapsed*.8f))*.7f,rightStick=new Vector2(-Mathf.Sin(elapsed*.8f),-Mathf.Cos(elapsed*.8f)),rightTrigger=elapsed%4<2?1:0,leftTrigger=elapsed%4>=2?1:0};
            var nearest=Vector3.zero;var distance=float.MaxValue;
            foreach(var enemy in zone.Spawner.Roster.Instances)
            {
                if(!enemy.gameObject.activeSelf || enemy.Health.IsDead)continue;
                var offset=enemy.transform.position-zone.Player.transform.position;offset.y=0;
                if(offset.sqrMagnitude<distance){distance=offset.sqrMagnitude;nearest=offset.normalized;}
            }
            if(distance<float.MaxValue)
            {
                var right=Camera.main.transform.right;right.y=0;right.Normalize();
                var forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();
                state.rightStick=new Vector2(Vector3.Dot(nearest,right),Vector3.Dot(nearest,forward));
            }
            if(elapsed%2<.15f)state=state.WithButton(GamepadButton.South);
            if(elapsed%1.2f<.15f)state=state.WithButton(GamepadButton.West);
            if(journeyRun)
            {
                var right=Camera.main.transform.right;right.y=0;right.Normalize();var forward=Camera.main.transform.forward;forward.y=0;forward.Normalize();
                var movement=distance>2?nearest:Vector3.zero;
                state=new GamepadState {rightStick=new Vector2(Vector3.Dot(nearest,right),Vector3.Dot(nearest,forward)),rightTrigger=distance>2.5f?1:0};
                if(distance<=3)state=state.WithButton(GamepadButton.West);
                if(zone.State==ZoneState.BetweenWaves || zone.State==ZoneState.Complete)
                {
                    // Walk through the centre of the doorway, just as a player must;
                    // a direct diagonal to the next room can run into a gate post.
                    if(exitRoom!=zone.WaveNumber){exitRoom=zone.WaveNumber;exitCentered=false;}
                    var centre=zone.RoomOrigin;var toCentre=centre-zone.Player.transform.position;toCentre.y=0;
                    if(toCentre.sqrMagnitude<.36f)exitCentered=true;
                    var destination=!exitCentered?centre:zone.State==ZoneState.Complete?new Vector3(0,0,44.1f):zone.RoomOrigin+Vector3.forward*19.6f;
                    movement=(destination-zone.Player.transform.position);movement.y=0;movement.Normalize();
                    state.rightTrigger=0;
                }
                state.leftStick=new Vector2(Vector3.Dot(movement,right),Vector3.Dot(movement,forward))*.85f;
            }
            InputSystem.QueueStateEvent(pad,state);
            if(elapsed>2){if(samples<frameSamples.Length)frameSamples[samples]=Time.unscaledDeltaTime;samples++;totalFrame+=Time.unscaledDeltaTime;maxFrame=Mathf.Max(maxFrame,Time.unscaledDeltaTime);var bytes=gc.Valid?gc.LastValue:0;totalAllocation+=bytes;maxAllocation=Math.Max(maxAllocation,bytes);}
        }
        private void RestoreCaptureSettings()
        {
            if(!captureConfigured)return;captureConfigured=false;
            Application.runInBackground=oldRunInBackground;InputSystem.settings.backgroundBehavior=oldBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
#endif
        }
        private void OnDestroy(){RestoreCaptureSettings();if(gc.Valid)gc.Dispose();if(pad!=null)InputSystem.RemoveDevice(pad);}
#endif
    }
}
