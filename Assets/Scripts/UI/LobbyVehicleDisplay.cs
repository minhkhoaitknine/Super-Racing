using SuperRacing.Data;
using UnityEngine;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class LobbyVehicleDisplay : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField, Min(0.1f)] private float targetSize = 4.2f;
        [SerializeField] private float groundY = 0.02f;
        [SerializeField, Range(0, 31)] private int previewLayer;

        public void Configure(GameCatalog gameCatalog, float previewTargetSize, float previewGroundY, int vehicleLayer)
        {
            catalog = gameCatalog;
            targetSize = previewTargetSize;
            groundY = previewGroundY;
            previewLayer = vehicleLayer;
        }

        private void Start()
        {
            CarDefinition car = ResolveSelectedCar();
            if (car == null || car.VehiclePrefab == null)
            {
                Debug.LogError("LobbyVehicleDisplay requires a selected car with a vehicle prefab.", this);
                return;
            }

            ClearPreview();
            GameObject vehicle = Instantiate(car.VehiclePrefab, transform, false);
            vehicle.name = car.DisplayName;
            DisableVehicleBehaviour(vehicle);
            SetLayerRecursively(vehicle, previewLayer);
            FitVehicle(vehicle);
        }

        private CarDefinition ResolveSelectedCar()
        {
            if (GameSelection.HasCar)
            {
                return GameSelection.SelectedCar;
            }

            if (catalog == null || catalog.Cars.Count == 0)
            {
                return null;
            }

            CarDefinition defaultCar = catalog.Cars[0];
            GameSelection.SelectCar(defaultCar);
            return defaultCar;
        }

        private void ClearPreview()
        {
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                GameObject preview = transform.GetChild(index).gameObject;
                preview.SetActive(false);
                Destroy(preview);
            }
        }

        private void FitVehicle(GameObject vehicle)
        {
            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
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
                vehicle.transform.localScale *= targetSize / largestSize;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            vehicle.transform.position += new Vector3(-bounds.center.x, groundY - bounds.min.y, -bounds.center.z);
        }

        private static void DisableVehicleBehaviour(GameObject vehicle)
        {
            foreach (MonoBehaviour behaviour in vehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (Collider collider in vehicle.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody body in vehicle.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
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
