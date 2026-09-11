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
        private TrackPreviewRotator previewRotator;
        private int previousVSyncCount;
        private bool ownsVSync;

        private void OnEnable()
        {
            // Desktop software frame limiting can deliver uneven presentation intervals.
            // Match the monitor while this rotating preview is visible.
            if (!Application.isMobilePlatform)
            {
                previousVSyncCount = QualitySettings.vSyncCount;
                QualitySettings.vSyncCount = 1;
                ownsVSync = true;
            }
        }

        private void OnDisable()
        {
            if (ownsVSync)
            {
                QualitySettings.vSyncCount = previousVSyncCount;
                ownsVSync = false;
            }
        }

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
            Canvas canvas = trackNameLabel.GetComponentInParent<Canvas>();
            if (canvas != null)
                GlobalLeaderboardPanel.AddButton(canvas.transform, () => catalog.Tracks[selectedIndex],
                    new Vector2(0.5f, 0f), new Vector2(0f, 85f));
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
            lapCountLabel.text = track.LapCount == 1 ? "1 Lap" : $"{track.LapCount} Laps";

            if (GameSelection.HasCar && RecordManager.TryGetBestTime(track, GameSelection.SelectedCar, out float bestTime))
            {
                recordLabel.text = $"Best  {RaceHUD.FormatTime(bestTime)}";
            }
            else
            {
                recordLabel.text = "No Record";
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
            if (previewRotator == null && trackNameLabel != null)
            {
                Canvas canvas = trackNameLabel.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    foreach (TrackPreviewRotator rotator in canvas.GetComponentsInChildren<TrackPreviewRotator>(true))
                        if (rotator.Target == trackPreviewRoot) { previewRotator = rotator; break; }
                }
            }
            previewRotator?.SyncRotation();
        }

        private void FitPreview(GameObject target)
        {
            if (!TryGetPreviewBounds(target, out Bounds bounds))
            {
                return;
            }

            float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
            float scale = 1f;
            if (horizontalSize > 0.001f)
            {
                scale = previewTargetSize / horizontalSize;
                target.transform.localScale *= scale;
            }

            // Bounds and translation share the rotation pivot's coordinate system.
            Vector3 anchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            target.transform.localPosition = (target.transform.localPosition - anchor) * scale;
        }

        private bool TryGetPreviewBounds(GameObject target, out Bounds bounds)
        {
            bounds = default;
            bool hasPoint = false;
            var vertices = new List<Vector3>();
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                Matrix4x4 toPivot = trackPreviewRoot.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh != null && mesh.vertexCount == 0)
                    continue;

                // Transform actual vertices: rotating a mesh's bounding box includes
                // empty corners and can shift the apparent center of the whole map.
                if (mesh != null && mesh.isReadable)
                {
                    mesh.GetVertices(vertices);
                    foreach (Vector3 vertex in vertices)
                        IncludePreviewPoint(ref bounds, ref hasPoint, toPivot.MultiplyPoint3x4(vertex));
                }
                else
                {
                    Bounds local = renderer.localBounds;
                    if (local.size.sqrMagnitude < 0.000001f)
                        continue;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 point = local.center + Vector3.Scale(local.extents, new Vector3(
                            (corner & 1) == 0 ? -1f : 1f,
                            (corner & 2) == 0 ? -1f : 1f,
                            (corner & 4) == 0 ? -1f : 1f));
                        IncludePreviewPoint(ref bounds, ref hasPoint, toPivot.MultiplyPoint3x4(point));
                    }
                }
            }
            return hasPoint;
        }

        private static void IncludePreviewPoint(ref Bounds bounds, ref bool hasPoint, Vector3 point)
        {
            if (hasPoint)
                bounds.Encapsulate(point);
            else
            {
                bounds = new Bounds(point, Vector3.zero);
                hasPoint = true;
            }
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
