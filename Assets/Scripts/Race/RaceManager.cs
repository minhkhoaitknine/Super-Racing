using System;
using System.Collections;
using SuperRacing.Contracts;
using SuperRacing.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SuperRacing.Race
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class RaceManager : MonoBehaviour
    {
        public enum RaceState
        {
            Ready,
            Countdown,
            Racing,
            Finished
        }

        [Serializable]
        public sealed class CountdownEvent : UnityEvent<int> { }

        [Serializable]
        public sealed class RaceFinishedEvent : UnityEvent<float, bool> { }

        [Header("Race Data")]
        [SerializeField] private TrackDefinition track;
        [SerializeField] private CarDefinition car;

        [Header("Scene References")]
        [SerializeField] private LapTracker lapTracker;
        [SerializeField] private RaceTimer raceTimer;
        [Tooltip("Must implement IVehicleController.")]
        [SerializeField] private MonoBehaviour vehicleController;

        [Header("Countdown")]
        [Min(1)] [SerializeField] private int countdownFrom = 3;
        [Min(0.1f)] [SerializeField] private float countdownStepSeconds = 1f;
        [SerializeField] private bool startAutomatically = true;

        [Header("Runtime Events")]
        [SerializeField] private CountdownEvent onCountdownTick = new();
        [SerializeField] private UnityEvent onRaceStarted = new();
        [SerializeField] private RaceFinishedEvent onRaceFinished = new();

        private IVehicleController vehicle;
        private Coroutine countdownRoutine;

        public RaceState State { get; private set; } = RaceState.Ready;
        public float FinalTimeSeconds { get; private set; }
        public bool SetNewRecord { get; private set; }
        public TrackDefinition Track => track;
        public CarDefinition Car => car;

        public event Action<int> CountdownTick;
        public event Action RaceStarted;
        public event Action<float, bool> RaceFinished;

        private void Awake()
        {
            track = GameSelection.SelectedTrack != null ? GameSelection.SelectedTrack : track;
            car = GameSelection.SelectedCar != null ? GameSelection.SelectedCar : car;

            LoadSelectedTrack();
            LoadSelectedVehicle();
            vehicle = vehicleController as IVehicleController;
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            lapTracker.RaceCompleted += FinishRace;
            vehicle.ApplyStats(car);
            vehicle.CanDrive = false;
            raceTimer.ResetTimer();
        }

        private void LoadSelectedTrack()
        {
            if (track == null)
            {
                return;
            }

            string selectedId = track.TrackId.ToLowerInvariant();
            foreach (GameObject rootObject in gameObject.scene.GetRootGameObjects())
            {
                Transform root = rootObject.transform;
                string rootName = root.name.ToLowerInvariant();
                if (!rootName.Contains("map_audit") && !rootName.Contains("map_physicsprototype"))
                {
                    continue;
                }

                bool belongsToSelectedTrack = rootName.Contains(selectedId);
                root.gameObject.SetActive(belongsToSelectedTrack);
            }
        }

        private void LoadSelectedVehicle()
        {
            if (car == null || car.VehiclePrefab == null)
            {
                return;
            }

            MonoBehaviour previousController = vehicleController;
            Transform spawn = previousController != null ? previousController.transform : null;
            Vector3 position = spawn != null ? spawn.position : Vector3.zero;
            Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;

            GameObject selectedVehicle = Instantiate(car.VehiclePrefab, position, rotation);
            selectedVehicle.name = car.VehiclePrefab.name;

            MonoBehaviour selectedController = null;
            foreach (MonoBehaviour component in selectedVehicle.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component is IVehicleController)
                {
                    selectedController = component;
                    break;
                }
            }

            if (selectedController == null)
            {
                Debug.LogError($"Selected car prefab '{car.VehiclePrefab.name}' does not contain an IVehicleController.", car.VehiclePrefab);
                Destroy(selectedVehicle);
                return;
            }

            LapTracker selectedLapTracker = selectedVehicle.GetComponent<LapTracker>();
            if (selectedLapTracker == null)
            {
                selectedLapTracker = selectedVehicle.AddComponent<LapTracker>();
            }

            vehicleController = selectedController;
            lapTracker = selectedLapTracker;

            RaceSetup setup = FindFirstObjectByType<RaceSetup>(FindObjectsInactive.Include);
            setup?.Configure(track, selectedLapTracker);

            foreach (SuperRacing.UI.RaceHUD hud in FindObjectsByType<SuperRacing.UI.RaceHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                hud.Configure(selectedController, selectedLapTracker, raceTimer);
            }

            if (previousController != null && previousController.gameObject != selectedVehicle)
            {
                previousController.gameObject.SetActive(false);
                Destroy(previousController.gameObject);
            }
        }

        private void Start()
        {
            if (startAutomatically)
            {
                BeginCountdown();
            }
        }

        private void OnDestroy()
        {
            if (lapTracker != null)
            {
                lapTracker.RaceCompleted -= FinishRace;
            }
        }

        public void BeginCountdown()
        {
            if (!enabled || State == RaceState.Countdown || State == RaceState.Racing)
            {
                return;
            }

            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
            }

            countdownRoutine = StartCoroutine(RunCountdown());
        }

        public void RestartRace()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        public void ReturnToMainMenu(string sceneName = "MainMenu")
        {
            SceneManager.LoadScene(sceneName);
        }

        public bool ValidateConfiguration()
        {
            if (track == null || car == null || lapTracker == null || raceTimer == null)
            {
                Debug.LogError("RaceManager requires track, car, LapTracker and RaceTimer references.", this);
                return false;
            }

            if (vehicle == null)
            {
                Debug.LogError("RaceManager vehicleController must implement IVehicleController.", this);
                return false;
            }

            return true;
        }

        private IEnumerator RunCountdown()
        {
            State = RaceState.Countdown;
            vehicle.CanDrive = false;
            lapTracker.CanAcceptCheckpoints = false;
            raceTimer.ResetTimer();

            WaitForSecondsRealtime wait = new(countdownStepSeconds);
            for (int value = countdownFrom; value > 0; value--)
            {
                CountdownTick?.Invoke(value);
                onCountdownTick.Invoke(value);
                yield return wait;
            }

            countdownRoutine = null;
            State = RaceState.Racing;
            lapTracker.ResetProgress();
            vehicle.CanDrive = true;
            raceTimer.StartTimer();
            RaceStarted?.Invoke();
            onRaceStarted.Invoke();
        }

        private void FinishRace()
        {
            if (State != RaceState.Racing)
            {
                return;
            }

            State = RaceState.Finished;
            vehicle.CanDrive = false;
            raceTimer.StopTimer();
            FinalTimeSeconds = raceTimer.ElapsedSeconds;
            SetNewRecord = RecordManager.TrySaveBestTime(track, car, FinalTimeSeconds);
            RaceFinished?.Invoke(FinalTimeSeconds, SetNewRecord);
            onRaceFinished.Invoke(FinalTimeSeconds, SetNewRecord);
        }
    }
}
