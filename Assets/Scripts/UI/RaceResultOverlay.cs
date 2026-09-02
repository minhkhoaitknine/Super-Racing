using SuperRacing.Race;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class RaceResultOverlay : MonoBehaviour
    {
        private const string RaceSceneName = "Race";
        private const string CompleteRaceSceneName = "complete_race";

        private RaceManager raceManager;
        private GameObject panel;
        private Text titleLabel;
        private Text timeLabel;
        private Text promptLabel;
        private bool subscribed;
        private bool waitingForDismiss;
        private Color titleBaseColor = Color.white;
        private Color timeBaseColor = Color.white;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryInstall(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstall(scene);
        }

        private static void TryInstall(Scene scene)
        {
            if (!scene.IsValid() || scene.name != RaceSceneName)
            {
                return;
            }

            if (FindFirstObjectByType<RacePanels>(FindObjectsInactive.Include) != null ||
                FindFirstObjectByType<RaceResultOverlay>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            RaceManager manager = FindFirstObjectByType<RaceManager>(FindObjectsInactive.Include);
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (manager == null || canvas == null)
            {
                return;
            }

            GameObject overlayObject = new("Race Result Overlay");
            overlayObject.transform.SetParent(canvas.transform, false);
            overlayObject.AddComponent<RaceResultOverlay>().Configure(manager);
        }

        private void Configure(RaceManager manager)
        {
            Unsubscribe();
            raceManager = manager;
            BuildUi();
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void BuildUi()
        {
            Font font = ResolveFont();

            RectTransform root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            panel = new GameObject("Finish Panel");
            panel.transform.SetParent(transform, false);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.08f, 0.12f, 0.92f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(560f, 280f);

            titleLabel = CreateLabel("Title", panel.transform, font, 42, TextAnchor.MiddleCenter);
            SetRect(titleLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(480f, 62f));

            timeLabel = CreateLabel("Final Time", panel.transform, font, 34, TextAnchor.MiddleCenter);
            SetRect(timeLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(480f, 52f));

            promptLabel = CreateLabel("Continue Prompt", panel.transform, font, 20, TextAnchor.MiddleCenter);
            promptLabel.color = new Color(0.75f, 0.95f, 1f, 1f);
            promptLabel.text = "Press any key or tap anywhere to continue";
            SetRect(promptLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -204f), new Vector2(500f, 36f));

            titleBaseColor = titleLabel.color;
            timeBaseColor = timeLabel.color;

            panel.SetActive(false);
        }

        private void Update()
        {
            if (!waitingForDismiss)
            {
                return;
            }

            UpdateBlink();

            if (WasDismissPressed())
            {
                waitingForDismiss = false;
                Time.timeScale = 1f;
                SceneManager.LoadScene(CompleteRaceSceneName);
            }
        }

        private void ShowFinish(float finalTimeSeconds, bool setNewRecord)
        {
            titleLabel.text = "COMPLETE";
            timeLabel.text = $"TIME  {RaceHUD.FormatTime(finalTimeSeconds)}";
            ResetBlink();
            panel.SetActive(true);
            waitingForDismiss = true;
        }

        private void UpdateBlink()
        {
            float alpha = 0.35f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.65f);
            titleLabel.color = WithAlpha(titleBaseColor, alpha);
            timeLabel.color = WithAlpha(timeBaseColor, alpha);
        }

        private void ResetBlink()
        {
            titleLabel.color = titleBaseColor;
            timeLabel.color = timeBaseColor;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static bool WasDismissPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                return true;
            }
#endif

            return false;
        }

        private void Subscribe()
        {
            if (subscribed || raceManager == null)
            {
                return;
            }

            raceManager.RaceFinished += ShowFinish;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || raceManager == null)
            {
                return;
            }

            raceManager.RaceFinished -= ShowFinish;
            subscribed = false;
        }

        private static Text CreateLabel(string name, Transform parent, Font font, int size, TextAnchor alignment)
        {
            GameObject labelObject = new(name);
            labelObject.transform.SetParent(parent, false);
            Text label = labelObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static Font ResolveFont()
        {
            Text existingText = FindFirstObjectByType<Text>(FindObjectsInactive.Include);
            if (existingText != null && existingText.font != null)
            {
                return existingText.font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
