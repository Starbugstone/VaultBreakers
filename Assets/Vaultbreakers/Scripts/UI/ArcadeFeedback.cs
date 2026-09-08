using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Vaultbreakers.Combat;
using Vaultbreakers.Enemies;
using Vaultbreakers.Zones;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Vaultbreakers.UI
{
    [DefaultExecutionOrder(20)]
    public sealed class ArcadeFeedback : MonoBehaviour
    {
        private struct Impact { public ParticleSystem particles; public Light light; public float until; }
        private struct Number { public Text text; public Vector3 origin; public float until; }
        private readonly Impact[] impacts = new Impact[8];
        private readonly Number[] numbers = new Number[24];
        [SerializeField] private Material sparkTemplate;
        public void Configure(Material material) => sparkTemplate=material;
        private ZoneController zone;
        private Camera cameraView;
        private Vector3 cameraRest;
        private Vaultbreakers.Dungeon.DungeonCamera follow;
        private Material sparkMaterial;
        private int nextImpact, nextNumber;
        private float shake, rumbleUntil;
        private bool rumbling;
        private Gamepad rumblePad;
        private Health player;
        private Volume volume;
        private Bloom bloom;
        private ColorAdjustments grading;
        private void Start()
        {
            zone=GetComponent<ZoneController>();player=zone.Player;cameraView=Camera.main;cameraRest=cameraView.transform.position;follow=cameraView.GetComponent<Vaultbreakers.Dungeon.DungeonCamera>();
            sparkMaterial=new Material(sparkTemplate);
            sparkMaterial.SetColor("_BaseColor",Color.white);
            for(var i=0;i<impacts.Length;i++)
            {
                var go=new GameObject("Impact "+i);go.transform.SetParent(transform);
                var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=.4f;main.startLifetime=new ParticleSystem.MinMaxCurve(.1f,.3f);
                main.startSpeed=new ParticleSystem.MinMaxCurve(2,5);main.startSize=new ParticleSystem.MinMaxCurve(.04f,.12f);main.maxParticles=48;main.gravityModifier=.3f;
                main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=ps.emission;emission.enabled=false;
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.05f;
                go.GetComponent<ParticleSystemRenderer>().sharedMaterial=sparkMaterial;
                var light=go.AddComponent<Light>();light.range=3;light.intensity=0;light.shadows=LightShadows.None;
                impacts[i]=new Impact{particles=ps,light=light};
            }
            var hud=GetComponent<CombatHud>();
            for(var i=0;i<numbers.Length;i++)
            {
                var go=new GameObject("Damage "+i,typeof(RectTransform),typeof(Text));go.transform.SetParent(hud.Canvas.transform,false);
                var text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontStyle=FontStyle.Bold;text.fontSize=23;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
                text.rectTransform.sizeDelta=new Vector2(100,40);text.enabled=false;numbers[i].text=text;
            }
            zone.Spawner.Roster.Initialize();
            foreach(var enemy in zone.Spawner.Roster.Instances)enemy.HitReceived+=OnEnemyDamage;
            player.Damaged+=OnPlayerDamage;player.GetComponent<ShieldController>().Blocked+=OnBlock;
            player.GetComponent<DodgeController>().DodgeStarted+=OnDodge;
            volume=Object.FindAnyObjectByType<Volume>();
            if(volume!=null){volume.profile.TryGet(out bloom);volume.profile.TryGet(out grading);}
        }
        private void OnEnemyDamage(EnemyBrain enemy,DamageInfo damage,DamageResult result)
        { Burst(enemy.transform.position+Vector3.up, new Color(1,.55f,.16f),result.AppliedDamage); }
        private void OnPlayerDamage(DamageInfo damage,DamageResult result)=>Burst(player.transform.position+Vector3.up,new Color(1,.15f,.3f),result.AppliedDamage);
        private void OnBlock(DamageInfo damage,float cost)=>Burst(player.transform.position+Vector3.up,new Color(.1f,.85f,1),0);
        private void OnDodge(Vector3 direction)=>Burst(player.transform.position+Vector3.up*.15f,new Color(.1f,.85f,1),0);
        public void Burst(Vector3 position,Color color,float amount)
        {
            var i=nextImpact++%impacts.Length;var impact=impacts[i];impact.particles.transform.position=position;
            var emit=new ParticleSystem.EmitParams{startColor=color};impact.particles.Emit(emit,FeedbackSettings.Flashes?24:7);
            impact.light.color=color;impact.light.intensity=FeedbackSettings.Flashes?3:0;impact.until=Time.time+.14f;impacts[i]=impact;
            if(FeedbackSettings.Shake)shake=.13f;
            if(FeedbackSettings.Rumble && Gamepad.current!=null){rumblePad=Gamepad.current;rumblePad.SetMotorSpeeds(.12f,.24f);rumbling=true;rumbleUntil=Time.unscaledTime+.07f;}
            if(amount>0 && FeedbackSettings.Numbers)
            {
                i=nextNumber++%numbers.Length;var number=numbers[i];number.origin=position+Vector3.up*.4f;number.until=Time.time+.5f;
                number.text.text=Mathf.CeilToInt(amount).ToString();number.text.color=color;number.text.enabled=true;numbers[i]=number;
            }
        }
        private void LateUpdate()
        {
            if(zone==null)return;
            if(bloom!=null)bloom.active=FeedbackSettings.Bloom;
            if(grading!=null)grading.saturation.value=FeedbackSettings.Grayscale?-100:0;
            for(var i=0;i<impacts.Length;i++)if(impacts[i].light!=null && (Time.time>=impacts[i].until || !FeedbackSettings.Flashes))impacts[i].light.intensity=0;
            for(var i=0;i<numbers.Length;i++)
            {
                if(numbers[i].text==null)continue;
                var alive=Time.time<numbers[i].until && FeedbackSettings.Numbers;numbers[i].text.enabled=alive;
                if(alive)numbers[i].text.transform.position=cameraView.WorldToScreenPoint(numbers[i].origin+Vector3.up*(.5f-(numbers[i].until-Time.time)));
            }
            shake=Mathf.Max(0,shake-Time.unscaledDeltaTime);
            cameraView.transform.position=(follow!=null?follow.RestPosition:cameraRest)+(FeedbackSettings.Shake && !zone.IsPaused?new Vector3(Mathf.Sin(Time.unscaledTime*83),Mathf.Cos(Time.unscaledTime*97),0)*shake*.3f:Vector3.zero);
            if(rumbling && (Time.unscaledTime>=rumbleUntil || zone.IsPaused || !FeedbackSettings.Rumble)){if(rumblePad!=null && rumblePad.added)rumblePad.SetMotorSpeeds(0,0);rumbling=false;}
            if(zone.State is ZoneState.Retry or ZoneState.Preparing)
            {
                foreach(var impact in impacts)if(impact.particles!=null)impact.particles.Clear();
                for(var i=0;i<numbers.Length;i++)numbers[i].until=0;
            }
        }
        private void OnDestroy()
        {
            if(zone!=null && zone.Spawner!=null && zone.Spawner.Roster!=null)
                foreach(var enemy in zone.Spawner.Roster.Instances)if(enemy!=null && enemy.Health!=null)enemy.HitReceived-=OnEnemyDamage;
            if(player!=null){player.Damaged-=OnPlayerDamage;player.GetComponent<ShieldController>().Blocked-=OnBlock;player.GetComponent<DodgeController>().DodgeStarted-=OnDodge;}
            if(sparkMaterial!=null)Destroy(sparkMaterial);
            if(Gamepad.current!=null)Gamepad.current.SetMotorSpeeds(0,0);
        }
    }
}
