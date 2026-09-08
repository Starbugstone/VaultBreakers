using System.Collections.Generic;
using UnityEngine;
using Vaultbreakers.Combat;

namespace Vaultbreakers.Enemies
{
    public sealed class EnemyRoster : MonoBehaviour
    {
        [SerializeField] private EnemyBrain[] prefabs;
        [SerializeField] private Health player;
        [SerializeField] private ProjectilePool projectiles;
        private readonly List<EnemyBrain> instances = new(24);
        public IReadOnlyList<EnemyBrain> Instances => instances;
        public ProjectilePool Projectiles => projectiles;
        public void Configure(EnemyBrain[] templates, Health target, ProjectilePool pool)
        { prefabs = templates; player = target; projectiles = pool; }
        private void Start() => Initialize();
        public void Initialize()
        {
            if (instances.Count != 0 || prefabs == null) return;
            foreach (var template in prefabs)
                for (var i = 0; i < 8; i++)
                {
                    var enemy = Instantiate(template, transform);
                    enemy.gameObject.SetActive(false); instances.Add(enemy);
                }
        }
        public EnemyBrain Spawn(EnemyRole role, Vector3 position)
        {
            Initialize();
            foreach (var enemy in instances)
                if (!enemy.gameObject.activeSelf && enemy.Definition.role == role)
                { enemy.Spawn(player, projectiles, position); return enemy; }
            return null;
        }
        public void Clear()
        {
            foreach (var enemy in instances) if (enemy != null) enemy.Despawn();
            if (projectiles != null) projectiles.ReleaseAll();
        }
    }
}
