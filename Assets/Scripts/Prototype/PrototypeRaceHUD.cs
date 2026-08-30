using SuperRacing.Contracts;
using SuperRacing.Race;
using SuperRacing.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SuperRacing.Prototype
{
    public sealed class PrototypeRaceHUD : MonoBehaviour
    {
        private RaceManager raceManager;
        private RaceTimer raceTimer;
        private LapTracker lapTracker;
        private IVehicleController vehicle;
        private int countdownValue;
        private GUIStyle labelStyle;
        private GUIStyle centerStyle;
        private GUIStyle titleStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForTestRace()
        {
            if (SceneManager.GetActiveScene().name != "Test_Race" || FindFirstObjectByType<PrototypeRaceHUD>() != null)
            {
                return;
            }

            new GameObject("Prototype Race HUD").AddComponent<PrototypeRaceHUD>();
        }

        private void Awake()
        {
            raceManager = FindFirstObjectByType<RaceManager>();
            raceTimer = FindFirstObjectByType<RaceTimer>();
            lapTracker = FindFirstObjectByType<LapTracker>();
            vehicle = FindFirstObjectByType<PrototypeVehicleController>();

            if (raceManager == null || raceTimer == null || lapTracker == null || vehicle == null)
            {
                Debug.LogError("PrototypeRaceHUD could not find the Test_Race systems.", this);
                enabled = false;
                return;
            }

            raceManager.CountdownTick += HandleCountdown;
            raceManager.RaceStarted += HandleRaceStarted;
        }

        private void OnDestroy()
        {
            if (raceManager == null)
            {
                return;
            }

            raceManager.CountdownTick -= HandleCountdown;
            raceManager.RaceStarted -= HandleRaceStarted;
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUI.Box(new Rect(20, 20, 260, 125), string.Empty);
            GUI.Label(new Rect(35, 30, 230, 30), $"Speed  {Mathf.RoundToInt(Mathf.Abs(vehicle.SpeedKmh))} km/h", labelStyle);
            GUI.Label(new Rect(35, 65, 230, 30), $"Lap  {lapTracker.CurrentLap}/{lapTracker.TotalLaps}", labelStyle);
            GUI.Label(new Rect(35, 100, 230, 30), RaceHUD.FormatTime(raceTimer.ElapsedSeconds), labelStyle);

            if (raceManager.State == RaceManager.RaceState.Countdown)
            {
                GUI.Label(new Rect(0, Screen.height * 0.32f, Screen.width, 100), countdownValue.ToString(), centerStyle);
            }

            if (raceManager.State != RaceManager.RaceState.Finished)
            {
                return;
            }

            float panelX = Screen.width * 0.5f - 210f;
            float panelY = Screen.height * 0.5f - 150f;
            GUI.Box(new Rect(panelX, panelY, 420, 300), string.Empty);
            GUI.Label(new Rect(panelX, panelY + 25, 420, 50), "RACE COMPLETE", titleStyle);
            GUI.Label(new Rect(panelX, panelY + 85, 420, 40), RaceHUD.FormatTime(raceManager.FinalTimeSeconds), centerStyle);
            GUI.Label(new Rect(panelX, panelY + 130, 420, 35), raceManager.SetNewRecord ? "NEW RECORD!" : "Finished", centerStyle);

            if (GUI.Button(new Rect(panelX + 45, panelY + 205, 150, 50), "Restart"))
            {
                raceManager.RestartRace();
            }

            if (GUI.Button(new Rect(panelX + 225, panelY + 205, 150, 50), "Main Menu"))
            {
                raceManager.ReturnToMainMenu();
            }
        }

        private void HandleCountdown(int value)
        {
            countdownValue = value;
        }

        private void HandleRaceStarted()
        {
            countdownValue = 0;
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            centerStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 34 };
            titleStyle = new GUIStyle(centerStyle) { fontSize = 38 };
        }
    }
}
