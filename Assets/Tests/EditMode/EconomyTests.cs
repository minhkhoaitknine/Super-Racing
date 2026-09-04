using NUnit.Framework;
using SuperRacing.Data;
using SuperRacing.Economy;
using UnityEngine;

namespace SuperRacing.Tests
{
    public sealed class EconomyTests
    {
        private const string BalanceKey = "super_racing_currency";

        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteKey(BalanceKey);

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteKey(BalanceKey);

        [Test]
        public void WalletAddsAndRejectsUnaffordablePurchase()
        {
            Assert.That(CurrencyWallet.Balance, Is.EqualTo(1000));
            CurrencyWallet.Add(500);
            Assert.That(CurrencyWallet.TrySpend(1200), Is.True);
            Assert.That(CurrencyWallet.Balance, Is.EqualTo(300));
            Assert.That(CurrencyWallet.TrySpend(301), Is.False);
            Assert.That(CurrencyWallet.Balance, Is.EqualTo(300));
        }

        [Test]
        public void RewardCalculatorCombinesFinishRecordAndCappedDrift()
        {
            TrackDefinition track = ScriptableObject.CreateInstance<TrackDefinition>();
            RaceRewardSummary rewards = RaceRewardCalculator.Calculate(track, true, 1000f);

            Assert.That(rewards.CompletionReward, Is.EqualTo(600));
            Assert.That(rewards.NewRecordBonus, Is.EqualTo(200));
            Assert.That(rewards.CleanDriftBonus, Is.EqualTo(500));
            Assert.That(rewards.Total, Is.EqualTo(1300));
            Object.DestroyImmediate(track);
        }
    }
}
