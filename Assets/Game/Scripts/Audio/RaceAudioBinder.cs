using SuperRacing.Race;
using UnityEngine;

namespace SuperRacing.Audio
{
    [DisallowMultipleComponent]
    public sealed class RaceAudioBinder : MonoBehaviour
    {
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private LapTracker lapTracker;
        [SerializeField] private GameAudioManager audioManager;

        private void Start()
        {
            if (raceManager == null) raceManager = FindFirstObjectByType<RaceManager>();
            if (lapTracker == null) lapTracker = FindFirstObjectByType<LapTracker>();
            if (audioManager == null) audioManager = GameAudioManager.Instance;
            if (raceManager == null || lapTracker == null || audioManager == null) return;

            raceManager.CountdownTick += OnCountdown;
            raceManager.RaceStarted += OnStarted;
            raceManager.RaceFinished += OnFinished;
            lapTracker.CheckpointPassed += OnCheckpoint;
            lapTracker.LapChanged += OnLapChanged;
        }

        private void OnDestroy()
        {
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
        }

        private void OnCountdown(int _) => Play(audioManager.Catalog.countdownTick);
        private void OnStarted()
        {
            audioManager.ApplySnapshot(AudioSnapshotId.Default, .15f);
            Play(audioManager.Catalog.startedGo);
            audioManager.PlayRaceMusic();
        }
        private void OnCheckpoint(int _) => Play(audioManager.Catalog.checkpointPassed, 0.65f);
        private void OnLapChanged(int current, int total)
        {
            if (current > 1 && current <= total) Play(audioManager.Catalog.lapChanged);
        }
        private void OnFinished(float _, bool record)
        {
            audioManager.ApplySnapshot(AudioSnapshotId.Finish, .35f);
            Play(record ? audioManager.Catalog.newRecord : audioManager.Catalog.finished);
        }
        private void Play(AudioClip clip, float volume = 1f)
        {
            if (clip == null || audioManager.Catalog == null) return;
            AudioCueId cue = clip == audioManager.Catalog.countdownTick ? AudioCueId.CountdownTick : clip == audioManager.Catalog.startedGo ? AudioCueId.StartedGo :
                clip == audioManager.Catalog.checkpointPassed ? AudioCueId.CheckpointPassed : clip == audioManager.Catalog.lapChanged ? AudioCueId.LapChanged :
                clip == audioManager.Catalog.newRecord ? AudioCueId.NewRecord : AudioCueId.Finished;
            audioManager.PlayCue(cue, null, volume);
        }
    }
}
