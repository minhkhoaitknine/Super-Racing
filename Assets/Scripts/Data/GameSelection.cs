namespace SuperRacing.Data
{
    public static class GameSelection
    {
        public static CarDefinition SelectedCar { get; private set; }
        public static TrackDefinition SelectedTrack { get; private set; }

        public static bool HasCar => SelectedCar != null;
        public static bool HasTrack => SelectedTrack != null;
        public static bool IsReadyToRace => HasCar && HasTrack;

        public static void SelectCar(CarDefinition car)
        {
            SelectedCar = car;
        }

        public static void SelectTrack(TrackDefinition track)
        {
            SelectedTrack = track;
        }

        public static void Clear()
        {
            SelectedCar = null;
            SelectedTrack = null;
        }
    }
}
