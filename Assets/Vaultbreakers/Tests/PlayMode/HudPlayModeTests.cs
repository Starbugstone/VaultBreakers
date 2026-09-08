using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Vaultbreakers.UI;
using Vaultbreakers.Zones;
using Vaultbreakers.Combat;

namespace Vaultbreakers.Tests.PlayMode
{
    public sealed class HudPlayModeTests
    {
        private Gamepad pad;
        [UnityTearDown] public IEnumerator Cleanup(){if(pad!=null && pad.added)InputSystem.RemoveDevice(pad);Time.timeScale=1;yield return IsolatedTestBed.Load();}
        [UnityTest] public IEnumerator VirtualControllerDisconnectPausesAndClearsItsHeldActions()
        {
            pad=InputSystem.AddDevice<Gamepad>();
            yield return SceneManager.LoadSceneAsync("Prototype_Arena");yield return null;yield return null;
            var zone=Object.FindAnyObjectByType<ZoneController>();zone.EnterTraining();
            zone.Player.GetComponent<PlayerInput>().SwitchCurrentControlScheme("Gamepad",pad);
            InputSystem.QueueStateEvent(pad,new GamepadState{rightTrigger=1,leftStick=Vector2.up});
            yield return null;yield return null;
            Assert.That(zone.Player.GetComponent<RangedController>().IsFiring,Is.True);
            InputSystem.RemoveDevice(pad);yield return null;
            Assert.That(zone.IsPaused,Is.True);Assert.That(Object.FindAnyObjectByType<CombatHud>().DeviceLost,Is.True);
            Assert.That(zone.Player.GetComponent<Vaultbreakers.Input.PlayerInputReader>().RangedHeld,Is.False);
            zone.SetPaused(false);yield return null;
            Assert.That(zone.Player.GetComponent<RangedController>().IsFiring,Is.False);
            pad=null;
        }
        [UnityTest] public IEnumerator MouseAimRotatesAnAlreadyRaisedShield()
        {
            var oldBackground=InputSystem.settings.backgroundBehavior;var oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                yield return SceneManager.LoadSceneAsync("Prototype_Arena");yield return null;yield return null;
                var zone=Object.FindAnyObjectByType<ZoneController>();zone.EnterTraining();
                zone.Player.GetComponent<PlayerInput>().SwitchCurrentControlScheme("Keyboard&Mouse",keyboard,mouse);
                zone.Player.GetComponent<Vaultbreakers.Player.PlayerFacing>().EnableMouseAim();
                var screen=Camera.main.WorldToScreenPoint(zone.Player.transform.position+Vector3.right*3);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(screen.x,screen.y),buttons=2});
                yield return null;yield return null;
                var shield=zone.Player.GetComponent<ShieldController>();Assert.That(shield.IsRaised,Is.True);
                Assert.That(Vector3.Dot(shield.ShieldFacing,Vector3.right),Is.GreaterThan(.9f));
                screen=Camera.main.WorldToScreenPoint(zone.Player.transform.position+Vector3.left*3);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(screen.x,screen.y),buttons=2});
                yield return null;yield return null;
                Assert.That(Vector3.Dot(shield.ShieldFacing,Vector3.left),Is.GreaterThan(.9f));
            }
            finally{InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;}
        }
        [UnityTest] public IEnumerator PresentationPreferencesDoNotChangeIncomingDamageOrWaveMembership()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Arena");yield return null;yield return null;
            var zone=Object.FindAnyObjectByType<ZoneController>();zone.EnterTraining();
            FeedbackSettings.Flashes=false;FeedbackSettings.Numbers=false;FeedbackSettings.Shake=false;
            var count=zone.Spawner.Progress.LivingCount;var hp=zone.Player.CurrentHealth;
            zone.Player.ReceiveDamage(new DamageInfo(7));
            Assert.That(zone.Player.CurrentHealth,Is.EqualTo(hp-7));Assert.That(zone.Spawner.Progress.LivingCount,Is.EqualTo(count));
            FeedbackSettings.Flashes=true;FeedbackSettings.Numbers=true;FeedbackSettings.Shake=true;
        }
    }
}
