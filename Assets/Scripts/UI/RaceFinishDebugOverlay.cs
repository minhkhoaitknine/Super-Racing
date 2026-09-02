using SuperRacing.Race;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class RaceFinishDebugOverlay : MonoBehaviour
    {
        private const string RaceSceneName = "Race";
        private const string DebugEnabledKey = "super_racing.finish_debug_enabled";

        private static readonly Color OkColor = new(0.0f, 1.0f, 0.38f, 1f);
        private static readonly Color NotOkColor = new(1.0f, 0.12f, 0.08f, 1f);
        private static readonly Color WarningColor = new(1.0f, 0.82f, 0.0f, 1f);

        private RaceManager raceManager;
        private Text statusLabel;

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
            if (!scene.IsValid() ||
                scene.name != RaceSceneName ||
                PlayerPrefs.GetInt(DebugEnabledKey, 0) == 0 ||
                FindFirstObjectByType<RaceFinishDebugOverlay>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                return;
            }

            GameObject overlayObject = new("Race Finish Debug Overlay");
            overlayObject.transform.SetParent(canvas.transform, false);
            overlayObject.AddComponent<RaceFinishDebugOverlay>();
        }

        private void Awake()
        {
            raceManager = FindFirstObjectByType<RaceManager>(FindObjectsInactive.Include);
            BuildUi();
        }

        private void Update()
        {
            if (raceManager == null)
            {
                raceManager = FindFirstObjectByType<RaceManager>(FindObjectsInactive.Include);
            }

            if (raceManager == null)
            {
                SetStatus("FINISH DETECT: NO MANAGER", WarningColor);
                return;
            }

            if (!raceManager.HasFinishLine)
            {
                SetStatus("FINISH DETECT: NOT FOUND", WarningColor);
                return;
            }

            SetStatus(raceManager.IsTouchingFinishLine ? "FINISH DETECT: OK" : "FINISH DETECT: NOT OK",
                raceManager.IsTouchingFinishLine ? OkColor : NotOkColor);
        }

        private void BuildUi()
        {
            Font font = ResolveFont();

            RectTransform root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -18f);
            root.sizeDelta = new Vector2(440f, 48f);

            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.05f, 0.07f, 0.82f);

            GameObject labelObject = new("Status");
            labelObject.transform.SetParent(transform, false);
            statusLabel = labelObject.AddComponent<Text>();
            statusLabel.font = font;
            statusLabel.fontSize = 24;
            statusLabel.fontStyle = FontStyle.Bold;
            statusLabel.alignment = TextAnchor.MiddleCenter;
            statusLabel.raycastTarget = false;

            RectTransform labelRect = statusLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);

            SetStatus("FINISH DETECT: NOT OK", NotOkColor);
        }

        private void SetStatus(string text, Color color)
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = text;
            statusLabel.color = color;
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
