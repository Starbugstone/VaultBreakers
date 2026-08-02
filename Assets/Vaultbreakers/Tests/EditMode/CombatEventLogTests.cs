using NUnit.Framework;
using Vaultbreakers.Debugging;

namespace Vaultbreakers.Tests.EditMode
{
    /// <summary>
    /// The overlay renders this list directly, so ordering and the capacity bound are part of its
    /// contract rather than an implementation detail.
    /// </summary>
    public sealed class CombatEventLogTests
    {
        [SetUp]
        public void SetUp() => CombatEventLog.Clear();

        [TearDown]
        public void TearDown() => CombatEventLog.Clear();

        [Test]
        public void Record_KeepsTheNewestEntryFirst()
        {
            CombatEventLog.Record("first");
            CombatEventLog.Record("second");

            Assert.That(CombatEventLog.Entries[0], Is.EqualTo("second"));
            Assert.That(CombatEventLog.Entries[1], Is.EqualTo("first"));
        }

        [Test]
        public void Record_DropsTheOldestEntryPastCapacity()
        {
            for (var index = 0; index < CombatEventLog.Capacity + 5; index++)
            {
                CombatEventLog.Record("entry " + index);
            }

            Assert.That(CombatEventLog.Entries.Count, Is.EqualTo(CombatEventLog.Capacity));
            Assert.That(CombatEventLog.Entries[0], Is.EqualTo("entry " + (CombatEventLog.Capacity + 4)));
            Assert.That(CombatEventLog.Entries, Has.No.Member("entry 0"));
        }

        [Test]
        public void Record_IgnoresEmptyMessages()
        {
            CombatEventLog.Record(null);
            CombatEventLog.Record(string.Empty);

            Assert.That(CombatEventLog.Entries, Is.Empty);
        }

        [Test]
        public void Clear_EmptiesTheLog()
        {
            CombatEventLog.Record("something happened");
            CombatEventLog.Clear();

            Assert.That(CombatEventLog.Entries, Is.Empty);
        }
    }
}
