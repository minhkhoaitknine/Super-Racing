using SuperRacing.Race;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioSettingsRuntimePresenter : MonoBehaviour
    {
        private Button launcher;
        private Button runtimeSettingsButton;
        private AudioSettingsPanel panel;
        private Canvas targetCanvas;
        private bool pausesGame;
        private bool externalPauseSnapshot;
        private bool raceSceneActive;
        private float previousTimeScale = 1f;
        private int sceneRevision;

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start() => ConfigureScene(SceneManager.GetActiveScene());
        private void OnSceneLoaded(Scene scene, LoadSceneMode _) => ConfigureScene(scene);

        private void Update()
        {
            if (!raceSceneActive || pausesGame || GameAudioManager.Instance == null) return;
            bool pausedExternally = Time.timeScale <= .0001f;
            if (pausedExternally && !externalPauseSnapshot)
            {
                GameAudioManager.Instance.PushSnapshot(AudioSnapshotId.Paused, .18f);
                externalPauseSnapshot = true;
            }
            else if (!pausedExternally && externalPauseSnapshot)
            {
                GameAudioManager.Instance.PopSnapshot(.18f);
                externalPauseSnapshot = false;
            }
        }

        private void ConfigureScene(Scene scene)
        {
            Close(false);
            if (launcher != null) launcher.onClick.RemoveListener(OpenFromMenu);
            launcher = null;
            runtimeSettingsButton = null;
            panel = null;
            externalPauseSnapshot = false;
            string sceneName = scene.name.ToLowerInvariant();
            raceSceneActive = IsRaceScene(scene.name) && !sceneName.Contains("sandbox");
            targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null && IsRaceScene(scene.name)) targetCanvas = CreateRuntimeCanvas(scene);
            if (targetCanvas == null) return;
            if (sceneName.Contains("garage"))
            {
                foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (button.name.ToLowerInvariant() != "settings") continue;
                    launcher = button;
                    launcher.onClick.AddListener(OpenFromMenu);
                    break;
                }
            }
            else if (raceSceneActive)
            {
                int revision = ++sceneRevision;
                StartCoroutine(AttachToRacePauseMenu(scene, revision));
            }
        }

        public void OpenFromMenu() => Open(false);
        public void OpenFromRace() => Open(true);

        private void Open(bool pause)
        {
            if (targetCanvas == null || GameAudioManager.Instance?.Catalog?.audioSettingsPrefab == null) return;
            EnsurePanel();
            if (panel == null) return;
            pausesGame = pause && Time.timeScale > .0001f;
            if (pausesGame)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                GameAudioManager.Instance.PushSnapshot(AudioSnapshotId.Paused, .2f);
            }
            panel.gameObject.SetActive(true);
            panel.Refresh();
        }

        public void Close() => Close(true);
        private void Close(bool restoreSnapshot)
        {
            if (panel != null) panel.gameObject.SetActive(false);
            if (pausesGame)
            {
                Time.timeScale = previousTimeScale;
                if (restoreSnapshot) GameAudioManager.Instance?.PopSnapshot(.2f);
            }
            pausesGame = false;
            GameAudioManager.Instance?.FlushSettings();
        }

        private void EnsurePanel()
        {
            if (panel != null) return;
            GameObject instance = Instantiate(GameAudioManager.Instance.Catalog.audioSettingsPrefab, targetCanvas.transform, false);
            instance.name = "Audio Settings (Runtime)";
            RectTransform rect = instance.GetComponent<RectTransform>();
            if (rect != null) { rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; }
            panel = instance.GetComponent<AudioSettingsPanel>();
            if (panel != null) panel.CloseRequested += Close;
        }

        private IEnumerator AttachToRacePauseMenu(Scene scene, int revision)
        {
            Transform pauseWindow = null;
            for (int attempt = 0; attempt < 10 && pauseWindow == null; attempt++)
            {
                yield return null;
                if (revision != sceneRevision || !scene.IsValid() || !scene.isLoaded) yield break;
                pauseWindow = FindSceneTransform(scene, "Pause Window");
            }

            if (pauseWindow != null)
            {
                PolishPauseWindow(pauseWindow);
                runtimeSettingsButton = CreateSettingsButton(pauseWindow, new Vector2(0f, -55f), new Vector2(280f, 58f));
            }
            else if (targetCanvas != null)
            {
                // Test/prototype race scenes do not own a pause menu. Keep a small,
                // audio-owned launcher so settings remain testable there.
                runtimeSettingsButton = CreateSettingsButton(targetCanvas.transform, new Vector2(-24f, -24f), new Vector2(190f, 46f));
                RectTransform rect = runtimeSettingsButton.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            }
            if (runtimeSettingsButton != null) runtimeSettingsButton.onClick.AddListener(OpenFromRace);
        }

        private static void PolishPauseWindow(Transform pauseWindow)
        {
            RectTransform card = pauseWindow as RectTransform;
            if (card != null) card.sizeDelta = new Vector2(500f, 390f);
            Image background = pauseWindow.GetComponent<Image>();
            if (background != null) background.color = new Color(.012f, .055f, .085f, .97f);
            Outline outline = pauseWindow.GetComponent<Outline>();
            if (outline == null) outline = pauseWindow.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, .78f, .92f, .55f);
            outline.effectDistance = new Vector2(2f, -2f);

            MoveChild(pauseWindow, "Title", new Vector2(0f, -62f));
            MoveChild(pauseWindow, "Continue Button", new Vector2(0f, 20f));
            MoveChild(pauseWindow, "Garage Button", new Vector2(0f, -130f));
        }

        private static void MoveChild(Transform parent, string name, Vector2 position)
        {
            Transform child = parent.Find(name);
            if (child is RectTransform rect) rect.anchoredPosition = position;
        }

        private static Button CreateSettingsButton(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject go = new("Audio Settings Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = new Color(.035f, .16f, .22f, 1f);
            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.65f, 1f, 1f, 1f);
            colors.pressedColor = new Color(.4f, .8f, .9f, 1f);
            button.colors = colors;
            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, .78f, .92f, .9f);
            outline.effectDistance = new Vector2(1f, -1f);

            GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(go.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            Text label = labelObject.GetComponent<Text>();
            label.text = "AUDIO SETTINGS";
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(.75f, .98f, 1f, 1f);
            UIButtonAudio audio = go.AddComponent<UIButtonAudio>();
            audio.EnableAutomaticClick(AudioCueId.UIClick);
            return button;
        }

        private static Transform FindSceneTransform(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    if (item.name == name) return item;
            return null;
        }

        private static Canvas CreateRuntimeCanvas(Scene scene)
        {
            GameObject root = new("Audio Settings Canvas (Runtime)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(root, scene);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new("Audio EventSystem (Runtime)", typeof(EventSystem), typeof(InputSystemUIInputModule));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
            }
            return canvas;
        }

        private static bool IsRaceScene(string sceneName)
        {
            string value = sceneName.ToLowerInvariant();
            if (value.Contains("complete_race") || value.Contains("completerace") || value.Contains("result")) return false;
            return value.Contains("race") || value.Contains("vehicle");
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (launcher != null) launcher.onClick.RemoveListener(OpenFromMenu);
            sceneRevision++;
            Close(false);
        }
    }
}
