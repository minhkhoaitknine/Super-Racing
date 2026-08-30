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

        [Header("Driving Stats")]
        [Min(1f)] [SerializeField] private float maxSpeedKmh = 140f;
        [Min(0f)] [SerializeField] private float motorTorque = 1500f;
        [Min(0f)] [SerializeField] private float brakeTorque = 3000f;
        [Range(1f, 60f)] [SerializeField] private float steeringAngle = 30f;
        [Min(0f)] [SerializeField] private float grip = 1f;

        public string CarId => carId;
        public string DisplayName => displayName;
        public GameObject VehiclePrefab => vehiclePrefab;
        public Sprite PreviewSprite => previewSprite;
        public float MaxSpeedKmh => maxSpeedKmh;
        public float MotorTorque => motorTorque;
        public float BrakeTorque => brakeTorque;
        public float SteeringAngle => steeringAngle;
        public float Grip => grip;

        private void OnValidate()
        {
            carId = NormalizeId(carId, "car");
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
