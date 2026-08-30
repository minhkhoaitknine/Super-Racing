using System.Collections;
using SuperRacing.Race;
using UnityEngine;
using UnityEngine.UI;

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

        private Coroutine hideCountdownRoutine;

        private void Awake()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            countdownPanel.SetActive(false);
            finishPanel.SetActive(false);
            restartButton.onClick.AddListener(raceManager.RestartRace);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
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

        private void OnDestroy()
        {
            if (restartButton != null && raceManager != null)
            {
                restartButton.onClick.RemoveListener(raceManager.RestartRace);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
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
            finalTimeLabel.text = $"Time  {RaceHUD.FormatTime(finalTimeSeconds)}";
            recordLabel.text = setNewRecord ? "NEW RECORD!" : "Race Complete";
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
    }
}
