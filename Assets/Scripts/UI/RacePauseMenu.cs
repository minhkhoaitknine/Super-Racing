using SuperRacing.Race;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class RacePauseMenu : MonoBehaviour
    {
        private const string RaceSceneName = "Race";
        private const string GarageSceneName = "Garage";

        private GameObject panel;
        private RaceManager raceManager;
        private bool isPaused;

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
            if (!scene.IsValid() || scene.name != RaceSceneName ||
                FindFirstObjectByType<RacePauseMenu>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                return;
            }

            GameObject menuObject = new("Race Pause Menu");
            menuObject.transform.SetParent(canvas.transform, false);
            menuObject.AddComponent<RacePauseMenu>();
        }

        private void Awake()
        {
            raceManager = FindFirstObjectByType<RaceManager>(FindObjectsInactive.Include);
            BuildUi();
            SetPaused(false);
        }

        private void Update()
        {
            if (WasPausePressed() && raceManager != null && raceManager.State != RaceManager.RaceState.Finished)
            {
                SetPaused(!isPaused);
            }
        }

        private void OnDestroy()
        {
            if (isPaused)
            {
                Time.timeScale = 1f;
            }
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (panel != null)
            {
                panel.SetActive(paused);
            }
        }

        private void ReturnToGarage()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GarageSceneName);
        }

        private static bool WasPausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private void BuildUi()
        {
            Font font = ResolveFont();

            RectTransform root = gameObject.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            panel = new GameObject("Pause Panel");
            panel.transform.SetParent(transform, false);
            Image backdrop = panel.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.58f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            GameObject card = new("Pause Window");
            card.transform.SetParent(panel.transform, false);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.02f, 0.10f, 0.14f, 0.94f);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(460f, 300f);

            Text title = CreateLabel("Title", card.transform, font, 42, TextAnchor.MiddleCenter);
            title.text = "PAUSED";
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(380f, 70f));

            Button continueButton = CreateButton("Continue Button", card.transform, font, "CONTINUE");
            SetRect(continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(280f, 58f));
            continueButton.onClick.AddListener(() => SetPaused(false));

            Button garageButton = CreateButton("Garage Button", card.transform, font, "GARAGE");
            SetRect(garageButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(280f, 58f));
            garageButton.onClick.AddListener(ReturnToGarage);
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

        private static Button CreateButton(string name, Transform parent, Font font, string label)
        {
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.0f, 0.78f, 0.92f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.18f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.0f, 0.55f, 0.7f, 1f);
            button.colors = colors;

            Text text = CreateLabel("Label", buttonObject.transform, font, 22, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = new Color(0.01f, 0.05f, 0.08f, 1f);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
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
