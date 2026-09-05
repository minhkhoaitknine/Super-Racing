using System;
using UnityEngine;

namespace SuperRacing.Economy
{
    public static class CurrencyWallet
    {
        private const string BalanceKey = "super_racing_currency";
        private const int StartingBalance = 1000;

        public static int Balance => PlayerPrefs.GetInt(BalanceKey, StartingBalance);
        public static event Action<int> BalanceChanged;

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            SetBalance(Balance + amount);
        }

        public static bool TrySpend(int amount)
        {
            if (amount < 0 || Balance < amount) return false;
            SetBalance(Balance - amount);
            return true;
        }

        private static void SetBalance(int value)
        {
            int balance = Mathf.Max(0, value);
            PlayerPrefs.SetInt(BalanceKey, balance);
            PlayerPrefs.Save();
            BalanceChanged?.Invoke(balance);
        }
    }
}
