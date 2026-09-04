using UnityEngine;

namespace SuperRacing.Audio
{
    public sealed class AudioSandboxDebugPanel : MonoBehaviour
    {
        private Vector2 scroll;
        private float master = GameAudioManager.MaxMasterVolume;
        private float music = 0.1f;
        private float sfx = 1f;
        private float ambience = 1f;
        private float ui = 1f;
        private int profileIndex = 1;

        private void OnGUI()
        {
            GameAudioManager manager = GameAudioManager.Instance;
            if (manager == null || manager.Catalog == null) return;
            AudioCatalog c = manager.Catalog;
            GUILayout.BeginArea(new Rect(15, 15, 390, Screen.height - 30), GUI.skin.box);
            GUILayout.Label("AUDIO SANDBOX - _CHOSEN clips");
            GUILayout.Label("Last played: " + manager.LastPlayedClipName);
            GUILayout.Label("Music: " + manager.CurrentMusicClipName);
            GUILayout.Label("Ambience: " + manager.CurrentAmbienceClipName);
            VehicleAudioEmitter vehicle = FindFirstObjectByType<VehicleAudioEmitter>();
            if (vehicle != null)
            {
                VehicleAudioTelemetry t = vehicle.CurrentTelemetry;
                GUILayout.Label($"Vehicle: {vehicle.Profile?.displayName ?? "Fallback"} | Gear {t.CurrentGear} | RPM {t.NormalizedRpm:0.00}");
                GUILayout.Label($"{t.SpeedKmh:0} km/h | throttle {t.Throttle:0.00} | slip {t.SidewaysSlip:0.00}/{t.ForwardSlip:0.00} | {t.CurrentSurface}");
                GUILayout.Label($"Last vehicle cue: {vehicle.LastOneShotClipName} | count {vehicle.OneShotPlayCount}");
                GUILayout.Label($"Engine loops: {vehicle.EngineLoopCount} | loudest {vehicle.LoudestEngineVolume:0.00} | tire loops {vehicle.TireLoopCount}");
                string[] profiles = { "Speedster", "Balanced", "Control" };
                int next = GUILayout.SelectionGrid(profileIndex, profiles, 3);
                if (next != profileIndex) { profileIndex = next; vehicle.SetProfile(next == 0 ? c.speedsterProfile : next == 1 ? c.balancedProfile : c.controlProfile); }
            }
            scroll = GUILayout.BeginScrollView(scroll);
            Header("Race events");
            Button("CountdownTick", c.countdownTick); Button("StartedGo", c.startedGo);
            Button("CheckpointPassed", c.checkpointPassed); Button("LapChanged", c.lapChanged);
            Button("Finished", c.finished); Button("NewRecord", c.newRecord);
            Button("InvalidCheckpoint", c.invalidCheckpoint); Button("Restart", c.restart);
            Header("UI events");
            Button("Hover", c.uiHover); Button("Click", c.uiClick); Button("Confirm", c.uiConfirm);
            Button("Back", c.uiBack); Button("SelectionChanged", c.uiSelectionChanged);
            Button("Error", c.uiError); Button("StartRace", c.uiStartRace); Button("ResultsOpen", c.uiResultsOpen);
            Header("Vehicle one-shots");
            Button("EngineStart", c.engineStart); Button("CollisionLight", c.collisionLight);
            Button("CollisionMedium", c.collisionMedium); Button("CollisionHeavy", c.collisionHeavy);
            Button("GearShift", c.gearShift); Button("Respawn", c.respawn); Button("Landing", c.landing);
            Button("Backfire (profile)", vehicle != null ? First(vehicle.Profile?.backfireVariants, vehicle.Profile?.backfire) : null);
            Header("Ambience / Music");
            if (GUILayout.Button("Solo Beach ambience")) { manager.StopMusic(); manager.ApplyMapProfile(Resources.Load<MapAudioProfile>("BeachAudioProfile")); }
            if (GUILayout.Button("Solo Desert ambience")) { manager.StopMusic(); manager.ApplyMapProfile(Resources.Load<MapAudioProfile>("DesertAudioProfile")); }
            if (GUILayout.Button("Solo Race music")) { manager.StopAmbience(); manager.PlayRaceMusic(); }
            Header("Mixer preview");
            Slider("Master", ref master, manager.SetMasterVolume);
            Slider("Music", ref music, manager.SetMusicVolume);
            Slider("SFX", ref sfx, manager.SetSfxVolume);
            Slider("Ambience", ref ambience, manager.SetAmbienceVolume);
            Slider("UI", ref ui, manager.SetUiVolume);
            if (GUILayout.Button(manager.IsMuted ? "Unmute" : "Mute")) manager.SetMuted(!manager.IsMuted);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Default")) manager.ApplySnapshot(AudioSnapshotId.Default);
            if (GUILayout.Button("Countdown")) manager.ApplySnapshot(AudioSnapshotId.Countdown);
            if (GUILayout.Button("Paused")) manager.ApplySnapshot(AudioSnapshotId.Paused);
            if (GUILayout.Button("Finish")) manager.ApplySnapshot(AudioSnapshotId.Finish);
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static void Header(string value) { GUILayout.Space(8); GUILayout.Label(value); }
        private static AudioClip First(AudioClip[] clips, AudioClip fallback)
        {
            if (clips != null) foreach (AudioClip clip in clips) if (clip != null) return clip;
            return fallback;
        }
        private static void Button(string label, AudioClip clip)
        {
            GUI.enabled = clip != null;
            if (GUILayout.Button(label + "  |  " + (clip != null ? clip.name : "MISSING"))) GameAudioManager.Instance.PlayOneShot(clip);
            GUI.enabled = true;
        }
        private static void Slider(string label, ref float value, System.Action<float> apply)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(55));
            float next = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.Label(next.ToString("0.00"), GUILayout.Width(40)); GUILayout.EndHorizontal();
            if (!Mathf.Approximately(next, value)) { value = next; apply(value); }
        }
    }
}
