using System.IO;
using UnityEditor;
using UnityEngine;
using Vaultbreakers.Combat;
using Vaultbreakers.Enemies;
using Vaultbreakers.Zones;

namespace Vaultbreakers.Editor
{
    internal static class VaultbreakersWaveBuilder
    {
        public static ZoneController Build(GameObject root, Health player, EnemyRoster roster, GameObject[] dummies)
        {
            Directory.CreateDirectory("Assets/Vaultbreakers/Data/Waves"); AssetDatabase.Refresh();
            var waves = new WaveDefinition[3];
            var entries = new[] {
                new[] { new EnemySpawnEntry(EnemyRole.Grunt, 3) },
                new[] { new EnemySpawnEntry(EnemyRole.Grunt, 2), new EnemySpawnEntry(EnemyRole.Shooter, 1) },
                new[] { new EnemySpawnEntry(EnemyRole.Grunt, 1), new EnemySpawnEntry(EnemyRole.Shooter, 1), new EnemySpawnEntry(EnemyRole.Bruiser, 1) }
            };
            var titles = new[] { "FIRST CONTACT", "CROSSFIRE", "HEAVY METAL" };
            for (var i = 0; i < waves.Length; i++)
            {
                var path = "Assets/Vaultbreakers/Data/Waves/Wave_" + (i + 1) + ".asset";
                waves[i] = AssetDatabase.LoadAssetAtPath<WaveDefinition>(path);
                if (waves[i] == null)
                {
                    waves[i] = ScriptableObject.CreateInstance<WaveDefinition>(); waves[i].title = titles[i]; waves[i].spawns = entries[i];
                    AssetDatabase.CreateAsset(waves[i], path);
                }
            }
            var spawner = root.AddComponent<WaveSpawner>();
            spawner.Configure(roster, new[] { GameObject.Find("SpawnPoint_A").transform, GameObject.Find("SpawnPoint_B").transform, GameObject.Find("SpawnPoint_C").transform });
            var zone = root.AddComponent<ZoneController>(); zone.Configure(player, spawner, waves, dummies);
            return zone;
        }
    }
}
