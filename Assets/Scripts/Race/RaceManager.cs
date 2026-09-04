using System;
using System.Collections;
using SuperRacing.Contracts;
using SuperRacing.Data;
using SuperRacing.Economy;
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
        [SerializeField] private GameCatalog catalog;
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
        [Min(0f)] [SerializeField] private float minimumFinishSeconds = 8f;
        [SerializeField] private bool startAutomatically = true;

        [Header("Runtime Events")]
        [SerializeField] private CountdownEvent onCountdownTick = new();
        [SerializeField] private UnityEvent onRaceStarted = new();
        [SerializeField] private RaceFinishedEvent onRaceFinished = new();

        private IVehicleController vehicle;
        private Coroutine countdownRoutine;
        private Transform selectedTrackRoot;
        private Checkpoint finishLineCheckpoint;
        private Collider finishLineTrigger;
        private bool hasLeftFinishLine;
        private DriftRewardTracker driftRewardTracker;

        public RaceState State { get; private set; } = RaceState.Ready;
        public float FinalTimeSeconds { get; private set; }
        public bool SetNewRecord { get; private set; }
        public bool HasFinishLine => finishLineTrigger != null;
        public bool IsTouchingFinishLine { get; private set; }
        public TrackDefinition Track => track;
        public CarDefinition Car => car;

        public event Action<int> CountdownTick;
        public event Action RaceStarted;
        public event Action<float, bool> RaceFinished;

        private void Awake()
        {
            GameSelection.RestoreFromCatalog(catalog);
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

            DisableEmbeddedTrackRoots();

            if (track.PreviewPrefab != null)
            {
                GameObject selectedTrack = Instantiate(track.PreviewPrefab);
                selectedTrack.name = $"{track.TrackId}_RuntimeMap";
                selectedTrackRoot = selectedTrack.transform;
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
                if (belongsToSelectedTrack)
                {
                    selectedTrackRoot = root;
                }
            }
        }

        private void DisableEmbeddedTrackRoots()
        {
            foreach (GameObject rootObject in gameObject.scene.GetRootGameObjects())
            {
                Transform root = rootObject.transform;
                string rootName = root.name.ToLowerInvariant();
                if (rootName.Contains("map_audit") ||
                    rootName.Contains("map_physicsprototype") ||
                    rootName.Equals("checkpoints", StringComparison.OrdinalIgnoreCase))
                {
                    root.gameObject.SetActive(false);
                }
            }
        }

        private void LoadSelectedVehicle()
        {
            if (car == null || car.VehiclePrefab == null)
            {
                return;
            }

            MonoBehaviour previousController = vehicleController;
            Transform spawn = ResolveSpawnTransform(previousController);
            Vector3 position = spawn != null ? spawn.position : Vector3.zero;
            Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;

            GameObject selectedVehicle = Instantiate(car.VehiclePrefab, position, rotation);
            selectedVehicle.name = car.VehiclePrefab.name;
            CarProgression.ApplyPaint(selectedVehicle, car);

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
            if (selectedController is IVehicleController selectedVehicleController)
            {
                driftRewardTracker = selectedController.gameObject.GetComponent<DriftRewardTracker>();
                if (driftRewardTracker == null)
                {
                    driftRewardTracker = selectedController.gameObject.AddComponent<DriftRewardTracker>();
                }
                driftRewardTracker.Configure(selectedVehicleController);
            }
            SetLapTracker(selectedLapTracker);
            RetargetFollowCamera(selectedVehicle.transform);

            RaceSetup setup = FindFirstObjectByType<RaceSetup>(FindObjectsInactive.Include);
            setup?.Configure(track, selectedLapTracker, selectedTrackRoot);
            finishLineCheckpoint = FindFinishLineCheckpoint();
            finishLineTrigger = finishLineCheckpoint != null ? finishLineCheckpoint.GetComponent<Collider>() : null;

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

        private Transform ResolveSpawnTransform(MonoBehaviour fallbackController)
        {
            Transform selectedSpawn = FindSelectedTrackSpawn();
            if (selectedSpawn != null)
            {
                return selectedSpawn;
            }

            return fallbackController != null ? fallbackController.transform : null;
        }

        private Transform FindSelectedTrackSpawn()
        {
            if (track == null || string.IsNullOrWhiteSpace(track.TrackId))
            {
                return null;
            }

            string spawnName = $"{track.TrackId.ToLowerInvariant()}_SpawnPoint";
            foreach (GameObject rootObject in gameObject.scene.GetRootGameObjects())
            {
                Transform namedSpawn = FindChildRecursive(rootObject.transform, spawnName);
                if (namedSpawn != null)
                {
                    return namedSpawn;
                }
            }

            if (selectedTrackRoot != null)
            {
                Transform selectedNamedSpawn = FindChildRecursive(selectedTrackRoot, spawnName);
                if (selectedNamedSpawn != null)
                {
                    return selectedNamedSpawn;
                }

                Transform selectedMapSpawn = selectedTrackRoot.Find("Markers/SpawnPoint");
                if (selectedMapSpawn != null)
                {
                    return selectedMapSpawn;
                }
            }

            foreach (GameObject rootObject in gameObject.scene.GetRootGameObjects())
            {
                if (!rootObject.activeInHierarchy)
                {
                    continue;
                }

                Transform mapSpawn = rootObject.transform.Find("Markers/SpawnPoint");
                if (mapSpawn != null)
                {
                    return mapSpawn;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform result = FindChildRecursive(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void RetargetFollowCamera(Transform selectedVehicle)
        {
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour.GetType().FullName != "SuperRacing.Vehicle.VehicleFollowCamera")
                {
                    continue;
                }

                behaviour.SendMessage("SetTarget", selectedVehicle, SendMessageOptions.DontRequireReceiver);
                return;
            }
        }

        private Checkpoint FindFinishLineCheckpoint()
        {
            if (selectedTrackRoot == null)
            {
                return null;
            }

            foreach (Checkpoint checkpoint in selectedTrackRoot.GetComponentsInChildren<Checkpoint>(true))
            {
                if (checkpoint.CheckpointIndex == 0 && checkpoint.name.Equals("FinishLine", StringComparison.OrdinalIgnoreCase))
                {
                    return checkpoint;
                }
            }

            foreach (Checkpoint checkpoint in selectedTrackRoot.GetComponentsInChildren<Checkpoint>(true))
            {
                if (checkpoint.CheckpointIndex == 0)
                {
                    return checkpoint;
                }
            }

            return null;
        }

        private void Start()
        {
            if (startAutomatically)
            {
                BeginCountdown();
            }
        }

        private void Update()
        {
            if (State == RaceState.Racing && lapTracker != null && lapTracker.IsRaceComplete)
            {
                FinishRace();
            }

            if (State == RaceState.Racing)
            {
                CheckAutomaticFinishLine();
            }
        }

        private void OnDestroy()
        {
            if (lapTracker != null)
            {
                lapTracker.RaceCompleted -= FinishRace;
            }
        }

        private void SetLapTracker(LapTracker selectedLapTracker)
        {
            if (lapTracker == selectedLapTracker)
            {
                return;
            }

            if (lapTracker != null)
            {
                lapTracker.RaceCompleted -= FinishRace;
            }

            lapTracker = selectedLapTracker;

            if (lapTracker != null)
            {
                lapTracker.RaceCompleted += FinishRace;
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

        public bool TryCompleteFromFinishLine(LapTracker triggeringLapTracker)
        {
            if (!CanTryFinishLineCheckpoint(triggeringLapTracker))
            {
                return false;
            }

            return triggeringLapTracker.TryPassCheckpoint(finishLineCheckpoint);
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
            driftRewardTracker?.ResetProgress();

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
            IsTouchingFinishLine = IsVehicleTouchingFinishLine();
            hasLeftFinishLine = !IsTouchingFinishLine;
            vehicle.CanDrive = true;
            raceTimer.StartTimer();
            RaceStarted?.Invoke();
            onRaceStarted.Invoke();
        }

        private void CheckAutomaticFinishLine()
        {
            if (raceTimer == null || finishLineCheckpoint == null || finishLineTrigger == null)
            {
                IsTouchingFinishLine = false;
                return;
            }

            IsTouchingFinishLine = IsVehicleTouchingFinishLine();
            if (!IsTouchingFinishLine)
            {
                hasLeftFinishLine = true;
                return;
            }

            if (hasLeftFinishLine && raceTimer.ElapsedSeconds >= minimumFinishSeconds)
            {
                TryCompleteFromFinishLine(lapTracker);
            }
        }

        private bool CanTryFinishLineCheckpoint(LapTracker triggeringLapTracker)
        {
            return State == RaceState.Racing &&
                triggeringLapTracker != null &&
                triggeringLapTracker == lapTracker &&
                finishLineCheckpoint != null &&
                raceTimer != null &&
                hasLeftFinishLine &&
                raceTimer.ElapsedSeconds >= minimumFinishSeconds;
        }

        private bool IsVehicleTouchingFinishLine()
        {
            if (finishLineTrigger == null || vehicleController == null)
            {
                return false;
            }

            foreach (Collider vehicleCollider in vehicleController.GetComponentsInChildren<Collider>())
            {
                if (vehicleCollider != null &&
                    vehicleCollider.enabled &&
                    !vehicleCollider.isTrigger &&
                    vehicleCollider.bounds.Intersects(finishLineTrigger.bounds))
                {
                    return true;
                }
            }

            return finishLineTrigger.bounds.Contains(vehicleController.transform.position);
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
            driftRewardTracker?.CompleteCurrentDrift();
            RaceRewardSummary rewards = RaceRewardCalculator.Calculate(
                track,
                SetNewRecord,
                driftRewardTracker != null ? driftRewardTracker.CleanDriftSeconds : 0f);
            CurrencyWallet.Add(rewards.Total);
            RaceCompletionState.Save(FinalTimeSeconds, SetNewRecord, track, car, rewards, CurrencyWallet.Balance);
            RaceFinished?.Invoke(FinalTimeSeconds, SetNewRecord);
            onRaceFinished.Invoke(FinalTimeSeconds, SetNewRecord);
        }
    }
}
