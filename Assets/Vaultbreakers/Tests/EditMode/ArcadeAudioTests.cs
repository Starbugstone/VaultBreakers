using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Combat;
namespace Vaultbreakers.Tests.EditMode
{
    public sealed class ArcadeAudioTests
    {
        [TestCase("VB_MeleeSwing", .16f, 1400f, .35f)]
        [TestCase("VB_RangedFire", .11f, 900f, .4f)]
        [TestCase("VB_ShieldBlock", .1f, 520f, .7f)]
        [TestCase("Dock9_Core", .9f, 660f, 1.2f)]
        public void CuesAreRepeatableFiniteBoundedAndTaperToSilence(string name,float duration,float hz,float decay)
        {
            var samples=PlaceholderAudio.Synthesize(name,duration,hz,decay);
            Assert.That(samples,Is.EqualTo(PlaceholderAudio.Synthesize(name,duration,hz,decay)));
            Assert.That(samples[0],Is.Zero);Assert.That(samples[samples.Length-1],Is.Zero);
            var energy=0f;
            foreach(var sample in samples){Assert.That(float.IsNaN(sample),Is.False);Assert.That(Mathf.Abs(sample),Is.LessThan(1));energy+=sample*sample;}
            Assert.That(energy/samples.Length,Is.GreaterThan(.00001f));
        }
    }
}
