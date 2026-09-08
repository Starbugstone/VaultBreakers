using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Enemies;

namespace Vaultbreakers.Tests.EditMode
{
    public sealed class EnemyRulesTests
    {
        [Test] public void AttackCannotLandBeforeItsTellAndOnlyStrikesOnce()
        {
            var attack = new EnemyAttack(); attack.Begin(0.65f);
            Assert.That(attack.Tick(0.64f, 0.1f, 0.8f), Is.False);
            Assert.That(attack.State, Is.EqualTo(EnemyState.Windup));
            Assert.That(attack.Tick(0.01f, 0.1f, 0.8f), Is.True);
            Assert.That(attack.Tick(0.1f, 0.1f, 0.8f), Is.False);
            Assert.That(attack.State, Is.EqualTo(EnemyState.Recovery));
            attack.Tick(0.8f, 0.1f, 0.8f);
            Assert.That(attack.State, Is.EqualTo(EnemyState.Approach));
        }
        [Test] public void DeathCancelsAnAttackEvenAtItsDamageBoundary()
        {
            var attack = new EnemyAttack(); attack.Begin(0.1f); attack.Reset(true);
            Assert.That(attack.Tick(5, 0.1f, 0.8f), Is.False);
        }
        [TestCase(0, 2, true)] [TestCase(0, -2, false)] [TestCase(3, 0, false)] [TestCase(0, 3.1f, false)]
        public void StrikeMatchesCommittedFrontalArea(float x, float z, bool expected)
        { Assert.That(EnemyAttack.Contains(Vector3.zero, Vector3.forward, new Vector3(x, 0, z), 3, 150), Is.EqualTo(expected)); }
    }
}
