using NUnit.Framework;
using UnityEngine;
using Vaultbreakers.Gems;

namespace Vaultbreakers.Tests.EditMode
{
    public sealed class GemRulesTests
    {
        private GemBalance balance;
        private GameObject host;
        private GemPool pool;
        [SetUp] public void Setup()
        {
            balance = ScriptableObject.CreateInstance<GemBalance>(); balance.capacity = 8;
            host = new GameObject("Gem rules test"); pool = host.AddComponent<GemPool>();
            pool.Configure(balance, new Mesh[5], new Material[5]); pool.Initialize();
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(host); Object.DestroyImmediate(balance); }

        [TestCase(LootSource.Grunt)] [TestCase(LootSource.Shooter)] [TestCase(LootSource.Bruiser)]
        [TestCase(LootSource.Crate)] [TestCase(LootSource.Chest)]
        public void EverySourceDropsSeveralGems(LootSource source)
        { pool.Drop(Vector3.zero, source); Assert.That(pool.ActiveValue, Is.EqualTo(balance.DropCount(source))); Assert.That(pool.ActiveCount, Is.GreaterThan(1)); }

        [Test] public void BoundedPoolPreservesAllFiveKindsAndEveryUnitAtCapacity()
        {
            for (var i = 0; i < 200; i++) pool.Spawn(Vector3.zero, GemKind.Melee, 1);
            for (var i = 1; i < 5; i++) pool.Spawn(Vector3.zero, (GemKind)i, 7);
            Assert.That(pool.ActiveCount, Is.LessThanOrEqualTo(8)); Assert.That(pool.ActiveValue, Is.EqualTo(228));
            var wallet = new GemWallet(); pool.Collected += (kind, value, position) => wallet.Add(kind, value);
            for (var i = 0; i < 40; i++) pool.Tick(.02f, Vector3.zero, true);
            Assert.That(wallet.Total, Is.EqualTo(228)); Assert.That(pool.ActiveCount, Is.Zero);
            for (var i = 1; i < 5; i++) Assert.That(wallet[(GemKind)i], Is.EqualTo(7));
            pool.Tick(1, Vector3.zero, true); Assert.That(wallet.Total, Is.EqualTo(228), "A collected slot cannot pay twice.");
        }
        [Test] public void BurstDelayPreventsInstantPickupAndPausedTimeDoesNotAdvanceIt()
        {
            pool.Spawn(Vector3.zero, GemKind.Gold, 1);
            pool.Tick(0, Vector3.zero, true); pool.Tick(.1f, Vector3.zero, true);
            Assert.That(pool.CollectedUnits, Is.Zero);
            for (var i = 0; i < 30; i++) pool.Tick(.02f, Vector3.zero, true);
            Assert.That(pool.CollectedUnits, Is.EqualTo(1));
        }
        [Test] public void DistantGemsExpireAndClearingNeverAwardsThem()
        {
            pool.Drop(Vector3.zero, LootSource.Crate);
            pool.Tick(balance.lifetime + .1f, Vector3.one * 50, true);
            Assert.That(pool.ActiveCount, Is.Zero); Assert.That(pool.ExpiredUnits, Is.EqualTo(balance.crateGems));
            pool.Drop(Vector3.zero, LootSource.Chest); pool.Clear();
            Assert.That(pool.ActiveCount, Is.Zero); Assert.That(pool.CollectedUnits, Is.Zero);
        }
        [Test] public void SuccessfulRoomsBankGemsButRetryCannotFarmTheCurrentRoom()
        {
            var wallet = new GemWallet(); wallet.Add(GemKind.Melee, 3); wallet.Bank();
            wallet.Add(GemKind.Gold, 12); wallet.Retry();
            Assert.That(wallet.Total, Is.EqualTo(3)); Assert.That(wallet[GemKind.Gold], Is.Zero);
            wallet.Add(GemKind.Gold, 6); wallet.Bank(); wallet.Add(GemKind.Relic, 2); wallet.Retry();
            Assert.That(wallet.Total, Is.EqualTo(9)); wallet.Reset(); wallet.Retry(); Assert.That(wallet.Total, Is.Zero);
        }
        [Test] public void MagnetNeedsRangeDelayAndAnUnobstructedRoute()
        {
            Assert.That(GemRules.CanAttract(.1f, 0, true, balance), Is.False);
            Assert.That(GemRules.CanAttract(1, 1, false, balance), Is.False);
            Assert.That(GemRules.CanAttract(1, 100, true, balance), Is.False);
            Assert.That(GemRules.CanAttract(1, 1, true, balance), Is.True);
        }
    }
}
