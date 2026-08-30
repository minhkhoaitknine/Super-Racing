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
        [SerializeField] private string trackSelectionSceneName = "TrackSelection";
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private int selectedIndex;

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
        }
    }
}
