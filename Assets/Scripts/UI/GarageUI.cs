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
        [SerializeField] private Text statsLabel;
        [SerializeField] private Image previewImage;
        [SerializeField] private Transform vehiclePreviewRoot;
        [SerializeField, Min(0f)] private float previewRotationSpeed = 12f;
        [SerializeField, Min(0.1f)] private float previewTargetSize = 4.5f;
        [SerializeField] private string trackSelectionSceneName = "TrackSelection";
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

        private void Update()
        {
            if (vehiclePreviewRoot != null)
            {
                vehiclePreviewRoot.Rotate(0f, previewRotationSpeed * Time.deltaTime, 0f, Space.World);
            }
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
            GameSelection.SelectCar(catalog.Cars[selectedIndex]);
            SceneManager.LoadScene(trackSelectionSceneName);
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        public bool ValidateConfiguration()
        {
            if (catalog == null || catalog.Cars.Count == 0 || carNameLabel == null || statsLabel == null)
            {
                Debug.LogError("GarageUI requires a catalog with at least one car and its text labels.", this);
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
            statsLabel.text =
                $"Top Speed  {car.MaxSpeedKmh:0} km/h\n" +
                $"Acceleration  {car.MotorTorque:0}\n" +
                $"Steering  {car.SteeringAngle:0}\n" +
                $"Grip  {car.Grip:0.0}";

            if (previewImage != null)
            {
                previewImage.sprite = car.PreviewSprite;
                previewImage.enabled = car.PreviewSprite != null;
            }

            RefreshVehiclePreview(car);
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

            vehiclePreviewRoot.rotation = Quaternion.Euler(0f, -25f, 0f);
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

            vehicle.transform.position -= bounds.center;
            vehicle.transform.position += Vector3.up * (bounds.extents.y * 0.35f);
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
