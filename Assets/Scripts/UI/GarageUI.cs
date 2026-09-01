using SuperRacing.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class GarageUI : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private Text carNameLabel;
        [SerializeField] private Text secondaryCarNameLabel;
        [SerializeField] private Text statsLabel;
        [SerializeField] private Image previewImage;
        [SerializeField] private Image powerFill;
        [SerializeField] private Image accelerationFill;
        [SerializeField] private Image handlingFill;
        [SerializeField] private Image gripFill;
        [SerializeField] private Text powerValueLabel;
        [SerializeField] private Text accelerationValueLabel;
        [SerializeField] private Text handlingValueLabel;
        [SerializeField] private Text gripValueLabel;
        [SerializeField] private Transform vehiclePreviewRoot;
        [SerializeField, Min(0.1f)] private float previewTargetSize = 3.25f;
        [SerializeField] private Vector3 vehiclePositionOffset = new Vector3(0f, -0.96f, 0f);
        [SerializeField] private Vector3 vehicleRotationEuler = new Vector3(0f, 8f, 0f);
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private int selectedIndex;
        private GameObject previewVehicle;

        private void Start()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            selectedIndex = FindSelectedCarIndex();
            RefreshView();
        }

        public void SelectCar(int index)
        {
            if (catalog == null || catalog.Cars.Count == 0)
            {
                return;
            }

            selectedIndex = WrapIndex(index, catalog.Cars.Count);
            RefreshView();
        }

        public void SelectPrevious()
        {
            selectedIndex = WrapIndex(selectedIndex - 1, catalog.Cars.Count);
            RefreshView();
        }

        public void SelectNext()
        {
            selectedIndex = WrapIndex(selectedIndex + 1, catalog.Cars.Count);
            RefreshView();
        }

        public void ConfirmSelection()
        {
            ReturnToMainMenu();
        }

        public void ReturnToMainMenu()
        {
            if (catalog != null && catalog.Cars.Count > 0)
            {
                GameSelection.SelectCar(catalog.Cars[selectedIndex]);
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        public bool ValidateConfiguration()
        {
            if (catalog == null || catalog.Cars.Count == 0 || carNameLabel == null)
            {
                Debug.LogError("GarageUI requires a catalog with at least one car and a car name label.", this);
                return false;
            }

            return true;
        }

        public static int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return (index % count + count) % count;
        }

        private int FindSelectedCarIndex()
        {
            if (!GameSelection.HasCar)
            {
                return 0;
            }

            for (int index = 0; index < catalog.Cars.Count; index++)
            {
                if (catalog.Cars[index] == GameSelection.SelectedCar)
                {
                    return index;
                }
            }

            return 0;
        }

        private void RefreshView()
        {
            CarDefinition car = catalog.Cars[selectedIndex];
            carNameLabel.text = car.DisplayName;
            if (secondaryCarNameLabel != null)
            {
                secondaryCarNameLabel.text = car.DisplayName;
            }

            if (statsLabel != null)
            {
                statsLabel.text =
                    $"Top Speed  {car.MaxSpeedPercent:0}%\n" +
                    $"Acceleration  {car.AccelerationPercent:0}%\n" +
                    $"Steering  {car.SteeringPercent:0}%\n" +
                    $"Grip  {car.GripPercent:0}%";
            }

            SetStat(powerFill, powerValueLabel, car.MaxSpeedPercent / 100f);
            SetStat(accelerationFill, accelerationValueLabel, car.AccelerationPercent / 100f);
            SetStat(handlingFill, handlingValueLabel, car.SteeringPercent / 100f);
            SetStat(gripFill, gripValueLabel, car.GripPercent / 100f);

            if (previewImage != null)
            {
                previewImage.sprite = car.PreviewSprite;
                previewImage.enabled = car.PreviewSprite != null;
            }

            RefreshVehiclePreview(car);
        }

        private static void SetStat(Image fill, Text valueLabel, float normalizedValue)
        {
            float value = Mathf.Clamp01(normalizedValue);
            if (fill != null)
            {
                fill.fillAmount = value;
            }

            if (valueLabel != null)
            {
                valueLabel.text = $"{value * 100f:0} %";
            }
        }

        private void RefreshVehiclePreview(CarDefinition car)
        {
            if (previewVehicle != null)
            {
                Destroy(previewVehicle);
            }

            if (vehiclePreviewRoot == null || car.VehiclePrefab == null)
            {
                return;
            }

            vehiclePreviewRoot.rotation = Quaternion.identity;
            previewVehicle = Instantiate(car.VehiclePrefab, vehiclePreviewRoot, false);
            previewVehicle.name = $"{car.DisplayName} Preview";
            previewVehicle.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            SetLayerRecursively(previewVehicle, vehiclePreviewRoot.gameObject.layer);
            DisableVehicleBehaviour(previewVehicle);
            FitPreviewVehicle(previewVehicle);
        }

        private void FitPreviewVehicle(GameObject vehicle)
        {
            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            float largestSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largestSize > 0.001f)
            {
                vehicle.transform.localScale *= previewTargetSize / largestSize;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            // Align horizontally to center, align bottom of wheels to ground level + offset
            Vector3 targetPosition = vehiclePreviewRoot.position;
            targetPosition.x -= bounds.center.x;
            targetPosition.z -= bounds.center.z;
            targetPosition.y -= bounds.min.y;
            targetPosition += vehiclePositionOffset;

            vehicle.transform.position = targetPosition;
            vehicle.transform.localRotation = Quaternion.Euler(vehicleRotationEuler);
        }

        private static void DisableVehicleBehaviour(GameObject vehicle)
        {
            foreach (MonoBehaviour behaviour in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (Rigidbody body in vehicle.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            foreach (Collider collider in vehicle.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
