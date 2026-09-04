using SuperRacing.Data;
using UnityEngine;

namespace SuperRacing.Economy
{
    public static class RaceRewardCalculator
    {
        public static RaceRewardSummary Calculate(TrackDefinition track, bool setNewRecord, float cleanDriftSeconds)
        {
            int completion = track != null ? track.CompletionReward : 0;
            int record = setNewRecord && track != null ? track.NewRecordBonus : 0;
            int drift = track == null ? 0 : Mathf.Min(
                track.MaximumDriftReward,
                Mathf.FloorToInt(Mathf.Max(0f, cleanDriftSeconds) * track.DriftCoinsPerSecond));
            return new RaceRewardSummary(completion, record, drift);
        }
    }
}
