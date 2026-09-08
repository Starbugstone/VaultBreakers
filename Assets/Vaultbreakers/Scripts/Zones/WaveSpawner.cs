using System;
using System.Collections.Generic;
using UnityEngine;
using Vaultbreakers.Enemies;

namespace Vaultbreakers.Zones
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private float roomSpacing;
        public void SetRoomSpacing(float spacing)=>roomSpacing=spacing;
        [SerializeField] private EnemyRoster roster;
        [SerializeField] private Transform[] points;
        private readonly List<EnemyBrain> subscribed = new(24);
        public WaveProgress Progress { get; } = new();
        public EnemyRoster Roster => roster;
        public event Action Cleared;
        public void Configure(EnemyRoster enemies, Transform[] spawnPoints) { roster = enemies; points = spawnPoints; }
        public void Clear()
        {
            foreach (var enemy in subscribed) if (enemy != null) enemy.Removed -= OnRemoved;
            subscribed.Clear(); Progress.Begin(Progress.ActiveWave);
            if (roster != null) roster.Clear();
        }
        public void Spawn(WaveDefinition wave, int index)
        {
            Clear(); Progress.Begin(index);
            var ordinal = 0;
            foreach (var entry in wave.spawns)
                for (var i = 0; i < entry.count; i++)
                {
                    var point = points[ordinal % points.Length].position + Vector3.forward * roomSpacing * index;
                    var offset = ordinal / points.Length;
                    point += Vector3.right * (offset % 2 == 0 ? offset * 0.6f : -offset * 0.6f);
                    point.y = 0;
                    var enemy = roster.Spawn(entry.role, point);
                    if (enemy == null) throw new InvalidOperationException("Wave exceeds the authored enemy pool capacity.");
                    Progress.Register(enemy.Identity); subscribed.Add(enemy); enemy.Removed += OnRemoved;
                    ordinal++;
                }
            if (Progress.LivingCount == 0) Cleared?.Invoke();
        }
        private void OnRemoved(EnemyBrain enemy)
        {
            if (Progress.Remove(enemy.Identity) && Progress.LivingCount == 0) Cleared?.Invoke();
        }
        private void OnDestroy() { foreach (var enemy in subscribed) if (enemy != null) enemy.Removed -= OnRemoved; }
    }
}
