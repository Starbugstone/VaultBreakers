using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vaultbreakers.Combat
{
    /// <summary>
    /// Fixed-size projectile pool. Every instance is allocated once, up front, and reused for the rest
    /// of the session: sustained fire must never allocate, which is one of the Phase 6 exit criteria.
    ///
    /// Instances live under their own scene object rather than under the shooter, because a projectile
    /// parented to a moving player would be dragged along by it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile prefab;
        [SerializeField, Min(1)] private int capacity = 32;
        [SerializeField, Min(0.01f)] private float projectileRadius = 0.12f;

        private readonly List<Projectile> active = new();
        private readonly Stack<Projectile> free = new();
        private Transform container;

        public int Capacity { get; private set; }
        public int ActiveCount => active.Count;
        public int FreeCount => free.Count;

        /// <summary>
        /// How often a shot had to steal the oldest projectile in flight because nothing was free.
        /// Any non-zero value in normal play means the pool is undersized for the cadence.
        /// </summary>
        public int RecycledUnderPressure { get; private set; }

        /// <summary>Impact point and surface normal, raised whether or not the hit found a receiver.</summary>
        public event Action<Vector3, Vector3> Impacted;

        private void Awake() => Initialize();

        private void OnDestroy()
        {
            if (container != null)
            {
                Destroy(container.gameObject);
            }
        }

        private void Update() => Tick(Time.deltaTime);

        public void Configure(Projectile projectilePrefab, int poolSize, float sweepRadius)
        {
            prefab = projectilePrefab;
            capacity = Mathf.Max(1, poolSize);
            projectileRadius = Mathf.Max(0.01f, sweepRadius);
        }

        /// <summary>Idempotent, so a pool configured after Awake still fills exactly once.</summary>
        public void Initialize()
        {
            if (container != null || prefab == null)
            {
                return;
            }

            container = new GameObject("PlayerProjectilePool").transform;

            for (var index = 0; index < capacity; index++)
            {
                var projectile = Instantiate(prefab, container);
                projectile.name = "Projectile_" + index;
                projectile.Configure(projectileRadius);
                projectile.gameObject.SetActive(false);
                free.Push(projectile);
            }

            Capacity = capacity;
        }

        /// <summary>
        /// One step for every projectile in flight, exposed so the travel and impact rules can be
        /// driven at an exact delta by the tests.
        /// </summary>
        public void Tick(float deltaTime)
        {
            for (var index = active.Count - 1; index >= 0; index--)
            {
                var projectile = active[index];
                if (projectile == null)
                {
                    active.RemoveAt(index);
                    continue;
                }

                var step = projectile.Tick(deltaTime);
                if (step == ProjectileStep.Travelling)
                {
                    continue;
                }

                if (step == ProjectileStep.Impacted)
                {
                    Impacted?.Invoke(projectile.LastImpactPoint, projectile.LastImpactNormal);
                }

                active.RemoveAt(index);
                Release(projectile);
            }
        }

        public Projectile Fire(
            Vector3 origin,
            Vector3 direction,
            float speed,
            float damage,
            float lifetime,
            GameObject owner)
        {
            Initialize();

            var projectile = Acquire();
            if (projectile == null)
            {
                return null;
            }

            active.Add(projectile);
            projectile.gameObject.SetActive(true);
            projectile.Launch(origin, direction, speed, damage, lifetime, owner);
            return projectile;
        }

        /// <summary>Recalls everything in flight, for a wave reset or a death.</summary>
        public void ReleaseAll()
        {
            for (var index = 0; index < active.Count; index++)
            {
                Release(active[index]);
            }

            active.Clear();
        }

        /// <summary>
        /// Exhaustion steals the oldest projectile in flight rather than dropping the shot. Silently
        /// swallowing an input the player made is the worse failure, and the counter above makes the
        /// condition visible instead of invisible.
        /// </summary>
        private Projectile Acquire()
        {
            if (free.Count > 0)
            {
                return free.Pop();
            }

            if (active.Count == 0)
            {
                return null;
            }

            var oldest = active[0];
            active.RemoveAt(0);
            RecycledUnderPressure++;
            return oldest;
        }

        private void Release(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.gameObject.SetActive(false);
            free.Push(projectile);
        }
    }
}
