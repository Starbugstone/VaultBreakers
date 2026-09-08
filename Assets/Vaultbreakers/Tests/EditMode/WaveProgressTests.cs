using NUnit.Framework;
using Vaultbreakers.Zones;
namespace Vaultbreakers.Tests.EditMode
{
    public sealed class WaveProgressTests
    {
        [Test] public void DuplicateDeathDoesNotCompleteAnOccupiedWave()
        {
            var run = new WaveProgress(); run.Begin(0); run.Register(1); run.Register(2);
            Assert.That(run.Register(2), Is.False); Assert.That(run.Remove(1), Is.True);
            Assert.That(run.Remove(1), Is.False); Assert.That(run.LivingCount, Is.EqualTo(1));
            Assert.That(run.Complete(), Is.False);
            run.Remove(2); Assert.That(run.Complete(), Is.True); Assert.That(run.Complete(), Is.False);
        }
        [Test] public void RetryClearsActiveMembershipAndPreservesCompletedWaves()
        {
            var run = new WaveProgress(); run.Begin(0); run.Register(1); run.Remove(1); run.Complete();
            run.Begin(1); run.Register(2); run.Begin(1);
            Assert.That(run.CompletedWaves, Is.EqualTo(1)); Assert.That(run.ActiveWave, Is.EqualTo(1));
            Assert.That(run.LivingCount, Is.Zero); Assert.That(run.Remove(2), Is.False);
            run.RestartRun(); Assert.That(run.CompletedWaves, Is.Zero); Assert.That(run.ActiveWave, Is.Zero);
        }
    }
}
