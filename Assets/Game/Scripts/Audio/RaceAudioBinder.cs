using System.Collections;
using SuperRacing.Race;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperRacing.Audio
{
    [DisallowMultipleComponent]
    public sealed class RaceAudioBinder : MonoBehaviour
    {
        private RaceManager raceManager;
        private LapTracker lapTracker;
        private GameAudioManager audioManager;
        private bool countdownSnapshotApplied;
        private bool finishHandled;
        private Coroutine restoreAfterGoRoutine;

        public bool IsBound => raceManager != null && lapTracker != null;

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start() => BindCurrentScene();
        private void OnSceneLoaded(Scene _, LoadSceneMode __) => BindCurrentScene();

        private void BindCurrentScene()
        {
            Unbind();
            audioManager = GameAudioManager.Instance;
            raceManager = FindFirstObjectByType<RaceManager>();
            lapTracker = FindFirstObjectByType<LapTracker>();
            countdownSnapshotApplied = false;
            finishHandled = false;
            if (raceManager == null || lapTracker == null || audioManager == null) return;

            raceManager.CountdownTick += OnCountdown;
            raceManager.RaceStarted += OnStarted;
            raceManager.RaceFinished += OnFinished;
            lapTracker.CheckpointPassed += OnCheckpoint;
            lapTracker.LapChanged += OnLapChanged;
        }

        private void Unbind()
        {
            if (restoreAfterGoRoutine != null)
            {
                StopCoroutine(restoreAfterGoRoutine);
                restoreAfterGoRoutine = null;
            }
            if (raceManager != null)
            {
                raceManager.CountdownTick -= OnCountdown;
                raceManager.RaceStarted -= OnStarted;
                raceManager.RaceFinished -= OnFinished;
            }
            if (lapTracker != null)
            {
                lapTracker.CheckpointPassed -= OnCheckpoint;
                lapTracker.LapChanged -= OnLapChanged;
            }
            raceManager = null;
            lapTracker = null;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Unbind();
        }

        private void OnCountdown(int _)
        {
            if (!countdownSnapshotApplied)
            {
                audioManager.ApplySnapshot(AudioSnapshotId.Countdown, .2f);
                countdownSnapshotApplied = true;
            }
            audioManager.PlayCue(AudioCueId.CountdownTick);
        }

        private void OnStarted()
        {
            audioManager.PlayCue(AudioCueId.StartedGo);
            audioManager.PlayRaceMusic();
            if (restoreAfterGoRoutine != null) StopCoroutine(restoreAfterGoRoutine);
            restoreAfterGoRoutine = StartCoroutine(RestoreDefaultAfterGo());
        }

        private IEnumerator RestoreDefaultAfterGo()
        {
            // Keep the countdown music duck active while the spoken GO is playing.
            float voiceLength = audioManager.Catalog?.startedGo != null ? audioManager.Catalog.startedGo.length : .5f;
            yield return new WaitForSecondsRealtime(Mathf.Clamp(voiceLength + .05f, .35f, .85f));
            audioManager.ApplySnapshot(AudioSnapshotId.Default, .15f);
            restoreAfterGoRoutine = null;
        }

        private void OnCheckpoint(int _) => audioManager.PlayCue(AudioCueId.CheckpointPassed, null, .65f);
        private void OnLapChanged(int current, int total)
        {
            if (current > 1 && current <= total) audioManager.PlayCue(AudioCueId.LapChanged);
        }

        private void OnFinished(float _, bool record)
        {
            if (finishHandled) return;
            finishHandled = true;
            audioManager.ApplySnapshot(AudioSnapshotId.Finish, .35f);
            AudioClip sting = record ? audioManager.Catalog?.newRecord : audioManager.Catalog?.finished;
            audioManager.PlayCue(record ? AudioCueId.NewRecord : AudioCueId.Finished);
            StartCoroutine(PlayResultAfterSting(sting != null ? sting.length : .5f));
        }

        private IEnumerator PlayResultAfterSting(float delay)
        {
            yield return new WaitForSecondsRealtime(Mathf.Clamp(delay, .25f, 2.5f));
            audioManager.PlayCue(AudioCueId.UIResultsOpen, null, .7f);
            audioManager.PlayResultMusic();
        }
    }
}
