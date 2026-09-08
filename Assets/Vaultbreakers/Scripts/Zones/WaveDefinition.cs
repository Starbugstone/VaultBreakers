using UnityEngine;
using Vaultbreakers.Enemies;

namespace Vaultbreakers.Zones
{
    [System.Serializable]
    public struct EnemySpawnEntry
    {
        public EnemyRole role;
        [Range(0, 8)] public int count;
        public EnemySpawnEntry(EnemyRole kind, int number) { role = kind; count = number; }
    }
    [CreateAssetMenu(menuName = "Vaultbreakers/Wave")]
    public sealed class WaveDefinition : ScriptableObject
    {
        public string title;
        public EnemySpawnEntry[] spawns;
        [Min(0)] public float nextWaveDelay = 1.8f;
    }
}
