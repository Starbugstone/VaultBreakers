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
        private PlayerInput reviewPlayerInput;
        private bool oldNeverAutoSwitch;
        private InputSettings.BackgroundBehavior oldBackground;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
#endif
        [Serializable] private sealed class SessionResult
        {
            public string kind="Scripted virtual controller; privileged world-state navigation; no human participant";
            public int policy, meleeSwings, shots, guardRaises, blocks, dodges, deaths, shotsWhileGuardRaised;
            public int shotsWhileShieldControllerRaised;
            public float firstWaveSeconds=-1, damageTaken;
            public bool completed, replayReset;
            public int enemyDrops, cratesBroken, chestsBroken, gemsSpawned, gemsCollected, gemTotal, gemsExpired, peakGems;
            public bool lootReplayReset;
            public string understanding="Not measurable by simulation", replayChoice="Scripted replay reset only; no preference inferred";
        }
        private SessionResult session;
        private ShieldController sessionShield;
        private float firstWaveStarted;
        private bool wasRaised;
        private float duration=12;
        private bool lootRun, capturedLoot, gemStress;
        private float nextGemStress;
        private int lastCapturedChest;
        private bool capturedCrateDebris;
        private float nextLootTrace;
        private float nextNavigationTrace;
        private Vaultbreakers.Gems.DungeonLoot loot;
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
            var policyIndex=Array.IndexOf(args,"--poc-policy");
            if(policyIndex>=0 && policyIndex+1<args.Length && int.TryParse(args[policyIndex+1],out var policy))
            {session=new SessionResult{policy=Mathf.Clamp(policy,0,4)};journeyRun=true;}
            var durationIndex=Array.IndexOf(args,"--poc-seconds");if(durationIndex>=0 && durationIndex+1<args.Length && float.TryParse(args[durationIndex+1],out var seconds))duration=Mathf.Clamp(seconds,12,600);
            zone=GetComponent<ZoneController>();journey=GetComponent<Vaultbreakers.Dungeon.DungeonJourney>();
            lootRun=Array.IndexOf(args,"--poc-loot")>=0;loot=GetComponent<Vaultbreakers.Gems.DungeonLoot>();
            gemStress=Array.IndexOf(args,"--poc-gem-stress")>=0;
            yield return null;yield return null;
            pad=InputSystem.AddDevice<Gamepad>("ReviewController");
            var playerInput=zone.Player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if(!playerInput.user.valid){playerInput.enabled=false;playerInput.enabled=true;}
            reviewPlayerInput=playerInput;oldNeverAutoSwitch=playerInput.neverAutoSwitchControlSchemes;
            // Desktop activity must not steal control from this opt-in scripted review.
            playerInput.neverAutoSwitchControlSchemes=true;
            playerInput.SwitchCurrentControlScheme("Gamepad",pad);
            zone.Player.SetInvulnerable(session==null);zone.StartSelectedWave(journeyRun?0:2);
            if(session!=null)
            {
                sessionShield=zone.Player.GetComponent<ShieldController>();
                zone.Player.GetComponent<MeleeController>().SwingStarted+=CountMelee;
                zone.Player.GetComponent<RangedController>().Fired+=CountShot;
                zone.Player.GetComponent<DodgeController>().DodgeStarted+=CountDodge;
                sessionShield.Blocked+=CountBlock;zone.Player.Damaged+=CountDamage;zone.Player.Died+=CountDeath;
            }
            while(zone.State!=ZoneState.Fighting)yield return null;
            firstWaveStarted=Time.unscaledTime;
            yield return new WaitForSecondsRealtime(.2f);
            // Keep the three-role workload alive throughout the synthetic performance sample.
            if(!journeyRun)foreach(var enemy in zone.Spawner.Roster.Instances)if(enemy.gameObject.activeSelf)enemy.Health.Configure(1000000);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"01-arena.png"));
            yield return new WaitForSecondsRealtime(.5f);
            started=Time.unscaledTime;review=true;
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            if(journeyRun)
            {
                var deadline=Time.unscaledTime+(Array.IndexOf(args,"--poc-video")>=0?1800:120);
                while(journey!=null && !journey.TreasureClaimed && Time.unscaledTime<deadline)
                {
                    if(capturedRoom!=zone.WaveNumber){capturedRoom=zone.WaveNumber;yield return new WaitForSecondsRealtime(.6f);ScreenCapture.CaptureScreenshot(Path.Combine(output,"zone-"+capturedRoom+".png"));}
                    yield return null;
                }
            }
            else yield return new WaitForSecondsRealtime(duration);
            review=false;
            if(session!=null)session.completed=journey!=null && journey.TreasureClaimed;
            if(loot!=null)File.WriteAllText(Path.Combine(output,"loot-metrics.json"),"{\"spawned\":"+loot.Pool.SpawnedUnits+",\"collected\":"+loot.Pool.CollectedUnits+",\"expired\":"+loot.Pool.ExpiredUnits+",\"merged\":"+loot.Pool.MergedUnits+",\"active\":"+loot.Pool.ActiveCount+",\"peakActive\":"+loot.Pool.PeakActive+",\"gemStress\":"+(gemStress?"true":"false")+"}");
            if(session!=null && loot!=null)
            {
                session.enemyDrops=loot.EnemyDrops;session.cratesBroken=loot.CratesBroken;session.chestsBroken=loot.ChestsBroken;
                session.gemsSpawned=loot.Pool.SpawnedUnits;session.gemsCollected=loot.Pool.CollectedUnits;session.gemsExpired=loot.Pool.ExpiredUnits;
                session.peakGems=loot.Pool.PeakActive;session.gemTotal=loot.Wallet.Total;
            }
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
            if(session!=null)
            {
                zone.SetPaused(false);InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
                if(session.completed)
                {
                    InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.Start));yield return null;yield return null;
                    InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSecondsRealtime(1);
                    session.replayReset=zone.WaveNumber==1 && zone.State==ZoneState.Fighting && !journey.TreasureClaimed && journey.Score==0;
                    session.lootReplayReset=loot==null || (loot.Wallet.Total==0 && loot.Pool.ActiveCount==0 && loot.CratesBroken==0 && loot.ChestsBroken==0);
                }
                File.WriteAllText(Path.Combine(output,"session.json"),JsonUtility.ToJson(session,true));
                zone.Player.GetComponent<MeleeController>().SwingStarted-=CountMelee;
                zone.Player.GetComponent<RangedController>().Fired-=CountShot;
                zone.Player.GetComponent<DodgeController>().DodgeStarted-=CountDodge;
                sessionShield.Blocked-=CountBlock;zone.Player.Damaged-=CountDamage;zone.Player.Died-=CountDeath;
            }
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
            if(gemStress && loot!=null && elapsed>=nextGemStress)
            {
                nextGemStress=elapsed+.4f;
                loot.Pool.Drop(zone.RoomOrigin+new Vector3(Mathf.Sin(elapsed)*4,0,Mathf.Cos(elapsed)*4),Vaultbreakers.Gems.LootSource.Chest);
            }
            var state=new GamepadState {leftStick=new Vector2(Mathf.Sin(elapsed*.8f),Mathf.Cos(elapsed*.8f))*.7f,rightStick=new Vector2(-Mathf.Sin(elapsed*.8f),-Mathf.Cos(elapsed*.8f)),rightTrigger=elapsed%4<2?1:0,leftTrigger=elapsed%4>=2?1:0};
            var nearest=Vector3.zero;var distance=float.MaxValue;
            var enemies=zone.Spawner.Roster.Instances;
            for(var enemyIndex=0;enemyIndex<enemies.Count;enemyIndex++)
            {
                var enemy=enemies[enemyIndex];
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
                if(session!=null && zone.State==ZoneState.Fighting)
                {
                    var phase=elapsed%4;
                    switch(session.policy)
                    {
                        case 0: // Melee-first, close distance; emergency ranged pressure at long range.
                            state=new GamepadState{rightStick=state.rightStick,rightTrigger=distance>64?1:0};
                            if(distance<=4)state=state.WithButton(GamepadButton.West);
                            break;
                        case 1: // Ranged pressure with limited kiting inside the combat floor.
                            state=new GamepadState{rightStick=state.rightStick,rightTrigger=1};
                            movement=distance<12?-nearest:distance>36?nearest:Vector3.Cross(Vector3.up,nearest)*.4f;
                            if(distance<2)state=state.WithButton(GamepadButton.West);
                            break;
                        case 2: // Guarded approach, then lower guard to attack.
                            if(phase<2){state=new GamepadState{rightStick=state.rightStick,leftTrigger=1};movement=distance>3?nearest:Vector3.zero;}
                            break;
                        case 3: // Dodge out of close pressure, then re-enter the fight.
                            if(elapsed%1.6f<.1f){state=state.WithButton(GamepadButton.South);if(distance<9)movement=-nearest;}
                            break;
                        case 4: // Alternate all verbs, including simultaneous trigger exclusion.
                            if(phase<1.2f)state=new GamepadState{rightStick=state.rightStick,leftTrigger=1,rightTrigger=phase<.3f?1:0};
                            if(phase>3.5f && phase<3.6f)state=state.WithButton(GamepadButton.South);
                            break;
                    }
                    var fromCentre=zone.Player.transform.position-zone.RoomOrigin;fromCentre.y=0;
                    if(Mathf.Abs(fromCentre.x)>9 || Mathf.Abs(fromCentre.z)>9)movement=-fromCentre.normalized;
                }
                if(zone.State==ZoneState.BetweenWaves || zone.State==ZoneState.Complete)
                {
                    // Walk through the centre of the doorway, just as a player must;
                    // a direct diagonal to the next room can run into a gate post.
                    if(exitRoom!=zone.WaveNumber){exitRoom=zone.WaveNumber;exitCentered=false;}
                    var centre=zone.RoomOrigin;var toCentre=centre-zone.Player.transform.position;toCentre.y=0;
                    if(toCentre.sqrMagnitude<.36f)exitCentered=true;
                    var destination=!exitCentered?centre:zone.State==ZoneState.Complete?Vaultbreakers.Dungeon.DungeonLayout.CorePosition:zone.RoomOrigin+Vector3.forward*Vaultbreakers.Dungeon.DungeonLayout.RoomSpacing;
                    movement=(destination-zone.Player.transform.position);movement.y=0;movement.Normalize();
                    state.rightTrigger=0;
                    if(lootRun && elapsed>=nextNavigationTrace)
                    {
                        nextNavigationTrace=elapsed+5;
                        var actor=zone.Player;
                        var reader=actor.GetComponent<Vaultbreakers.Input.PlayerInputReader>();
                        var motor=actor.GetComponent<Vaultbreakers.Player.PlayerMotor>();
                        var origin=actor.transform.position+Vector3.up;
                        var blocked=Physics.SphereCast(origin,.42f,movement,out var hit,1f,Vaultbreakers.Core.GameLayers.Blocking,QueryTriggerInteraction.Ignore);
                        Debug.Log("Review navigation t="+elapsed+" room="+zone.WaveNumber+" pos="+actor.transform.position+" destination="+destination+" centered="+exitCentered+" input="+reader.Move+" device="+reader.CurrentDevice+" velocity="+motor.PlanarVelocity+" suspended="+motor.IsMovementSuspended+" scale="+Time.timeScale+" blocker="+(blocked?hit.collider.name:"none"));
                    }
                }
                state.leftStick=new Vector2(Vector3.Dot(movement,right),Vector3.Dot(movement,forward))*.85f;
                if(lootRun && loot!=null && zone.State is ZoneState.BetweenWaves or ZoneState.Complete)
                {
                    Vaultbreakers.Gems.BreakableLoot cache=null;var best=float.MaxValue;
                    foreach(var candidate in loot.Containers)
                    {
                        var d=(candidate.transform.position-zone.Player.transform.position).sqrMagnitude;
                        if(candidate.Room==zone.WaveNumber-1 && !candidate.IsBroken && d<best){best=d;cache=candidate;}
                    }
                    var destination=zone.Player.transform.position;var gathering=loot.Pool.TryNearest(destination,out var gemPosition);
                    if(cache!=null || gathering)
                    {
                        exitCentered=false; // Re-centre after the detour before approaching narrow gate posts.
                        // Collect close drops before approaching the next cache; use genuine attack input.
                        var collectFirst=gathering && (gemPosition-zone.Player.transform.position).sqrMagnitude<20;
                        destination=collectFirst || cache==null?gemPosition:cache.transform.position;
                        var toward=destination-zone.Player.transform.position;toward.y=0;var d=toward.sqrMagnitude;toward.Normalize();
                        state=new GamepadState{rightStick=new Vector2(Vector3.Dot(toward,right),Vector3.Dot(toward,forward))};
                        var attacking=cache!=null && !collectFirst;
                        if(attacking && cache.Source==Vaultbreakers.Gems.LootSource.Chest && d<36)state.rightTrigger=1;
                        if(attacking && d<4)state=state.WithButton(GamepadButton.West);
                        var stopDistance=attacking && cache.Source==Vaultbreakers.Gems.LootSource.Chest?12:1.65f;
                        if(!attacking || d>stopDistance)state.leftStick=state.rightStick*.85f;
                        if(elapsed>=nextLootTrace)
                        {
                            nextLootTrace=elapsed+5;
                            Debug.Log("Loot route t="+elapsed+" room="+zone.WaveNumber+" pos="+zone.Player.transform.position+" target="+destination+" cache="+(cache!=null?cache.name:"none")+" hp="+(cache!=null?cache.Health.CurrentHealth:0)+" gather="+collectFirst+" gems="+loot.Pool.ActiveCount+" move="+state.leftStick+" scale="+Time.timeScale);
                        }
                    }
                    if(!capturedLoot && loot.Pool.ActiveCount>=4)
                    {capturedLoot=true;ScreenCapture.CaptureScreenshot(Path.Combine(output,"05-gem-burst.png"));}
                    if(loot.ChestsBroken>lastCapturedChest)
                    {lastCapturedChest=loot.ChestsBroken;StartCoroutine(CaptureCacheLoot("chest-"+lastCapturedChest));}
                    if(!capturedCrateDebris && loot.CratesBroken>0)
                    {capturedCrateDebris=true;StartCoroutine(CaptureCacheLoot("crate-1"));}
                }
            }
            if(session!=null)
            {
                if(session.firstWaveSeconds<0 && zone.WaveNumber==1 && zone.State==ZoneState.BetweenWaves)
                    session.firstWaveSeconds=Time.unscaledTime-firstWaveStarted;
                if(sessionShield.IsRaised && !wasRaised)session.guardRaises++;
                wasRaised=sessionShield.IsRaised;
            }
            InputSystem.QueueStateEvent(pad,state);
            if(elapsed>2){if(samples<frameSamples.Length)frameSamples[samples]=Time.unscaledDeltaTime;samples++;totalFrame+=Time.unscaledDeltaTime;maxFrame=Mathf.Max(maxFrame,Time.unscaledDeltaTime);var bytes=gc.Valid?gc.LastValue:0;totalAllocation+=bytes;maxAllocation=Math.Max(maxAllocation,bytes);}
        }
        private IEnumerator CaptureCacheLoot(string prefix)
        {
            yield return new WaitForSeconds(.18f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,prefix+"-burst.png"));
            yield return new WaitForSeconds(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,prefix+"-collection.png"));
            yield return new WaitForSeconds(.55f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,prefix+"-cleared.png"));
        }
        private void CountMelee(Vector3 direction)=>session.meleeSwings++;
        private void CountShot(Vector3 position,Vector3 direction)
        {
            session.shots++;
            // Melee can revoke guard before Ranged.Update; Shield.Update reconciles its cached
            // state afterwards. Preserve that raw observation separately from the action contract.
            var actions=zone.Player.GetComponent<PlayerActionCoordinator>();
            var shielding=(actions.State & PlayerActionState.Shielding)!=0;
            if(shielding)session.shotsWhileGuardRaised++;
            if(sessionShield.IsRaised)
            {
                session.shotsWhileShieldControllerRaised++;
                Debug.Log("Review shot while shield controller raised: actions="+actions.State+" shieldHeld="+zone.Player.GetComponent<Vaultbreakers.Input.PlayerInputReader>().ShieldHeld+" frame="+Time.frameCount);
            }
        }
        private void CountDodge(Vector3 direction)=>session.dodges++;
        private void CountBlock(DamageInfo damage,float cost)=>session.blocks++;
        private void CountDamage(DamageInfo damage,DamageResult result)=>session.damageTaken+=result.AppliedDamage;
        private void CountDeath(DamageInfo damage)=>session.deaths++;
        private void RestoreCaptureSettings()
        {
            if(!captureConfigured)return;captureConfigured=false;
            Application.runInBackground=oldRunInBackground;InputSystem.settings.backgroundBehavior=oldBackground;
            if(reviewPlayerInput!=null)reviewPlayerInput.neverAutoSwitchControlSchemes=oldNeverAutoSwitch;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
#endif
        }
        private void OnDestroy(){RestoreCaptureSettings();if(gc.Valid)gc.Dispose();if(pad!=null)InputSystem.RemoveDevice(pad);}
#endif
    }
}
