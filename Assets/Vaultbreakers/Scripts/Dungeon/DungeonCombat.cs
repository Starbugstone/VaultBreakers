using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;
using Vaultbreakers.Input;
using Vaultbreakers.Player;
using Vaultbreakers.Zones;
using Vaultbreakers.UI;
namespace Vaultbreakers.Dungeon
{
    [DefaultExecutionOrder(-5)]
    public sealed class DungeonCombat : MonoBehaviour
    {
        private ZoneController zone;
        private PlayerInputReader kitInput;
        private MeleeController melee;
        private CharacterController motor;
        private float lunge;
        private Vector3 lungeDirection;
        private LineRenderer feet;
        private Material fx;
        [SerializeField] private Material template;
        public void Configure(Material material)=>template=material;
        private void Start()
        {
            zone=Object.FindAnyObjectByType<ZoneController>();kitInput=GetComponent<PlayerInputReader>();
            melee=GetComponent<MeleeController>();motor=GetComponent<CharacterController>();
            fx=new Material(template);feet=Line("Hero marker",new Color(.5f,.9f,1),.025f,33);
            melee.SwingStarted+=OnSwing;zone.Player.ResetPerformed+=ResetCombat;
        }
        private LineRenderer Line(string name,Color color,float width,int count)
        {
            var o=new GameObject(name);o.transform.SetParent(transform);var line=o.AddComponent<LineRenderer>();line.sharedMaterial=fx;
            line.startColor=line.endColor=color;line.startWidth=line.endWidth=width;line.positionCount=count;line.useWorldSpace=true;line.enabled=false;line.numCapVertices=3;return line;
        }
        private void Update()
        {
            if(zone==null || zone.IsPaused || zone.Player.IsDead || Time.timeScale<=0)return;
            if(kitInput.MeleeHeld)melee.BufferSwing();
            if(melee.Phase==MeleePhase.Ready)lunge=0;
            if(lunge>0 && motor.enabled){motor.Move(lungeDirection*(3.2f*Time.deltaTime));lunge-=Time.deltaTime;}
            var pos=transform.position;
            feet.enabled=true;Circle(feet,pos+Vector3.up*.045f,.42f);
        }
        private static void Circle(LineRenderer line,Vector3 pos,float radius)
        {for(var i=0;i<line.positionCount;i++){var a=i*Mathf.PI*2/(line.positionCount-1);line.SetPosition(i,pos+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius);}}
        private void OnSwing(Vector3 direction)
        {
            lungeDirection=direction;lunge=.10f;
        }
        private void ResetCombat(){lunge=0;}
        private void OnDestroy(){if(melee!=null)melee.SwingStarted-=OnSwing;if(zone!=null && zone.Player!=null)zone.Player.ResetPerformed-=ResetCombat;if(fx!=null)Destroy(fx);}
    }
}
