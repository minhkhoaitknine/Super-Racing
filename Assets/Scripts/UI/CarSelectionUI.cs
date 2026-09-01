using SuperRacing.Data;
using SuperRacing.Race;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class CarSelectionUI : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private string trackSelectionSceneName = "TrackSelection";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Labels")]
        [SerializeField] private Text carNameLabel;
        [SerializeField] private Text statsLabel;

        [Header("3D Preview")]
        [SerializeField] private Transform previewRoot;
        [SerializeField] private float previewRotationSpeed = 40f;

        private int selectedIndex;
        private GameObject previewInstance;

        private void Start()
        {
            if (catalog == null || catalog.Cars.Count == 0 || carNameLabel == null)
            {
                Debug.LogError("CarSelectionUI: missing catalog or labels.", this);
                enabled = false;
                return;
            }

            selectedIndex = FindSelectedCarIndex();
            RefreshView();
        }

        private void Update()
        {
            if (previewRoot != null && previewRotationSpeed > 0f)
                previewRoot.Rotate(0f, previewRotationSpeed * Time.deltaTime, 0f, Space.World);
        }

        public void SelectPrevious()
        {
            selectedIndex = Wrap(selectedIndex - 1, catalog.Cars.Count);
            RefreshView();
        }

        public void SelectNext()
        {
            selectedIndex = Wrap(selectedIndex + 1, catalog.Cars.Count);
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

        private void RefreshView()
        {
            CarDefinition car = catalog.Cars[selectedIndex];
            carNameLabel.text = car.DisplayName;

            if (statsLabel != null)
            {
                string record = "";
                if (GameSelection.HasTrack && RecordManager.TryGetBestTime(
                    GameSelection.SelectedTrack.TrackId, car.CarId, out float best))
                    record = $"\nBest  {RaceHUD.FormatTime(best)}";

                statsLabel.text =
                    $"Top Speed  {car.MaxSpeedKmh:0} km/h\n" +
                    $"Motor      {car.MotorTorque:0}\n" +
                    $"Steering   {car.SteeringAngle:0}\n" +
                    $"Grip       {car.Grip:0.0}" +
                    record;
            }

            RefreshPreview(car);
        }

        private void RefreshPreview(CarDefinition car)
        {
            if (previewInstance != null)
                Destroy(previewInstance);

            if (previewRoot == null || car.VehiclePrefab == null)
                return;

            previewRoot.rotation = Quaternion.identity;
            previewInstance = Instantiate(car.VehiclePrefab, previewRoot, false);
            previewInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            foreach (var mb in previewInstance.GetComponentsInChildren<MonoBehaviour>(true))
                mb.enabled = false;
            foreach (var rb in previewInstance.GetComponentsInChildren<Rigidbody>(true))
            { rb.isKinematic = true; rb.useGravity = false; }
            foreach (var col in previewInstance.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            FitPreview(previewInstance);
        }

        private void FitPreview(GameObject vehicle)
        {
            var renderers = vehicle.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (largest > 0.001f)
                vehicle.transform.localScale *= 3f / largest;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 pos = previewRoot.position;
            pos.x -= bounds.center.x;
            pos.z -= bounds.center.z;
            pos.y -= bounds.min.y - 0.05f;
            vehicle.transform.position = pos;
        }

        private int FindSelectedCarIndex()
        {
            if (!GameSelection.HasCar) return 0;
            for (int i = 0; i < catalog.Cars.Count; i++)
                if (catalog.Cars[i] == GameSelection.SelectedCar)
                    return i;
            return 0;
        }

        private static int Wrap(int index, int count) =>
            count <= 0 ? 0 : (index % count + count) % count;
    }
}
