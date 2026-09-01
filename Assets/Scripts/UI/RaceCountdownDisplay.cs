using System.Collections;
using SuperRacing.Race;
using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class RaceCountdownDisplay : MonoBehaviour
    {
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private Text countdownLabel;

        private void Awake()
        {
            if (raceManager == null || countdownLabel == null)
            {
                enabled = false;
                return;
            }

            countdownLabel.text = "";
            raceManager.CountdownTick += OnCountdownTick;
            raceManager.RaceStarted   += OnRaceStarted;
            raceManager.RaceFinished  += OnRaceFinished;
        }

        private void OnDestroy()
        {
            if (raceManager == null) return;
            raceManager.CountdownTick -= OnCountdownTick;
            raceManager.RaceStarted   -= OnRaceStarted;
            raceManager.RaceFinished  -= OnRaceFinished;
        }

        private void OnCountdownTick(int value)
        {
            countdownLabel.text = value.ToString();
            StopAllCoroutines();
            StartCoroutine(FadeOut());
        }

        private void OnRaceStarted()
        {
            countdownLabel.text = "GO!";
            StopAllCoroutines();
            StartCoroutine(FadeOut());
        }

        private void OnRaceFinished(float time, bool newRecord)
        {
            StopAllCoroutines();
            countdownLabel.fontSize = 60;
            countdownLabel.text = newRecord
                ? $"FINISHED!\nNEW RECORD!\n{RaceHUD.FormatTime(time)}"
                : $"FINISHED!\n{RaceHUD.FormatTime(time)}";
        }

        private IEnumerator FadeOut()
        {
            yield return new WaitForSeconds(0.7f);
            float elapsed = 0f;
            var startColor = countdownLabel.color;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                countdownLabel.color = new Color(startColor.r, startColor.g, startColor.b, 1f - elapsed / 0.3f);
                yield return null;
            }
            countdownLabel.text = "";
            countdownLabel.color = startColor;
        }
    }
}
