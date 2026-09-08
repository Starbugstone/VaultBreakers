using System;
using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Core;

namespace Vaultbreakers.Enemies
{
    [RequireComponent(typeof(Health), typeof(CharacterController))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        private Health health;
        private Health target;
        private CharacterController motor;
        private ProjectilePool projectiles;
        private Vector3 committedFacing;
        private Vector3 impulse;
        private readonly Collider[] neighbours = new Collider[24];
        private bool lifeReported;
        private static int nextIdentity;
        public int Identity { get; private set; }
        private float arrivalDelay;
        public EnemyAttack Attack { get; } = new();
        public EnemyDefinition Definition => definition;
        public Health Health => health;
        public Vector3 AttackFacing => committedFacing;
        public event Action<EnemyBrain> Removed;
        public event Action<EnemyBrain, DamageInfo, DamageResult> HitReceived;
        private void Awake()
        {
            Identity = ++nextIdentity;
            health = GetComponent<Health>(); motor = GetComponent<CharacterController>();
            health.Damaged += OnDamaged; health.Died += OnDied;
        }
        public void Configure(EnemyDefinition data) => definition = data;
        public void Spawn(Health player, ProjectilePool pool, Vector3 position)
        {
            target = player; projectiles = pool; lifeReported = false; impulse = Vector3.zero;
            motor.enabled = false; transform.position = position; motor.enabled = true;
            health.Configure(definition.health); Attack.Reset(); arrivalDelay = 0.65f;
            gameObject.SetActive(true);
        }
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float delta)
        {
            if (delta <= 0 || definition == null || health.IsDead || target == null || target.IsDead) return;
            if (arrivalDelay > 0) { arrivalDelay -= delta; return; }
            if (impulse.sqrMagnitude > 0.001f)
            {
                motor.Move(impulse * delta); impulse = Vector3.MoveTowards(impulse, Vector3.zero, 18 * delta);
            }
            if (Attack.State != EnemyState.Approach)
            {
                if (Attack.Tick(delta, definition.active, definition.recovery)) Strike();
                return;
            }
            var offset = target.transform.position - transform.position; offset.y = 0;
            var distance = offset.magnitude;
            var direction = distance > 0.001f ? offset / distance : transform.forward;
            transform.rotation = Quaternion.LookRotation(direction);
            var shooter = definition.role == EnemyRole.Shooter;
            var desired = shooter ? definition.preferredRange : definition.range * 0.85f;
            var sightClear = !shooter || !Physics.Linecast(transform.position+Vector3.up,target.transform.position+Vector3.up,GameLayers.Blocking,QueryTriggerInteraction.Ignore);
            var movement = !sightClear ? direction : distance > desired ? direction : shooter && distance < desired - 1 ? -direction : Vector3.zero;
            var count = Physics.OverlapSphereNonAlloc(transform.position, 1.3f, neighbours, GameLayers.EnemyTargets);
            var separation = Vector3.zero;
            for (var i = 0; i < count; i++)
            {
                if (neighbours[i] == motor) continue;
                var away = transform.position - neighbours[i].transform.position; away.y = 0;
                if (away.sqrMagnitude > 0.001f) separation += away.normalized / Mathf.Max(0.3f, away.magnitude);
            }
            var steering=Vector3.ClampMagnitude(movement+separation*.6f,1);
            if(steering.sqrMagnitude>.001f && Physics.SphereCast(transform.position+Vector3.up*.8f,motor.radius*.85f,steering.normalized,out var obstacle,.8f,GameLayers.Blocking,QueryTriggerInteraction.Ignore))
            {
                var tangent=Vector3.ProjectOnPlane(steering,obstacle.normal);tangent.y=0;
                if(tangent.sqrMagnitude<.05f)tangent=Vector3.Cross(Vector3.up,obstacle.normal)*(Identity%2==0?1:-1);
                steering=tangent.normalized;
            }
            motor.Move((steering * definition.speed + Vector3.down * 2) * delta);
            if (sightClear && distance <= (shooter ? definition.preferredRange + 1 : definition.range))
            {
                committedFacing = direction; Attack.Begin(definition.windup);
            }
        }
        private void Strike()
        {
            if (definition.role == EnemyRole.Shooter)
            {
                if (projectiles != null) projectiles.Fire(transform.position + Vector3.up + committedFacing * 0.7f,
                    committedFacing, definition.projectileSpeed, definition.damage, 3, gameObject);
            }
            else if (EnemyAttack.Contains(transform.position, committedFacing, target.transform.position, definition.range, definition.arc))
            {
                target.ReceiveDamage(new DamageInfo(definition.damage, transform.position, gameObject,
                    committedFacing * 2, definition.stabilityDamage));
            }
        }
        private void OnDamaged(DamageInfo damage, DamageResult result) { impulse = damage.Knockback; HitReceived?.Invoke(this, damage, result); }
        private void OnDied(DamageInfo damage) { Attack.Reset(true); motor.enabled = false; ReportRemoved(); }
        private void ReportRemoved() { if (lifeReported) return; lifeReported = true; Removed?.Invoke(this); }
        private void OnDisable() { ReportRemoved(); }
        private void OnDestroy() { ReportRemoved(); health.Damaged -= OnDamaged; health.Died -= OnDied; }
        public void Despawn() { Attack.Reset(true); gameObject.SetActive(false); }
    }
}
