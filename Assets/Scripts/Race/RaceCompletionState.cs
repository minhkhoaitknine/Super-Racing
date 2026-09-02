using SuperRacing.Data;

namespace SuperRacing.Race
{
    public static class RaceCompletionState
    {
        public static float FinalTimeSeconds { get; private set; }
        public static bool SetNewRecord { get; private set; }
        public static string TrackName { get; private set; } = "Track";
        public static string CarName { get; private set; } = "Car";

        public static void Save(float finalTimeSeconds, bool setNewRecord, TrackDefinition track, CarDefinition car)
        {
            FinalTimeSeconds = finalTimeSeconds;
            SetNewRecord = setNewRecord;
            TrackName = track != null ? track.DisplayName : "Track";
            CarName = car != null ? car.DisplayName : "Car";
        }
    }
}
