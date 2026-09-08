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
        private string state;
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
        private void OnEnable(){state=null;reactionUntil=0;}
        private void OnHit(DamageInfo damage,DamageResult result){reactionUntil=Time.time+.18f;blockReaction=false;}
        private void OnBlock(DamageInfo damage,float cost){reactionUntil=Time.time+.12f;blockReaction=true;}
        private void OnSwing(Vector3 direction){state=null;Play("Melee",2.35f);}
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
            if(melee!=null && melee.Phase!=MeleePhase.Ready){Play("Melee",2.35f);return;}
            if(Time.time<reactionUntil){Play(blockReaction?"ShieldHit":"Hit",2);return;}
            if(shield!=null && shield.IsUnavailable){Play("ShieldBreak");return;}
            if(shield!=null && shield.IsRaised){Play("Shield");return;}
            if(ranged!=null && ranged.IsFiring){Play("Ranged",2);return;}
            Play(motor!=null && motor.PlanarVelocity.sqrMagnitude>.1f?"Move":"Idle");
        }
        private void Play(string next,float speed=1)
        {
            if(animator==null)return;
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
