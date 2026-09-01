using System.Collections.Generic;
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
        [SerializeField] private Transform trackPreviewRoot;
        [SerializeField, Min(0.1f)] private float previewTargetSize = 10f;
        [SerializeField] private List<Image> trackCards = new();
        [SerializeField] private Sprite normalCardSprite;
        [SerializeField] private Sprite selectedCardSprite;
        [SerializeField] private string garageSceneName = "Garage";

        private int selectedIndex;
        private GameObject previewTrack;

        public void Configure(
            GameCatalog gameCatalog,
            Text nameLabel,
            Text lapsLabel,
            Text bestRecordLabel,
            Transform previewRoot,
            float targetSize,
            string returnSceneName,
            List<Image> cards,
            Sprite normalSprite,
            Sprite selectedSprite)
        {
            catalog = gameCatalog;
            trackNameLabel = nameLabel;
            lapCountLabel = lapsLabel;
            recordLabel = bestRecordLabel;
            trackPreviewRoot = previewRoot;
            previewTargetSize = targetSize;
            garageSceneName = returnSceneName;
            trackCards = cards;
            normalCardSprite = normalSprite;
            selectedCardSprite = selectedSprite;
        }

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

        public void SelectTrack(int index)
        {
            selectedIndex = GarageUI.WrapIndex(index, catalog.Tracks.Count);
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

            RefreshTrackPreview(track);
            RefreshCardSelection();
        }

        private void RefreshCardSelection()
        {
            for (int index = 0; index < trackCards.Count; index++)
            {
                if (trackCards[index] != null)
                {
                    trackCards[index].sprite = index == selectedIndex ? selectedCardSprite : normalCardSprite;
                }
            }
        }

        private void RefreshTrackPreview(TrackDefinition track)
        {
            if (previewTrack != null)
            {
                previewTrack.SetActive(false);
                Destroy(previewTrack);
            }

            if (trackPreviewRoot == null || track.PreviewPrefab == null)
            {
                Debug.LogError($"Track '{track.DisplayName}' requires a 3D preview prefab.", this);
                return;
            }

            trackPreviewRoot.localRotation = Quaternion.identity;
            previewTrack = Instantiate(track.PreviewPrefab, trackPreviewRoot, false);
            previewTrack.name = $"{track.DisplayName} Preview";
            DisablePreviewBehaviour(previewTrack);
            SetLayerRecursively(previewTrack, trackPreviewRoot.gameObject.layer);
            FitPreview(previewTrack);
        }

        private void FitPreview(GameObject target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
            if (horizontalSize > 0.001f)
            {
                target.transform.localScale *= previewTargetSize / horizontalSize;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            target.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        }

        private static void DisablePreviewBehaviour(GameObject target)
        {
            foreach (MonoBehaviour behaviour in target.GetComponentsInChildren<MonoBehaviour>(true))
            {
                behaviour.enabled = false;
            }

            foreach (Collider collider in target.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (Rigidbody body in target.GetComponentsInChildren<Rigidbody>(true))
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
