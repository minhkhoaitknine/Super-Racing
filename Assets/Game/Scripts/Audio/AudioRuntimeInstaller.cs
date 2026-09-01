using SuperRacing.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperRacing.Audio
{
    public static class AudioRuntimeInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (GameAudioManager.Instance != null) return;
            AudioCatalog catalog = Resources.Load<AudioCatalog>("AudioCatalog");
            if (catalog == null) return;
            GameObject root = new("AudioRoot (Runtime)");
            GameAudioManager manager = root.AddComponent<GameAudioManager>();
            manager.Configure(catalog);
            root.AddComponent<RaceAudioBinder>();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachVehicleEmitters();
            GameAudioManager manager = GameAudioManager.Instance;
            if (manager != null)
            {
                if (scene.name == "MainMenu" || scene.name == "Garage") manager.PlayMenuMusic();
                else if (scene.name == "AudioSandbox" || scene.name == "Test_Vehicle" || scene.name == "Test_Race" || scene.name.Contains("Race")) manager.PlayRaceMusic();
            }
            if (scene.name == "AudioSandbox" && Object.FindFirstObjectByType<AudioSandboxDebugPanel>() == null)
            {
                if (Object.FindFirstObjectByType<VehicleAudioMonitorOverlay>() == null)
                {
                    new GameObject("Vehicle Audio Monitor").AddComponent<VehicleAudioMonitorOverlay>();
                }
                new GameObject("Audio Sandbox Debug Panel").AddComponent<AudioSandboxDebugPanel>();
            }
        }

        private static void AttachVehicleEmitters()
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                bool isVehicle = behaviour is IVehicleAudioTelemetrySource || behaviour is IVehicleController;
                if (isVehicle && behaviour.GetComponent<VehicleAudioEmitter>() == null && behaviour.GetComponent<Rigidbody>() != null)
                {
                    behaviour.gameObject.AddComponent<VehicleAudioEmitter>();
                }
            }
        }
    }
}
