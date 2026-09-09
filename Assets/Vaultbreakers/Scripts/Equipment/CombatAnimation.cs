using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Player;
using Vaultbreakers.Enemies;

namespace Vaultbreakers.Equipment
{
    // Clips only pose the shared bones. No events, root motion, or animation-driven damage.
    public sealed class CombatAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private Health health;
        private PlayerMotor motor;
        private MeleeController melee;
        private RangedController ranged;
        private ShieldController shield;
        private DodgeController dodge;
        private EnemyBrain enemy;
        private string state, upperState;
        private float reactionUntil;
        private bool blockReaction;
        public void Configure(Animator target) => animator=target;
        private void Awake()
        {
            health=GetComponent<Health>();enemy=GetComponent<EnemyBrain>();
            motor=GetComponent<PlayerMotor>();melee=GetComponent<MeleeController>();ranged=GetComponent<RangedController>();
            shield=GetComponent<ShieldController>();dodge=GetComponent<DodgeController>();
            if(health!=null)health.Damaged+=OnHit;
            if(melee!=null)melee.SwingStarted+=OnSwing;
            if(shield!=null)shield.Blocked+=OnBlock;
        }
        private void OnEnable(){state=null;upperState=null;reactionUntil=0;}
        private void OnHit(DamageInfo damage,DamageResult result){reactionUntil=Time.time+.18f;blockReaction=false;}
        private void OnBlock(DamageInfo damage,float cost){reactionUntil=Time.time+.12f;blockReaction=true;}
        private void OnSwing(Vector3 direction){upperState=null;}
        private void Update()
        {
            if(animator==null || Time.timeScale<=0)return;
            if(health!=null && health.IsDead){Play("Death");return;}
            if(enemy!=null)
            {
                switch(enemy.Attack.State)
                {
                    case EnemyState.Windup:Play(enemy.Definition.role==EnemyRole.Shooter?"Shield":"Melee",.4f);break;
                    case EnemyState.Active:Play(enemy.Definition.role==EnemyRole.Shooter?"Ranged":"Melee",2);break;
                    case EnemyState.Recovery:Play("Idle");break;
                    default:Play("Move",enemy.Definition.speed/3);break;
                }
                return;
            }
            if(dodge!=null && dodge.IsDodging){Play("Dodge",2.3f);return;}
            if(Time.time<reactionUntil && !blockReaction){Play("Hit",2);return;}
            var moving=motor!=null && motor.PlanarVelocity.sqrMagnitude>.1f;
            var next=melee!=null && melee.Phase!=MeleePhase.Ready?"Melee":Time.time<reactionUntil?"ShieldHit":
                shield!=null && shield.IsUnavailable?"ShieldBreak":shield!=null && shield.IsRaised?"Shield":
                ranged!=null && ranged.IsFiring?"Ranged":null;
            Play(moving?"Move":"Idle");
            if(animator.layerCount<2)return;
            animator.SetLayerWeight(1,next==null?0:1);
            if(next!=null && upperState!=next)
            {upperState=next;animator.CrossFadeInFixedTime(next,.04f,1,0);}
            if(next==null)upperState=null;
        }
        private void Play(string next,float speed=1)
        {
            if(animator==null)return;
            if(animator.layerCount>1)animator.SetLayerWeight(1,0);
            animator.speed=speed;if(state==next)return;state=next;animator.CrossFadeInFixedTime(next,.04f,0,0);
        }
        private void OnDestroy()
        {
            if(health!=null)health.Damaged-=OnHit;
            if(melee!=null)melee.SwingStarted-=OnSwing;
            if(shield!=null)shield.Blocked-=OnBlock;
        }
    }
}
