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
        private Transform vehicleTransform;
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
            PrototypeVehicleController prototypeVehicle = FindFirstObjectByType<PrototypeVehicleController>();
            vehicle = prototypeVehicle;
            vehicleTransform = prototypeVehicle != null ? prototypeVehicle.transform : null;

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

            DrawLapPanel();
            DrawMinimap();
            DrawSpeedometer();

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

        private void DrawLapPanel()
        {
            const float x = 20f;
            const float y = 20f;
            GUI.Box(new Rect(x, y, 330, 190), string.Empty);
            GUI.Label(new Rect(x + 15, y + 10, 300, 30), $"CURRENT LAP  {lapTracker.CurrentLap}/{lapTracker.TotalLaps}", labelStyle);
            GUI.Label(new Rect(x + 15, y + 42, 300, 34), RaceHUD.FormatTime(raceTimer.ElapsedSeconds), titleStyle);
            GUI.Label(new Rect(x + 15, y + 82, 300, 25), "LAP STANDINGS", labelStyle);
            GUI.Label(new Rect(x + 15, y + 112, 300, 25), "1     PLAYER", labelStyle);

            string bestText = "--:--.---";
            if (raceManager.Track != null && raceManager.Car != null &&
                RecordManager.TryGetBestTime(raceManager.Track.TrackId, raceManager.Car.CarId, out float bestTime))
            {
                bestText = RaceHUD.FormatTime(bestTime);
            }

            GUI.Label(new Rect(x + 15, y + 145, 300, 25), $"BEST TIME     {bestText}", labelStyle);
        }

        private void DrawMinimap()
        {
            const float size = 190f;
            float x = Screen.width - size - 25f;
            const float y = 20f;
            GUI.Box(new Rect(x, y, size, size), "MAP");

            float left = x + 35f;
            float top = y + 35f;
            float trackSize = size - 70f;
            GUI.Box(new Rect(left, top, trackSize, 4), string.Empty);
            GUI.Box(new Rect(left, top + trackSize, trackSize, 4), string.Empty);
            GUI.Box(new Rect(left, top, 4, trackSize), string.Empty);
            GUI.Box(new Rect(left + trackSize, top, 4, trackSize), string.Empty);

            Vector3 position = vehicleTransform.position;
            float markerX = left + Mathf.InverseLerp(-20f, 20f, position.x) * trackSize;
            float markerY = top + (1f - Mathf.InverseLerp(-20f, 20f, position.z)) * trackSize;
            Color previousColor = GUI.color;
            GUI.color = Color.cyan;
            GUI.Box(new Rect(markerX - 6f, markerY - 6f, 12f, 12f), string.Empty);
            GUI.color = previousColor;
        }

        private void DrawSpeedometer()
        {
            const float width = 270f;
            const float height = 160f;
            float x = Screen.width - width - 25f;
            float y = Screen.height - height - 25f;
            int speed = Mathf.RoundToInt(Mathf.Abs(vehicle.SpeedKmh));
            GUI.Box(new Rect(x, y, width, height), string.Empty);
            GUI.Label(new Rect(x, y + 15, width, 65), speed.ToString("000"), titleStyle);
            GUI.Label(new Rect(x, y + 68, width, 30), "km/h", centerStyle);
            GUI.Box(new Rect(x + 25, y + 115, width - 50, 18), string.Empty);
            float fill = Mathf.Clamp01(speed / 80f) * (width - 54f);
            Color previousColor = GUI.color;
            GUI.color = speed > 65 ? Color.red : Color.cyan;
            GUI.Box(new Rect(x + 27, y + 117, fill, 14), string.Empty);
            GUI.color = previousColor;
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
