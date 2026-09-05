using SuperRacing.Data;
using UnityEngine;

namespace SuperRacing.Economy
{
    public enum CarUpgradeType
    {
        TopSpeed,
        Acceleration,
        Braking,
        Steering,
        Grip
    }

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

    public static class CarProgression
    {
        public const int MaxUpgradeLevel = 5;
        public const float MaxHeadroomGain = 0.5f;
        private const string UpgradePrefix = "super_racing_upgrade_";
        private const string PaintOwnedPrefix = "super_racing_paint_owned_";
        private const string PaintEquippedPrefix = "super_racing_paint_equipped_";

        private static readonly Color[] PaintColors =
        {
            Color.white,
            new Color(0.05f, 0.35f, 0.95f),
            new Color(0.9f, 0.06f, 0.04f),
            new Color(0.05f, 0.05f, 0.06f),
            new Color(0.95f, 0.65f, 0.04f),
            new Color(0.18f, 0.8f, 0.35f)
        };

        public static int PaintCount => PaintColors.Length;

        public static int GetUpgradeLevel(CarDefinition car, CarUpgradeType type)
        {
            return car == null ? 0 : Mathf.Clamp(PlayerPrefs.GetInt(UpgradeKey(car, type), 0), 0, MaxUpgradeLevel);
        }

        public static int GetUpgradePrice(CarDefinition car, CarUpgradeType type)
        {
            int level = GetUpgradeLevel(car, type);
            return !CanUpgrade(car, type) ? 0 : 600 * ((int)type + 1) * (level + 1);
        }

        public static bool CanUpgrade(CarDefinition car, CarUpgradeType type)
        {
            return car != null && GetBasePercent(car, type) < 100f && GetUpgradeLevel(car, type) < MaxUpgradeLevel;
        }

        public static bool TryUpgrade(CarDefinition car, CarUpgradeType type)
        {
            if (car == null || !CarOwnership.IsOwned(car) || !CanUpgrade(car, type)) return false;
            int level = GetUpgradeLevel(car, type);
            if (!CurrencyWallet.TrySpend(GetUpgradePrice(car, type))) return false;
            PlayerPrefs.SetInt(UpgradeKey(car, type), level + 1);
            PlayerPrefs.Save();
            return true;
        }

        public static float GetEffectivePercent(CarDefinition car, CarUpgradeType type, float basePercent)
        {
            return CalculateEffectivePercent(basePercent, GetUpgradeLevel(car, type));
        }

        public static float CalculateEffectivePercent(float basePercent, int upgradeLevel)
        {
            float baseValue = Mathf.Clamp(basePercent, 0f, 100f);
            float levelProgress = Mathf.Clamp(upgradeLevel, 0, MaxUpgradeLevel) / (float)MaxUpgradeLevel;
            float gainedHeadroom = (100f - baseValue) * MaxHeadroomGain * levelProgress;
            return baseValue + gainedHeadroom;
        }

        public static Color GetPaintColor(int index) => PaintColors[Mathf.Clamp(index, 0, PaintColors.Length - 1)];

        public static int GetPaintPrice(int index) => index <= 0 ? 0 : 900 + index * 550;

        public static bool IsPaintOwned(CarDefinition car, int index)
        {
            return car != null && (index == 0 || PlayerPrefs.GetInt(PaintOwnedKey(car, index), 0) != 0);
        }

        public static int GetEquippedPaint(CarDefinition car)
        {
            return car == null ? 0 : Mathf.Clamp(PlayerPrefs.GetInt(PaintEquippedPrefix + car.CarId, 0), 0, PaintColors.Length - 1);
        }

        public static bool TryBuyAndEquipPaint(CarDefinition car, int index)
        {
            if (car == null || !CarOwnership.IsOwned(car) || index < 0 || index >= PaintColors.Length) return false;
            if (!IsPaintOwned(car, index))
            {
                if (!CurrencyWallet.TrySpend(GetPaintPrice(index))) return false;
                PlayerPrefs.SetInt(PaintOwnedKey(car, index), 1);
            }

            PlayerPrefs.SetInt(PaintEquippedPrefix + car.CarId, index);
            PlayerPrefs.Save();
            return true;
        }

        public static void ApplyPaint(GameObject vehicle, CarDefinition car)
        {
            if (vehicle == null || car == null) return;
            int paintIndex = GetEquippedPaint(car);
            if (paintIndex == 0) return;
            Color color = GetPaintColor(paintIndex);
            foreach (Renderer renderer in vehicle.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.materials;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material material = materials[index];
                    if (material == null || !material.name.ToLowerInvariant().Contains("body")) continue;
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    else if (material.HasProperty("_Color")) material.color = color;
                }
            }
        }

        private static string UpgradeKey(CarDefinition car, CarUpgradeType type) => $"{UpgradePrefix}{car.CarId}_{type}";
        private static string PaintOwnedKey(CarDefinition car, int index) => $"{PaintOwnedPrefix}{car.CarId}_{index}";

        private static float GetBasePercent(CarDefinition car, CarUpgradeType type)
        {
            return type switch
            {
                CarUpgradeType.TopSpeed => car.BaseMaxSpeedPercent,
                CarUpgradeType.Acceleration => car.BaseAccelerationPercent,
                CarUpgradeType.Braking => car.BaseBrakingPercent,
                CarUpgradeType.Steering => car.BaseSteeringPercent,
                _ => car.BaseGripPercent
            };
        }
    }
}
