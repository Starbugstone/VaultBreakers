using UnityEngine;
using UnityEngine.UI;
using Vaultbreakers.Enemies;
using Vaultbreakers.Combat;
using Vaultbreakers.UI;
using Vaultbreakers.Zones;
namespace Vaultbreakers.Dungeon
{
    [DefaultExecutionOrder(25)]
    public sealed class DungeonVitals : MonoBehaviour
    {
        private sealed class Entry {public EnemyBrain enemy;public RectTransform root,fill;public Renderer[] renderers;public float flashUntil;public bool flashing;}
        private Entry[] entries;
        private Camera view;
        private MaterialPropertyBlock flash;
        private void Start()
        {
            view=Camera.main;flash=new MaterialPropertyBlock();flash.SetColor("_BaseColor",new Color(2,2,2));
            var roster=GetComponent<ZoneController>().Spawner.Roster;roster.Initialize();entries=new Entry[roster.Instances.Count];
            var canvas=GetComponent<CombatHud>().Canvas;var i=0;
            foreach(var enemy in roster.Instances)
            {
                var root=new GameObject("Enemy vitality",typeof(RectTransform),typeof(Image));root.transform.SetParent(canvas.transform,false);var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(62,6);root.GetComponent<Image>().color=new Color(.04f,.04f,.04f,.9f);
                var child=new GameObject("Fill",typeof(RectTransform),typeof(Image));child.transform.SetParent(root.transform,false);var fill=(RectTransform)child.transform;fill.anchorMin=fill.anchorMax=new Vector2(0,.5f);fill.pivot=new Vector2(0,.5f);fill.sizeDelta=new Vector2(62,6);child.GetComponent<Image>().color=enemy.Definition.role==EnemyRole.Bruiser?new Color(1,.7f,.15f):new Color(1,.30f,.18f);
                root.GetComponent<Image>().raycastTarget=child.GetComponent<Image>().raycastTarget=false;
                entries[i++]=new Entry{enemy=enemy,root=rect,fill=fill,renderers=enemy.GetComponentsInChildren<Renderer>(true)};enemy.HitReceived+=OnHit;
            }
        }
        private void OnHit(EnemyBrain enemy,DamageInfo info,DamageResult result)
        {if(!FeedbackSettings.Flashes)return;foreach(var e in entries)if(e.enemy==enemy){e.flashUntil=Time.time+.065f;e.flashing=true;foreach(var r in e.renderers)if(r!=null && r is not LineRenderer)r.SetPropertyBlock(flash);break;}}
        private void LateUpdate()
        {
            if(entries==null)return;
            foreach(var e in entries)
            {
                var visible=e.enemy.gameObject.activeSelf && !e.enemy.Health.IsDead;
                if(e.root.gameObject.activeSelf!=visible)e.root.gameObject.SetActive(visible);
                if(visible){e.root.position=view.WorldToScreenPoint(e.enemy.transform.position+Vector3.up*(e.enemy.Definition.role==EnemyRole.Bruiser?2.85f:2.25f));e.fill.sizeDelta=new Vector2(62*e.enemy.Health.CurrentHealth/e.enemy.Health.MaximumHealth,6);}
                if(e.flashing && (Time.time>=e.flashUntil || !FeedbackSettings.Flashes)){e.flashing=false;foreach(var r in e.renderers)if(r!=null && r is not LineRenderer)r.SetPropertyBlock(null);}
            }
        }
        private void OnDestroy(){if(entries!=null)foreach(var e in entries)if(e.enemy!=null)e.enemy.HitReceived-=OnHit;}
    }
}
