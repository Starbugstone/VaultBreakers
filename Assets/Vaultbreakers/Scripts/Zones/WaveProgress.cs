using System.Collections.Generic;

namespace Vaultbreakers.Zones
{
    // Identity tracking makes duplicate deaths/despawns harmless, including during resets.
    public sealed class WaveProgress
    {
        private readonly HashSet<int> living = new();
        public int ActiveWave { get; private set; }
        public int CompletedWaves { get; private set; }
        public int LivingCount => living.Count;
        public void Begin(int index) { ActiveWave = index; living.Clear(); }
        public bool Register(int id) => living.Add(id);
        public bool Remove(int id) => living.Remove(id);
        public bool Complete()
        {
            if (living.Count != 0 || CompletedWaves > ActiveWave) return false;
            CompletedWaves = ActiveWave + 1; return true;
        }
        public void RestartRun() { ActiveWave = 0; CompletedWaves = 0; living.Clear(); }
    }
}
