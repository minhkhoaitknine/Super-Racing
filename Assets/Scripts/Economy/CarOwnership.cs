using SuperRacing.Data;
using UnityEngine;

namespace SuperRacing.Economy
{
    public static class CarOwnership
    {
        private const string Prefix = "super_racing_owned_car_";

        public static bool IsOwned(CarDefinition car)
        {
            return car != null && (car.UnlockedByDefault || PlayerPrefs.GetInt(Prefix + car.CarId, 0) != 0);
        }

        public static bool TryPurchase(CarDefinition car)
        {
            if (car == null || IsOwned(car)) return car != null;
            if (!CurrencyWallet.TrySpend(car.PurchasePrice)) return false;
            PlayerPrefs.SetInt(Prefix + car.CarId, 1);
            PlayerPrefs.Save();
            return true;
        }
    }
}
