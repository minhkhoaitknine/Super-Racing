using System.Collections;
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
    public sealed class RacePanels : MonoBehaviour
    {
        [Header("Race")]
        [SerializeField] private RaceManager raceManager;

        [Header("Countdown")]
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private Text countdownLabel;

        [Header("Finish")]
        [SerializeField] private GameObject finishPanel;
        [SerializeField] private Text finalTimeLabel;
        [SerializeField] private Text recordLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string completeRaceSceneName = "complete_race";
        [SerializeField, Min(0.1f)] private float finishBlinkSpeed = 2.5f;

        private Coroutine hideCountdownRoutine;
        private Text continuePromptLabel;
        private bool waitingForFinishDismiss;
        private Color finalTimeBaseColor = Color.white;
        private Color recordBaseColor = Color.white;

        private void Awake()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            countdownPanel.SetActive(false);
            finishPanel.SetActive(false);
            finalTimeBaseColor = finalTimeLabel.color;
            recordBaseColor = recordLabel.color;
            continuePromptLabel = CreateContinuePromptLabel();

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (raceManager == null)
            {
                return;
            }

            raceManager.CountdownTick += ShowCountdown;
            raceManager.RaceStarted += HideCountdown;
            raceManager.RaceFinished += ShowFinish;
        }

        private void OnDisable()
        {
            if (raceManager == null)
            {
                return;
            }

            raceManager.CountdownTick -= ShowCountdown;
            raceManager.RaceStarted -= HideCountdown;
            raceManager.RaceFinished -= ShowFinish;
        }

        private void Update()
        {
            if (!waitingForFinishDismiss)
            {
                return;
            }

            UpdateFinishBlink();

            if (WasDismissPressed())
            {
                waitingForFinishDismiss = false;
                Time.timeScale = 1f;
                SceneManager.LoadScene(completeRaceSceneName);
            }
        }

        public bool ValidateConfiguration()
        {
            if (raceManager == null || countdownPanel == null || countdownLabel == null ||
                finishPanel == null || finalTimeLabel == null || recordLabel == null ||
                restartButton == null || mainMenuButton == null)
            {
                Debug.LogError("RacePanels has one or more missing references.", this);
                return false;
            }

            return true;
        }

        private void ShowCountdown(int value)
        {
            if (hideCountdownRoutine != null)
            {
                StopCoroutine(hideCountdownRoutine);
                hideCountdownRoutine = null;
            }

            finishPanel.SetActive(false);
            countdownPanel.SetActive(true);
            countdownLabel.text = value.ToString();
        }

        private void HideCountdown()
        {
            countdownLabel.text = "GO!";
            countdownPanel.SetActive(true);
            hideCountdownRoutine = StartCoroutine(HideCountdownAfterDelay());
        }

        private void ShowFinish(float finalTimeSeconds, bool setNewRecord)
        {
            countdownPanel.SetActive(false);
            finishPanel.SetActive(true);
            finalTimeLabel.text = "COMPLETE";
            recordLabel.text = $"TIME  {RaceHUD.FormatTime(finalTimeSeconds)}";
            continuePromptLabel.text = "Press any key or tap anywhere to continue";
            ResetFinishBlink();

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(false);
            }

            waitingForFinishDismiss = true;
        }

        private void ReturnToMainMenu()
        {
            raceManager.ReturnToMainMenu(mainMenuSceneName);
        }

        private IEnumerator HideCountdownAfterDelay()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            countdownPanel.SetActive(false);
            hideCountdownRoutine = null;
        }

        private void UpdateFinishBlink()
        {
            float alpha = 0.35f + Mathf.PingPong(Time.unscaledTime * finishBlinkSpeed, 0.65f);
            finalTimeLabel.color = WithAlpha(finalTimeBaseColor, alpha);
            recordLabel.color = WithAlpha(recordBaseColor, alpha);
        }

        private void ResetFinishBlink()
        {
            finalTimeLabel.color = finalTimeBaseColor;
            recordLabel.color = recordBaseColor;
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

        private Text CreateContinuePromptLabel()
        {
            GameObject promptObject = new("Continue Prompt");
            promptObject.transform.SetParent(finishPanel.transform, false);

            Text prompt = promptObject.AddComponent<Text>();
            prompt.font = finalTimeLabel.font;
            prompt.fontSize = 20;
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.color = new Color(0.75f, 0.95f, 1f, 1f);
            prompt.raycastTarget = false;
            prompt.text = "Press any key or tap anywhere to continue";

            RectTransform rect = prompt.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 42f);
            rect.sizeDelta = new Vector2(500f, 36f);

            return prompt;
        }
    }
}
