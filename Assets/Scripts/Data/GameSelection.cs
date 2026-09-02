namespace SuperRacing.Data
{
    public static class GameSelection
    {
        private const string SelectedCarIdKey = "super_racing.selected_car_id";
        private const string SelectedTrackIdKey = "super_racing.selected_track_id";

        public static CarDefinition SelectedCar { get; private set; }
        public static TrackDefinition SelectedTrack { get; private set; }

        public static bool HasCar => SelectedCar != null;
        public static bool HasTrack => SelectedTrack != null;
        public static bool IsReadyToRace => HasCar && HasTrack;

        public static void SelectCar(CarDefinition car)
        {
            SelectedCar = car;

            if (car != null)
            {
                UnityEngine.PlayerPrefs.SetString(SelectedCarIdKey, car.CarId);
                UnityEngine.PlayerPrefs.Save();
            }
        }

        public static void SelectTrack(TrackDefinition track)
        {
            SelectedTrack = track;

            if (track != null)
            {
                UnityEngine.PlayerPrefs.SetString(SelectedTrackIdKey, track.TrackId);
                UnityEngine.PlayerPrefs.Save();
            }
        }

        public static void RestoreFromCatalog(GameCatalog catalog)
        {
            if (catalog == null)
            {
                return;
            }

            if (SelectedCar == null && UnityEngine.PlayerPrefs.HasKey(SelectedCarIdKey))
            {
                string selectedCarId = UnityEngine.PlayerPrefs.GetString(SelectedCarIdKey);
                for (int index = 0; index < catalog.Cars.Count; index++)
                {
                    CarDefinition candidate = catalog.Cars[index];
                    if (candidate != null && candidate.CarId == selectedCarId)
                    {
                        SelectedCar = candidate;
                        break;
                    }
                }
            }

            if (SelectedTrack == null && UnityEngine.PlayerPrefs.HasKey(SelectedTrackIdKey))
            {
                string selectedTrackId = UnityEngine.PlayerPrefs.GetString(SelectedTrackIdKey);
                for (int index = 0; index < catalog.Tracks.Count; index++)
                {
                    TrackDefinition candidate = catalog.Tracks[index];
                    if (candidate != null && candidate.TrackId == selectedTrackId)
                    {
                        SelectedTrack = candidate;
                        break;
                    }
                }
            }
        }

        public static void Clear()
        {
            SelectedCar = null;
            SelectedTrack = null;
            UnityEngine.PlayerPrefs.DeleteKey(SelectedCarIdKey);
            UnityEngine.PlayerPrefs.DeleteKey(SelectedTrackIdKey);
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
