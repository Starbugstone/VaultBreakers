using System.Collections.Generic;
using UnityEngine;

namespace Vaultbreakers.Debugging
{
    /// <summary>
    /// Fixed-size, newest-first ring of development combat messages. Static state survives a play
    /// session when domain reloading is disabled, so it clears itself on load rather than carrying
    /// the previous session's events into a new one.
    /// </summary>
    public static class CombatEventLog
    {
        public const int Capacity = 8;

        private static readonly List<string> entries = new(Capacity);

        /// <summary>Most recent entry first, so callers can render it without reversing.</summary>
        public static IReadOnlyList<string> Entries => entries;

        public static void Record(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            entries.Insert(0, message);
            if (entries.Count > Capacity)
            {
                entries.RemoveAt(entries.Count - 1);
            }
        }

        public static void Clear() => entries.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession() => Clear();
    }
}
