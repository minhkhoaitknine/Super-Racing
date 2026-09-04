using System.Collections;
using SuperRacing.Contracts;
using SuperRacing.Data;
using SuperRacing.Race;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SuperRacing.Audio
{
    public static class AudioRuntimeInstaller
    {
        private static string previousSceneName = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            if (GameAudioManager.Instance != null) return;
            AudioCatalog catalog = Resources.Load<AudioCatalog>("AudioCatalog");
            if (catalog == null) return;
            GameObject root = new("AudioRoot (Runtime)");
            GameAudioManager manager = root.AddComponent<GameAudioManager>();
            manager.Configure(catalog, catalog.mixer);
            root.AddComponent<RaceAudioBinder>();
            root.AddComponent<AudioSettingsRuntimePresenter>();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureAudioListener();
            bool menuScene = IsMenuScene(scene.name);
            bool resultScene = IsResultScene(scene.name);
            if (menuScene || resultScene) SilenceMenuVehicleAudio();
            else if (IsRaceScene(scene.name))
            {
                AttachVehicleEmitters();
                AttachCheckpointObservers();
            }
            AttachUIButtonAudio();
            GameAudioManager manager = GameAudioManager.Instance;
            if (manager != null)
            {
                if (menuScene)
                {
                    manager.ResetSnapshotState(AudioSnapshotId.Default, .15f);
                    if (scene.name.ToLowerInvariant().Contains("trackselection"))
                    {
                        // Keep the menu bed while the highlighted map contributes its
                        // own Beach/Desert ambience layer.
                        manager.PlayMenuMusic();
                        AttachTrackSelectionAudioPreview(scene);
                    }
                    else
                    {
                        manager.StopAmbience();
                        manager.PlayMenuMusic();
                    }
                }
                else if (resultScene)
                {
                    manager.ResetSnapshotState(AudioSnapshotId.Finish, .2f);
                    manager.StopAmbience();
                    manager.PlayResultMusic();
                    if (!IsRaceScene(previousSceneName)) manager.PlayCue(AudioCueId.UIResultsOpen, null, .7f);
                }
                else if (IsRaceScene(scene.name))
                {
                    // A persistent manager can arrive from Garage/TrackSelection with a
                    // snapshot left by a previous pause, countdown, or finish. Start every
                    // race from the same mix so opening Test_Vehicle directly and entering
                    // through MainMenu cannot produce different vehicle loudness.
                    manager.ResetSnapshotState(AudioSnapshotId.Default, 0f);
                    manager.PlayRaceMusic();
                    if (scene.name != "AudioSandbox") ApplySelectedMapAmbience(scene, manager);
                    if (!string.IsNullOrEmpty(previousSceneName) && previousSceneName == scene.name)
                        manager.PlayCue(AudioCueId.Restart, null, .8f);
                }
                manager.StartCoroutine(RefreshLateRuntimeBindings(scene));
            }
            previousSceneName = scene.name;
            if (scene.name == "AudioSandbox" && Object.FindFirstObjectByType<AudioSandboxDebugPanel>() == null)
            {
                if (Object.FindFirstObjectByType<VehicleAudioMonitorOverlay>() == null)
                {
                    new GameObject("Vehicle Audio Monitor").AddComponent<VehicleAudioMonitorOverlay>();
                }
                new GameObject("Audio Sandbox Debug Panel").AddComponent<AudioSandboxDebugPanel>();
            }
        }

        private static IEnumerator RefreshLateRuntimeBindings(Scene scene)
        {
            // Several teammate-owned screens build their buttons and selected vehicle at
            // runtime. Re-scan after Awake/Start without requiring changes to those systems.
            yield return null;
            if (!scene.IsValid() || !scene.isLoaded) yield break;
            AttachUIButtonAudio();
            if (IsRaceScene(scene.name))
            {
                AttachVehicleEmitters();
                AttachCheckpointObservers();
            }
        }

        private static void AttachVehicleEmitters()
        {
            int attached = 0;
            int existing = 0;
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                bool isVehicle = behaviour is IVehicleAudioTelemetrySource || behaviour is IVehicleController;
                if (!isVehicle || behaviour.GetComponent<Rigidbody>() == null) continue;
                if (behaviour.GetComponent<VehicleAudioEmitter>() == null)
                {
                    behaviour.gameObject.AddComponent<VehicleAudioEmitter>();
                    attached++;
                }
                else existing++;
            }
            Debug.Log($"[Audio] Vehicle emitters: attached {attached}, existing {existing}.");
        }

        private static void SilenceMenuVehicleAudio()
        {
            foreach (VehicleAudioEmitter emitter in Object.FindObjectsByType<VehicleAudioEmitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                emitter.enabled = false;
                foreach (AudioSource source in emitter.GetComponents<AudioSource>())
                {
                    source.Stop();
                    source.mute = true;
                }
            }
        }

        private static void AttachUIButtonAudio()
        {
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                UIButtonAudio audio = button.GetComponent<UIButtonAudio>();
                if (audio == null) audio = button.gameObject.AddComponent<UIButtonAudio>();
                audio.EnableAutomaticClick(CueForButton(button));
            }
        }

        public static AudioCueId CueForButton(Button button)
        {
            string semantic = button != null ? button.name.ToLowerInvariant() : "";
            if (button != null)
            {
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    semantic += " " + button.onClick.GetPersistentMethodName(i).ToLowerInvariant();
            }
            string normalized = semantic.Replace(" ", "").Replace("_", "").Replace("-", "");
            if (normalized.Contains("startrace") || normalized.Contains("beginrace")) return AudioCueId.UIStartRace;
            if (normalized.Contains("error") || normalized.Contains("invalid") || normalized.Contains("denied")) return AudioCueId.UIError;
            if (normalized.Contains("confirm") || normalized.Contains("apply") || normalized.Contains("save")) return AudioCueId.UIConfirm;
            if (normalized.Contains("return") || normalized.Contains("back") || normalized.Contains("close") || normalized.Contains("cancel") || normalized.Contains("garage") || normalized.Contains("mainmenu")) return AudioCueId.UIBack;
            if (normalized.Contains("select") || normalized.Contains("previous") || normalized.Contains("next")) return AudioCueId.UISelectionChanged;
            return AudioCueId.UIClick;
        }

        private static void AttachCheckpointObservers()
        {
            foreach (Checkpoint checkpoint in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
                if (checkpoint.GetComponent<CheckpointAudioObserver>() == null) checkpoint.gameObject.AddComponent<CheckpointAudioObserver>();
        }

        private static bool IsMenuScene(string sceneName)
        {
            string value = sceneName.ToLowerInvariant();
            return value.Contains("menu") || value.Contains("garage") || value.Contains("selection") || value.Contains("lobby");
        }

        private static bool IsRaceScene(string sceneName)
        {
            string value = sceneName.ToLowerInvariant();
            if (IsResultScene(value)) return false;
            return value.Contains("race") || value.Contains("vehicle") || value.Contains("sandbox") || value.Contains("track");
        }

        private static bool IsResultScene(string sceneName)
        {
            string value = sceneName == null ? "" : sceneName.ToLowerInvariant();
            return value.Contains("complete_race") || value.Contains("completerace") || value.Contains("result");
        }

        private static void ApplySelectedMapAmbience(Scene scene, GameAudioManager manager)
        {
            // A concrete active map in the loaded scene is more reliable than a stale
            // selection left over from a previous menu/test run.
            string sceneTrackId = InferTrackId(scene);
            string trackId = !string.IsNullOrWhiteSpace(sceneTrackId)
                ? sceneTrackId
                : GameSelection.SelectedTrack != null ? GameSelection.SelectedTrack.TrackId : "";

            string profileName = trackId.ToLowerInvariant() switch
            {
                "beach" => "BeachAudioProfile",
                "desert" => "DesertAudioProfile",
                "town_square" => "TownSquareAudioProfile",
                _ => ""
            };

            if (string.IsNullOrEmpty(profileName))
            {
                manager.StopAmbience();
                return;
            }

            MapAudioProfile profile = Resources.Load<MapAudioProfile>(profileName);
            manager.ApplyMapProfile(profile);
            if (profile != null) Debug.Log($"[Audio] Applied map ambience: {profile.displayName}");
        }

        private static void AttachTrackSelectionAudioPreview(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<TrackSelectionAudioPreview>(true) != null) return;
            }

            GameObject preview = new("Track Selection Audio Preview (Runtime)");
            SceneManager.MoveGameObjectToScene(preview, scene);
            preview.AddComponent<TrackSelectionAudioPreview>();
        }

        private static string InferTrackId(Scene scene)
        {
            string sceneName = scene.name.ToLowerInvariant();
            if (sceneName.Contains("beach")) return "beach";
            if (sceneName.Contains("desert")) return "desert";
            if (sceneName.Contains("townsquare") || sceneName.Contains("town_square")) return "town_square";

            bool beachActive = false;
            bool desertActive = false;
            bool townSquareActive = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (!root.activeInHierarchy) continue;
                string rootName = root.name.ToLowerInvariant();
                beachActive |= rootName.Contains("beach") && rootName.Contains("map");
                desertActive |= rootName.Contains("desert") && rootName.Contains("map");
                townSquareActive |= (rootName.Contains("townsquare") || rootName.Contains("town_square")) && rootName.Contains("map");
            }

            if (townSquareActive) return "town_square";
            if (beachActive && !desertActive) return "beach";
            if (desertActive && !beachActive) return "desert";
            return "";
        }

        private static void EnsureAudioListener()
        {
            if (Object.FindFirstObjectByType<AudioListener>() != null) return;
            Camera camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null) camera.gameObject.AddComponent<AudioListener>();
            else new GameObject("Audio Listener (Runtime)").AddComponent<AudioListener>();
        }
    }

    [DefaultExecutionOrder(-10000)]
    internal sealed class CheckpointAudioObserver : MonoBehaviour
    {
        private Checkpoint checkpoint;
        private float lastInvalidTime = -10f;
        private void Awake() => checkpoint = GetComponent<Checkpoint>();
        private void OnTriggerEnter(Collider other)
        {
            LapTracker tracker = other.GetComponentInParent<LapTracker>();
            if (tracker == null || checkpoint == null || !tracker.CanAcceptCheckpoints || tracker.IsRaceComplete) return;
            if (checkpoint.CheckpointIndex == tracker.ExpectedCheckpointIndex) return;
            if (Time.unscaledTime - lastInvalidTime < .5f) return;
            lastInvalidTime = Time.unscaledTime;
            GameAudioManager.Instance?.PlayCue(AudioCueId.InvalidCheckpoint, null, .7f);
        }
    }

    /// <summary>
    /// Audio-only adapter for the teammate-owned TrackSelection scene. The selection UI creates
    /// one active object named "{Track Display Name} Preview", so observing that object lets the
    /// ambience follow the highlighted card without coupling to TrackSelectionUI's private state.
    /// </summary>
    internal sealed class TrackSelectionAudioPreview : MonoBehaviour
    {
        private const float ScanInterval = .12f;
        private string activeProfileName;
        private float nextScanTime;

        private void Update()
        {
            if (Time.unscaledTime < nextScanTime) return;
            nextScanTime = Time.unscaledTime + ScanInterval;

            string profileName = FindActivePreviewProfile();
            if (string.IsNullOrEmpty(profileName) || profileName == activeProfileName) return;

            MapAudioProfile profile = Resources.Load<MapAudioProfile>(profileName);
            if (profile == null || GameAudioManager.Instance == null) return;

            activeProfileName = profileName;
            GameAudioManager.Instance.ApplyMapProfile(profile, .4f);
            Debug.Log($"[Audio] Track preview ambience: {profile.displayName}");
        }

        private string FindActivePreviewProfile()
        {
            Scene scene = gameObject.scene;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(false))
                {
                    string value = item.name.ToLowerInvariant();
                    if (!value.Contains("preview")) continue;
                    if (value.Contains("beach")) return "BeachAudioProfile";
                    if (value.Contains("desert")) return "DesertAudioProfile";
                    if (value.Contains("town square") || value.Contains("townsquare") || value.Contains("town_square")) return "TownSquareAudioProfile";
                }
            }

            return "";
        }
    }
}
