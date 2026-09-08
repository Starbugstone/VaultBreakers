using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Vaultbreakers.Combat;
using Vaultbreakers.Zones;

namespace Vaultbreakers.UI
{
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(ZoneController))]
    public sealed class CombatHud : MonoBehaviour
    {
        private ZoneController zone;
        private ShieldController shield;
        private DodgeController dodge;
        private Canvas canvas;
        private Font font;
        private Image healthFill, shieldFill;
        private Text healthText, shieldText, waveText, enemiesText, banner, dodgeText;
        private Vaultbreakers.Dungeon.DungeonJourney journey;
        private Text objective, score;
        private int lastScore=-1;
        private string lastObjective;
        private readonly System.Collections.Generic.List<System.Action> refreshOptions=new();
        private GameObject pausePanel;
        private Button resume;
        private bool wasPaused;
        private bool deviceLost;
        private Gamepad usedPad;
        private int lastHealth = -1, lastStability = -1, lastWave = -1, lastCount = -1;
        private ShieldState lastShield = (ShieldState)(-1);
        private ZoneState lastState = (ZoneState)(-1);
        private readonly Color cyan = new(.35f, .9f, 1);
        private readonly Color ink = new(.012f, .035f, .045f, .9f);
        public Canvas Canvas => canvas;
        public bool DeviceLost => deviceLost;
        private void Start()
        {
            zone = GetComponent<ZoneController>();journey=GetComponent<Vaultbreakers.Dungeon.DungeonJourney>(); shield = zone.Player.GetComponent<ShieldController>(); dodge = zone.Player.GetComponent<DodgeController>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var host = new GameObject("Combat HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            host.transform.SetParent(transform, false); canvas = host.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = host.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            if (EventSystem.current == null)
            {
                var events = new GameObject("UI Events", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var top = Panel("Top rail", canvas.transform, new Vector2(0,1), new Vector2(0,1), new Vector2(32,-26), new Vector2(440,120), ink);
            Label("Brand",top,"VAULTBREAKERS",28,new Vector2(22,-10),new Vector2(360,42),FontStyle.Bold);
            Label("Subtitle",top,"DOCK 9 DISASTER  /  THE SHATTERBELT",14,new Vector2(24,-54),new Vector2(420,24),FontStyle.Normal,cyan);
            waveText=Label("Wave",top,"",16,new Vector2(24,-84),new Vector2(410,30),FontStyle.Bold);
            var right=Panel("Run status",canvas.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(-32,-26),new Vector2(320,120),ink);
            enemiesText=Label("Enemies",right,"",20,new Vector2(22,-18),new Vector2(280,32),FontStyle.Bold);
            score=Label("Score",right,"",18,new Vector2(22,-63),new Vector2(280,32),FontStyle.Bold,new Color(1,.8f,.35f));
            objective=Label("Objective",canvas.transform,"",17,new Vector2(0,-155),new Vector2(1000,30),FontStyle.Bold,new Color(1,.90f,.65f));
            objective.rectTransform.anchorMin=objective.rectTransform.anchorMax=new Vector2(.5f,1);objective.rectTransform.pivot=new Vector2(.5f,1);objective.alignment=TextAnchor.MiddleCenter;
            var vitality=Panel("Vitality",canvas.transform,new Vector2(0,0),new Vector2(0,0),new Vector2(32,34),new Vector2(370,152),ink);
            healthText=Label("Health",vitality,"",20,new Vector2(20,-12),new Vector2(330,30),FontStyle.Bold);
            healthFill=Bar(vitality,"Health bar",new Vector2(20,-48),new Color(1,.3f,.22f));
            shieldText=Label("Shield",vitality,"",17,new Vector2(20,-76),new Vector2(340,28),FontStyle.Bold);
            shieldFill=Bar(vitality,"Stability bar",new Vector2(20,-112),cyan);
            var abilities=Panel("Actions",canvas.transform,new Vector2(1,0),new Vector2(1,0),new Vector2(-32,34),new Vector2(710,92),ink);
            var verbs=new[]{"MELEE","FIRE","SHIELD","DODGE"};var keys=new[]{"X / LMB","RT / E","LT / RMB","A / SPACE"};
            for(var i=0;i<4;i++)
            {
                Label("Key"+i,abilities,keys[i],16,new Vector2(24+i*175,-13),new Vector2(165,26),FontStyle.Bold,cyan);
                var label=Label("Action"+i,abilities,verbs[i],20,new Vector2(24+i*175,-43),new Vector2(165,34),FontStyle.Bold);
                if(i==3)dodgeText=label;
            }
            banner=Label("Wave banner",canvas.transform,"",30,new Vector2(0,-198),new Vector2(1000,60),FontStyle.Bold);
            banner.rectTransform.anchorMin=banner.rectTransform.anchorMax=new Vector2(.5f,1);banner.rectTransform.pivot=new Vector2(.5f,1);banner.alignment=TextAnchor.MiddleCenter;
            Label("Movement help",canvas.transform,"MOVE  WASD / LS     AIM  MOUSE / RS",15,new Vector2(32,-156),new Vector2(490,26),FontStyle.Normal,cyan);
            CreatePause(); InputSystem.onDeviceChange+=OnDeviceChange;
        }
        private Image Bar(Transform parent,string name,Vector2 pos,Color color)
        {
            var rail=Panel(name,parent,new Vector2(0,1),new Vector2(0,1),pos,new Vector2(328,18),new Color(.14f,.18f,.25f));
            var fill=Panel("Fill",rail,new Vector2(0,0),new Vector2(0,0),Vector2.zero,new Vector2(328,18),color).GetComponent<Image>();
            return fill;
        }
        private void Update()
        {
            if(zone==null || canvas==null)return;
            if(Gamepad.current!=null && (Gamepad.current.leftStick.ReadValue().sqrMagnitude>.05f || Gamepad.current.buttonSouth.isPressed || Gamepad.current.rightTrigger.isPressed))usedPad=Gamepad.current;
            var hp=Mathf.CeilToInt(zone.Player.CurrentHealth);var stability=Mathf.CeilToInt(shield.Stability);
            if(hp!=lastHealth){lastHealth=hp;healthText.text="HEALTH   "+hp+" / "+Mathf.CeilToInt(zone.Player.MaximumHealth);healthFill.rectTransform.sizeDelta=new Vector2(328*hp/zone.Player.MaximumHealth,18);}
            if(stability!=lastStability || shield.State!=lastShield)
            {
                lastStability=stability;lastShield=shield.State;
                shieldText.text=shield.IsUnavailable ? "GUARD BROKEN  /  RECHARGING" : (stability<34?"LOW GUARD   ":"GUARD   ")+stability+" / 100";
                shieldFill.rectTransform.sizeDelta=new Vector2(328*shield.StabilityFraction,18);
                shieldFill.color=shield.IsUnavailable?new Color(1,.4f,.2f):cyan;
            }
            if(journey!=null)
            {
                if(lastScore!=journey.Score){lastScore=journey.Score;score.text="SALVAGE SCORE   "+lastScore.ToString("00000");}
                if(lastObjective!=journey.Objective){lastObjective=journey.Objective;objective.text=lastObjective;if(journey.TreasureClaimed)banner.text="VAULT CORE RECOVERED";}
            }
            var count=zone.Spawner.Progress.LivingCount;
            if(lastWave!=zone.WaveNumber){lastWave=zone.WaveNumber;waveText.text=(journey!=null?"ZONE  0":"WAVE  0")+lastWave+" / 0"+zone.WaveCount+"    "+zone.WaveTitle;}
            if(lastCount!=count){lastCount=count;enemiesText.text=count+" HOSTILES REMAIN";}
            var verb=dodge.CooldownRemaining>.01f?"RECHARGING":"DODGE";
            if(dodgeText.text!=verb)dodgeText.text=verb;
            if(lastState!=zone.State)
            {
                lastState=zone.State;
                banner.text=zone.State switch {
                    ZoneState.Preparing=>"GET READY",ZoneState.BetweenWaves=>"ZONE SECURED",
                    ZoneState.Retry=>"RIG DOWN  /  CHECKPOINT RESTART",ZoneState.Complete=>journey!=null?"RECOVERY AUTHORIZED":"VAULT SECURED   •   R / START TO REPLAY",
                    ZoneState.Training=>"TRAINING RANGE",_=>""};
            }
            if(zone.State==ZoneState.Complete && Gamepad.current!=null && Gamepad.current.startButton.wasPressedThisFrame){zone.RestartRun();}
            if(wasPaused!=zone.IsPaused)
            {
                wasPaused=zone.IsPaused;pausePanel.SetActive(wasPaused);
                if(wasPaused){foreach(var refresh in refreshOptions)refresh();EventSystem.current.SetSelectedGameObject(resume.gameObject);}
            }
        }
        private void OnDeviceChange(InputDevice device,InputDeviceChange change)
        {
            if(zone==null)return;
            if(device==usedPad && change is InputDeviceChange.Disconnected or InputDeviceChange.Removed or InputDeviceChange.Disabled)
            { deviceLost=true;zone.SetPaused(true);banner.text="CONTROLLER DISCONNECTED  /  RECONNECT OR USE KEYBOARD"; }
            if(device is Gamepad && change is InputDeviceChange.Reconnected or InputDeviceChange.Added)
            { deviceLost=false;lastState=(ZoneState)(-1); }
        }
        private void CreatePause()
        {
            var shade=Panel("Pause shade",canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1920,1080),new Color(.006f,.012f,.035f,.85f));
            pausePanel=shade.gameObject;
            var card=Panel("Pause card",shade,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(600,700),ink);
            Label("Pause title",card,"TAKE A BREATHER",34,new Vector2(36,-30),new Vector2(540,50),FontStyle.Bold);
            resume=Button(card,"RESUME",-112,()=>{deviceLost=false;lastState=(ZoneState)(-1);zone.SetPaused(false);});
            Button(card,"RETRY CHECKPOINT",-170,()=>zone.RestartWave());
            Button(card,"NEW RUN",-228,()=>zone.RestartRun());
            Toggle(card,"SCREEN SHAKE",-302,()=>FeedbackSettings.Shake,v=>FeedbackSettings.Shake=v);
            Toggle(card,"IMPACT FLASHES",-352,()=>FeedbackSettings.Flashes,v=>FeedbackSettings.Flashes=v);
            Toggle(card,"CONTROLLER RUMBLE",-402,()=>FeedbackSettings.Rumble,v=>FeedbackSettings.Rumble=v);
            Toggle(card,"DAMAGE NUMBERS",-452,()=>FeedbackSettings.Numbers,v=>FeedbackSettings.Numbers=v);
            Toggle(card,"BLOOM",-502,()=>FeedbackSettings.Bloom,v=>FeedbackSettings.Bloom=v);
            Toggle(card,"GRAYSCALE",-552,()=>FeedbackSettings.Grayscale,v=>FeedbackSettings.Grayscale=v);
            Toggle(card,"HIT STOP",-602,()=>FeedbackSettings.HitStopScale>0,v=>FeedbackSettings.HitStopScale=v?1:0);
            pausePanel.SetActive(false);
        }
        private Button Button(Transform parent,string caption,float y,UnityEngine.Events.UnityAction action)
        {
            var panel=Panel(caption,parent,new Vector2(0,1),new Vector2(0,1),new Vector2(36,y),new Vector2(528,48),new Color(.055f,.15f,.22f));
            var button=panel.gameObject.AddComponent<Button>();button.targetGraphic=panel.GetComponent<Image>();button.onClick.AddListener(action);
            var colors=button.colors;colors.highlightedColor=new Color(.2f,.9f,1);colors.selectedColor=colors.highlightedColor;button.colors=colors;
            Label("Caption",panel,caption,19,new Vector2(15,-7),new Vector2(490,34),FontStyle.Bold);return button;
        }
        private void Toggle(Transform parent,string caption,float y,System.Func<bool> get,System.Action<bool> set)
        {
            Button button=null;
            button=Button(parent,caption+"   "+(get()?"ON":"OFF"),y,()=>{set(!get());button.GetComponentInChildren<Text>().text=caption+"   "+(get()?"ON":"OFF");});
            var label=button.GetComponentInChildren<Text>();
            refreshOptions.Add(()=>label.text=caption+"   "+(get()?"ON":"OFF"));
        }
        private RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;
            go.GetComponent<Image>().color=color;return rect;
        }
        private Text Label(string name,Transform parent,string content,int size,Vector2 position,Vector2 dimensions,FontStyle style=FontStyle.Normal,Color? color=null)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var text=go.GetComponent<Text>();text.font=font;text.fontSize=size;text.fontStyle=style;text.color=color??Color.white;text.text=content;text.raycastTarget=false;
            var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=dimensions;
            return text;
        }
        private void OnDestroy(){InputSystem.onDeviceChange-=OnDeviceChange;}
    }
}
