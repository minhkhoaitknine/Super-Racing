using System;
using UnityEngine;
using UnityEngine.Events;

namespace SuperRacing.Race
{
    [DisallowMultipleComponent]
    public sealed class LapTracker : MonoBehaviour
    {
        [Serializable]
        public sealed class LapProgressEvent : UnityEvent<int, int> { }

        [Header("Runtime Events")]
        [SerializeField] private LapProgressEvent onLapChanged = new();
        [SerializeField] private UnityEvent onRaceCompleted = new();

        private int checkpointCount;
        private int expectedCheckpointIndex;
        private int completedLaps;
        private int totalLaps = 1;

        public int CurrentLap => Mathf.Min(completedLaps + 1, totalLaps);
        public int TotalLaps => totalLaps;
        public int ExpectedCheckpointIndex => expectedCheckpointIndex;
        public int CompletedLaps => completedLaps;
        public bool IsRaceComplete { get; private set; }
        public bool CanAcceptCheckpoints { get; set; } = true;

        public event Action<int, int> LapChanged;
        public event Action RaceCompleted;

        public void Initialize(int numberOfCheckpoints, int numberOfLaps)
        {
            checkpointCount = Mathf.Max(0, numberOfCheckpoints);
            totalLaps = Mathf.Max(1, numberOfLaps);
            ResetProgress();
        }

        public void ResetProgress()
        {
            expectedCheckpointIndex = 0;
            completedLaps = 0;
            IsRaceComplete = false;
            CanAcceptCheckpoints = true;
            NotifyLapChanged();
        }

        public bool TryPassCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint == null || !CanAcceptCheckpoints || IsRaceComplete || checkpointCount == 0)
            {
                return false;
            }

            if (checkpoint.CheckpointIndex != expectedCheckpointIndex)
            {
                return false;
            }

            expectedCheckpointIndex++;
            if (expectedCheckpointIndex < checkpointCount)
            {
                return true;
            }

            expectedCheckpointIndex = 0;
            completedLaps++;

            if (completedLaps >= totalLaps)
            {
                IsRaceComplete = true;
                CanAcceptCheckpoints = false;
                RaceCompleted?.Invoke();
                onRaceCompleted.Invoke();
                return true;
            }

            NotifyLapChanged();
            return true;
        }

        private void NotifyLapChanged()
        {
            LapChanged?.Invoke(CurrentLap, totalLaps);
            onLapChanged.Invoke(CurrentLap, totalLaps);
        }
    }
}
