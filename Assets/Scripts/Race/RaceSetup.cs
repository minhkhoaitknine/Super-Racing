using System;
using System.Collections.Generic;
using SuperRacing.Data;
using UnityEngine;

namespace SuperRacing.Race
{
    [DisallowMultipleComponent]
    public sealed class RaceSetup : MonoBehaviour
    {
        [SerializeField] private TrackDefinition track;
        [SerializeField] private LapTracker playerLapTracker;
        [SerializeField] private List<Checkpoint> checkpoints = new();
        [SerializeField] private bool discoverCheckpointsOnAwake = true;

        public IReadOnlyList<Checkpoint> Checkpoints => checkpoints;

        public void Configure(TrackDefinition selectedTrack, LapTracker selectedLapTracker)
        {
            track = selectedTrack;
            playerLapTracker = selectedLapTracker;
        }

        private void Awake()
        {
            track = GameSelection.SelectedTrack != null ? GameSelection.SelectedTrack : track;
            if (discoverCheckpointsOnAwake || checkpoints.Count == 0)
            {
                DiscoverCheckpoints();
            }

            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            int laps = track != null ? track.LapCount : 1;
            playerLapTracker.Initialize(checkpoints.Count, laps);
        }

        [ContextMenu("Discover Checkpoints")]
        public void DiscoverCheckpoints()
        {
            checkpoints.Clear();
            checkpoints.AddRange(FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            checkpoints.Sort((left, right) => left.CheckpointIndex.CompareTo(right.CheckpointIndex));
        }

        public bool ValidateConfiguration()
        {
            if (playerLapTracker == null)
            {
                Debug.LogError("RaceSetup requires a player LapTracker.", this);
                return false;
            }

            if (checkpoints.Count == 0)
            {
                Debug.LogError("RaceSetup requires at least one Checkpoint.", this);
                return false;
            }

            for (int index = 0; index < checkpoints.Count; index++)
            {
                if (checkpoints[index] == null || checkpoints[index].CheckpointIndex != index)
                {
                    Debug.LogError($"Checkpoint indices must be unique and contiguous from 0. Problem at list position {index}.", this);
                    return false;
                }
            }

            return true;
        }
    }
}
