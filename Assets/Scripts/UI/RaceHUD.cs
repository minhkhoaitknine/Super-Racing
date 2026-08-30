using SuperRacing.Contracts;
using SuperRacing.Race;
using UnityEngine;
using UnityEngine.UI;

namespace SuperRacing.UI
{
    [DisallowMultipleComponent]
    public sealed class RaceHUD : MonoBehaviour
    {
        [Header("Data Sources")]
        [Tooltip("Must implement IVehicleController.")]
        [SerializeField] private MonoBehaviour vehicleController;
        [SerializeField] private LapTracker lapTracker;
        [SerializeField] private RaceTimer raceTimer;

        [Header("Labels")]
        [SerializeField] private Text speedLabel;
        [SerializeField] private Text lapLabel;
        [SerializeField] private Text timeLabel;

        private IVehicleController vehicle;

        private void Awake()
        {
            vehicle = vehicleController as IVehicleController;
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            UpdateSpeed();
            UpdateLap(lapTracker.CurrentLap, lapTracker.TotalLaps);
            UpdateTime(raceTimer.ElapsedSeconds);
        }

        private void OnEnable()
        {
            if (lapTracker != null)
            {
                lapTracker.LapChanged += UpdateLap;
            }

            if (raceTimer != null)
            {
                raceTimer.TimeChanged += UpdateTime;
            }
        }

        private void OnDisable()
        {
            if (lapTracker != null)
            {
                lapTracker.LapChanged -= UpdateLap;
            }

            if (raceTimer != null)
            {
                raceTimer.TimeChanged -= UpdateTime;
            }
        }

        private void Update()
        {
            UpdateSpeed();
        }

        public bool ValidateConfiguration()
        {
            if (vehicle == null || lapTracker == null || raceTimer == null)
            {
                Debug.LogError("RaceHUD requires a vehicle implementing IVehicleController, LapTracker and RaceTimer.", this);
                return false;
            }

            if (speedLabel == null || lapLabel == null || timeLabel == null)
            {
                Debug.LogError("RaceHUD requires speed, lap and time labels.", this);
                return false;
            }

            return true;
        }

        public static string FormatTime(float elapsedSeconds)
        {
            elapsedSeconds = Mathf.Max(0f, elapsedSeconds);
            int minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
            float seconds = elapsedSeconds - minutes * 60f;
            return $"{minutes:00}:{seconds:00.000}";
        }

        private void UpdateSpeed()
        {
            if (vehicle != null && speedLabel != null)
            {
                speedLabel.text = $"{Mathf.RoundToInt(Mathf.Abs(vehicle.SpeedKmh))} km/h";
            }
        }

        private void UpdateLap(int currentLap, int totalLaps)
        {
            if (lapLabel != null)
            {
                lapLabel.text = $"Lap {currentLap}/{totalLaps}";
            }
        }

        private void UpdateTime(float elapsedSeconds)
        {
            if (timeLabel != null)
            {
                timeLabel.text = FormatTime(elapsedSeconds);
            }
        }
    }
}
