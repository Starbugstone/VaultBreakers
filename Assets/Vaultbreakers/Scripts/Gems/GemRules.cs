using UnityEngine;

namespace Vaultbreakers.Gems
{
    /// <summary>Deterministic drop composition and bounded storage; no input or scene ownership.</summary>
    public static class GemRules
    {
        public const int KindCount = 5;

        public static GemKind KindFor(LootSource source, int index)
        {
            if (source == LootSource.Chest) return (GemKind)(index % KindCount);
            if (source == LootSource.Crate) return index % 2 == 0 ? GemKind.Gold : (GemKind)((index / 2) % 4);
            if (index % 3 == 2) return GemKind.Gold;
            return source switch
            {
                LootSource.Shooter => GemKind.Ranged,
                LootSource.Bruiser => index % 2 == 0 ? GemKind.Shield : GemKind.Melee,
                _ => GemKind.Melee
            };
        }

        public static bool CanAttract(float age, float distanceSquared, bool clearPath, GemBalance balance)
            => age >= balance.magnetDelay && clearPath && distanceSquared <= balance.magnetRadius * balance.magnetRadius;

        // Reserve room for an unseen kind. At the cap all five kinds can still keep their value.
        public static bool ShouldMerge(int active, int capacity, bool sameKindExists)
            => sameKindExists && active >= capacity - (KindCount - 1);
    }

    public sealed class GemWallet
    {
        private readonly int[] values = new int[GemRules.KindCount];
        private readonly int[] checkpoint = new int[GemRules.KindCount];
        public int Total { get; private set; }
        public int this[GemKind kind] => values[(int)kind];
        public void Add(GemKind kind, int amount)
        {
            if (amount <= 0) return;
            values[(int)kind] += amount; Total += amount;
        }
        public void Bank() { System.Array.Copy(values, checkpoint, values.Length); }
        public void Retry()
        {
            Total = 0;
            for (var i = 0; i < values.Length; i++) { values[i] = checkpoint[i]; Total += values[i]; }
        }
        public void Reset()
        {
            System.Array.Clear(values, 0, values.Length);
            System.Array.Clear(checkpoint, 0, checkpoint.Length); Total = 0;
        }
    }
}
