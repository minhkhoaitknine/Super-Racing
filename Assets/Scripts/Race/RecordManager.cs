using SuperRacing.Data;
using UnityEngine;

namespace SuperRacing.Race
{
    public static class RecordManager
    {
        private const string KeyPrefix = "best_time";

        public static string BuildKey(string trackId, string carId)
        {
            return $"{KeyPrefix}_{trackId}_{carId}";
        }

        public static bool TryGetBestTime(string trackId, string carId, out float bestTime)
        {
            string key = BuildKey(trackId, carId);
            if (!PlayerPrefs.HasKey(key))
            {
                bestTime = 0f;
                return false;
            }

            bestTime = PlayerPrefs.GetFloat(key);
            return bestTime > 0f;
        }

        public static bool TrySaveBestTime(TrackDefinition track, CarDefinition car, float elapsedSeconds)
        {
            if (track == null || car == null)
            {
                Debug.LogError("A track and car are required to save a race record.");
                return false;
            }

            return TrySaveBestTime(track.TrackId, car.CarId, elapsedSeconds);
        }

        public static bool TrySaveBestTime(string trackId, string carId, float elapsedSeconds)
        {
            if (string.IsNullOrWhiteSpace(trackId) || string.IsNullOrWhiteSpace(carId) || elapsedSeconds <= 0f)
            {
                return false;
            }

            if (TryGetBestTime(trackId, carId, out float currentBest) && elapsedSeconds >= currentBest)
            {
                return false;
            }

            PlayerPrefs.SetFloat(BuildKey(trackId, carId), elapsedSeconds);
            PlayerPrefs.Save();
            return true;
        }

        public static void DeleteBestTime(string trackId, string carId)
        {
            PlayerPrefs.DeleteKey(BuildKey(trackId, carId));
            PlayerPrefs.Save();
        }
    }
}
