using NUnit.Framework;
using SuperRacing.Data;
using SuperRacing.Economy;
using UnityEngine;

namespace SuperRacing.Tests
{
    public sealed class EconomyTests
    {
        private const string BalanceKey = "super_racing_currency";
        private const string OwnedCarKey = "super_racing_owned_car_car";
        private const string SpeedUpgradeKey = "super_racing_upgrade_car_TopSpeed";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(BalanceKey);
            PlayerPrefs.DeleteKey(OwnedCarKey);
            PlayerPrefs.DeleteKey(SpeedUpgradeKey);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(BalanceKey);
            PlayerPrefs.DeleteKey(OwnedCarKey);
            PlayerPrefs.DeleteKey(SpeedUpgradeKey);
        }

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

        [Test]
        public void UpgradeIsStoredPerCarAndChargesItsPrice()
        {
            CarDefinition car = ScriptableObject.CreateInstance<CarDefinition>();
            PlayerPrefs.SetInt(OwnedCarKey, 1);

            Assert.That(CarProgression.TryUpgrade(car, CarUpgradeType.TopSpeed), Is.True);
            Assert.That(CarProgression.GetUpgradeLevel(car, CarUpgradeType.TopSpeed), Is.EqualTo(1));
            Assert.That(CurrencyWallet.Balance, Is.EqualTo(400));
            Assert.That(CarProgression.TryUpgrade(car, CarUpgradeType.TopSpeed), Is.False);

            Object.DestroyImmediate(car);
        }
    }
}
