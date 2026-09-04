using UnityEngine;

namespace SuperRacing.Data
{
    [CreateAssetMenu(fileName = "CarDefinition", menuName = "Super Racing/Car Definition")]
    public sealed class CarDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string carId = "car";
        [SerializeField] private string displayName = "New Car";
        [SerializeField] private GameObject vehiclePrefab;
        [SerializeField] private Sprite previewSprite;

        [Header("Shop")]
        [SerializeField] private bool unlockedByDefault;
        [Min(0)] [SerializeField] private int purchasePrice = 5000;

        [Header("Driving Stats (% of VehicleController defaults)")]
        [Range(0f, 100f)] [SerializeField] private float maxSpeedPercent = 100f;
        [Range(0f, 100f)] [SerializeField] private float accelerationPercent = 100f;
        [Range(0f, 100f)] [SerializeField] private float brakingPercent = 100f;
        [Range(0f, 100f)] [SerializeField] private float steeringPercent = 100f;
        [Range(0f, 100f)] [SerializeField] private float gripPercent = 100f;

        public string CarId => carId;
        public string DisplayName => displayName;
        public GameObject VehiclePrefab => vehiclePrefab;
        public Sprite PreviewSprite => previewSprite;
        public bool UnlockedByDefault => unlockedByDefault;
        public int PurchasePrice => purchasePrice;
        public float MaxSpeedPercent => maxSpeedPercent;
        public float AccelerationPercent => accelerationPercent;
        public float BrakingPercent => brakingPercent;
        public float SteeringPercent => steeringPercent;
        public float GripPercent => gripPercent;

        private void OnValidate()
        {
            carId = NormalizeId(carId, "car");
            purchasePrice = Mathf.Max(0, purchasePrice);
        }

        private static string NormalizeId(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return value.Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }
}
