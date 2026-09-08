using UnityEngine;
using Vaultbreakers.Core;

namespace Vaultbreakers.Combat
{
    public enum ProjectileStep
    {
        Travelling = 0,
        Impacted = 1,
        Expired = 2
    }

    /// <summary>
    /// A pooled player projectile. It carries no <see cref="Rigidbody"/> and no collider: each step
    /// sweeps a sphere from where it was to where it is going, which is what stops it from tunnelling
    /// through a target at speed and guarantees it resolves exactly one hit. The pool owns its
    /// lifetime and drives <see cref="Tick"/>, so a hundred projectiles cost one Update, not a hundred.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float radius = 0.12f;

        private Vector3 direction = Vector3.forward;
        private float speed;
        private float damage;
        private float remainingLife;
        private GameObject owner;
        private int hitMask = GameLayers.PlayerProjectileHits;

        public float Radius => radius;
        public Vector3 Direction => direction;
        public Vector3 LastImpactPoint { get; private set; }
        public Vector3 LastImpactNormal { get; private set; } = Vector3.up;

        /// <summary>The receiver damaged by the last impact, or null when it hit geometry.</summary>
        public Health LastImpactTarget { get; private set; }

        public void Configure(float sweepRadius, int collisionMask = 0)
        { radius = Mathf.Max(0.01f, sweepRadius); hitMask = collisionMask == 0 ? GameLayers.PlayerProjectileHits : collisionMask; }

        public void Launch(
            Vector3 origin,
            Vector3 travelDirection,
            float travelSpeed,
            float impactDamage,
            float lifetime,
            GameObject firedBy)
        {
            travelDirection.y = 0f;
            direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector3.forward;
            speed = Mathf.Max(0f, travelSpeed);
            damage = Mathf.Max(0f, impactDamage);
            remainingLife = Mathf.Max(0f, lifetime);
            owner = firedBy;
            LastImpactTarget = null;

            transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction, Vector3.up));
        }

        /// <summary>
        /// Advances one step and reports what happened. Driven by the pool rather than by Unity so the
        /// travel and impact rules can be stepped at an exact delta by the tests.
        /// </summary>
        public ProjectileStep Tick(float deltaTime)
        {
            remainingLife -= deltaTime;

            var distance = speed * deltaTime;
            var origin = transform.position;

            if (distance > 0f && Physics.SphereCast(
                    origin,
                    radius,
                    direction,
                    out var hit,
                    distance,
                    hitMask,
                    QueryTriggerInteraction.Collide))
            {
                Resolve(hit, origin);
                return ProjectileStep.Impacted;
            }

            transform.position = origin + direction * distance;
            return remainingLife <= 0f ? ProjectileStep.Expired : ProjectileStep.Travelling;
        }

        /// <summary>
        /// A sweep that starts already overlapping a collider reports zero distance and a meaningless
        /// point and normal, which happens whenever the muzzle is buried in something. Fall back to
        /// the projectile's own position and incoming direction so the impact still reads correctly.
        /// </summary>
        private void Resolve(RaycastHit hit, Vector3 origin)
        {
            LastImpactPoint = hit.distance > 0f ? hit.point : origin;
            LastImpactNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal : -direction;
            transform.position = origin + direction * hit.distance;

            var target = hit.collider != null ? hit.collider.GetComponentInParent<Health>() : null;
            if (target == null || target.IsDead)
            {
                LastImpactTarget = null;
                return;
            }

            LastImpactTarget = target;
            target.ReceiveDamage(new DamageInfo(damage, LastImpactPoint, owner));
        }
    }
}
