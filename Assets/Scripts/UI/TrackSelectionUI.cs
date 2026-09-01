using SuperRacing.Data;
using SuperRacing.Race;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class TrackSelectionUI : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;
        [SerializeField] private Text trackNameLabel;
        [SerializeField] private Text lapCountLabel;
        [SerializeField] private Text recordLabel;
        [SerializeField] private Image previewImage;
        [SerializeField] private string garageSceneName = "Garage";

        private int selectedIndex;

        private void Start()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            selectedIndex = FindSelectedTrackIndex();
            RefreshView();
        }

        public void SelectPrevious()
        {
            selectedIndex = GarageUI.WrapIndex(selectedIndex - 1, catalog.Tracks.Count);
            RefreshView();
        }

        public void SelectNext()
        {
            selectedIndex = GarageUI.WrapIndex(selectedIndex + 1, catalog.Tracks.Count);
            RefreshView();
        }

        public void StartRace()
        {
            TrackDefinition track = catalog.Tracks[selectedIndex];
            GameSelection.SelectTrack(track);
            SceneManager.LoadScene(track.SceneName);
        }

        public void ReturnToGarage()
        {
            SceneManager.LoadScene(garageSceneName);
        }

        public bool ValidateConfiguration()
        {
            if (catalog == null || catalog.Tracks.Count == 0 ||
                trackNameLabel == null || lapCountLabel == null || recordLabel == null)
            {
                Debug.LogError("TrackSelectionUI requires a catalog with tracks and all text labels.", this);
                return false;
            }

            return true;
        }

        private int FindSelectedTrackIndex()
        {
            if (!GameSelection.HasTrack)
            {
                return 0;
            }

            for (int index = 0; index < catalog.Tracks.Count; index++)
            {
                if (catalog.Tracks[index] == GameSelection.SelectedTrack)
                {
                    return index;
                }
            }

            return 0;
        }

        private void RefreshView()
        {
            TrackDefinition track = catalog.Tracks[selectedIndex];
            trackNameLabel.text = track.DisplayName;
            lapCountLabel.text = $"{track.LapCount} Laps";

            if (GameSelection.HasCar && RecordManager.TryGetBestTime(track.TrackId, GameSelection.SelectedCar.CarId, out float bestTime))
            {
                recordLabel.text = $"Best  {RaceHUD.FormatTime(bestTime)}";
            }
            else
            {
                recordLabel.text = "Best  --:--.---";
            }

            if (previewImage != null)
            {
                previewImage.sprite = track.PreviewSprite;
                previewImage.enabled = track.PreviewSprite != null;
            }
        }
    }
}
