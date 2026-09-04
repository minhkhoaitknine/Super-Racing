# Super Racing Audio — teammate integration guide

This folder contains the Unity Audio implementation for vehicle, race, UI, music, ambience, mixer settings, and the `AudioSandbox` audition scene.

## Quick start

1. Open the project and wait for Unity to finish importing audio.
2. Open `Assets/Game/Scenes/AudioSandbox.unity` and press Play.
3. Use the panel in Game view to audition Race/UI/Vehicle cues, solo Beach/Desert ambience, solo Race music, change vehicle profiles, snapshots, and bus volumes.
4. For a normal scene, do not write bootstrap code: `AudioRuntimeInstaller` creates one persistent `GameAudioManager` before scene load.
5. A GameObject implementing `IVehicleController` or `IVehicleAudioTelemetrySource`, with a `Rigidbody`, automatically receives one `VehicleAudioEmitter`.

`AudioRoot.prefab` may be placed manually in a bootstrap scene if preferred. `GameAudioManager` is a singleton, so duplicate managers destroy themselves.

## Important folders

```text
Assets/Game/Audio/
├── Clips/                 Imported audio grouped by Vehicle/Race/UI/Ambience/Music
├── Documentation/         Attribution, source manifest, integration and merge notes
├── Prefabs/               AudioRoot and AudioSettingsPanel
├── Resources/             Catalog, vehicle/surface/map profiles
├── SuperRacingAudioMixer.mixer
└── README.md

Assets/Game/Scripts/Audio/
├── AudioRuntimeInstaller.cs
├── GameAudioManager.cs
├── VehicleAudioEmitter.cs
├── RaceAudioBinder.cs
├── UIButtonAudio.cs
└── profile/API types
```

## Vehicle integration

### Automatic fallback

If a controller only implements `IVehicleController`, audio derives speed, grounded state, and approximate sideways slip from its `Rigidbody`. This is sufficient for prototypes such as `PrototypeVehicleController`.

### Accurate controller telemetry

For production vehicles, implement `IVehicleAudioTelemetrySource` on the controller:

```csharp
using SuperRacing.Audio;

public VehicleAudioTelemetry AudioTelemetry => new()
{
    SpeedKmh = speedKmh,
    NormalizedRpm = normalizedRpm, // 0..1; leave 0 for audio simulation
    CurrentGear = currentGear,     // leave 0 for audio simulation
    Throttle = Mathf.Abs(throttle),
    Brake = brake,
    IsGrounded = groundedWheelCount > 0,
    ForwardSlip = maximumForwardSlip,
    SidewaysSlip = maximumSidewaysSlip,
    CurrentSurface = SurfaceType.Asphalt
};
```

`VehicleController` already provides this interface using its four WheelColliders. Audio only reads telemetry; it does not change torque, steering, braking, drift, transmission, or Rigidbody physics.

### Vehicle profiles

Profiles live in `Resources`:

- `SpeedsterAudioProfile.asset`: brighter, high-revving, six simulated gears.
- `BalancedAudioProfile.asset`: neutral five-gear profile.
- `ControlAudioProfile.asset`: lower/heavier four-gear profile.

Runtime chooses a profile from the vehicle GameObject name: `Speedster`, `Control`, otherwise `Balanced`. It can also be changed explicitly:

```csharp
VehicleAudioEmitter emitter = vehicle.GetComponent<VehicleAudioEmitter>();
emitter.SetProfile(profile);
```

Audio gear changes are presentation-only and never modify gameplay gearing.

## Race integration

`RaceAudioBinder` automatically locates `RaceManager`, `LapTracker`, and `GameAudioManager`, then subscribes to:

- Countdown tick
- Race started / GO
- Valid checkpoint
- Lap changed
- Finish / new record

`LapTracker.CheckpointPassed` is raised only after the expected checkpoint is accepted. Do not invoke audio directly from checkpoint triggers as that would duplicate the cue.

For custom race systems, use enum-based calls:

```csharp
GameAudioManager.Instance.PlayCue(AudioCueId.CheckpointPassed);
GameAudioManager.Instance.PlayCue(AudioCueId.InvalidCheckpoint);
GameAudioManager.Instance.ApplySnapshot(AudioSnapshotId.Countdown, 0.25f);
GameAudioManager.Instance.PlayMusic(MusicId.Race, 0.5f);
```

## UI integration

Add `UIButtonAudio` to a UI object and connect its public methods from Unity Button events:

- `PlayClick`
- `PlayConfirm`
- `PlayBack`
- `PlaySelectionChanged`
- `PlayError`

`UIButtonAudio` implements pointer/select interfaces, but hover and navigation-select playback are intentionally disabled until a softer hover recording is approved. Runtime semantic binding selects exactly one click cue from the button name and persistent method: Start Race, Confirm, Back/Return, car/map selection, or generic Click.

```csharp
GameAudioManager.Instance.PlayCue(AudioCueId.UIStartRace);
GameAudioManager.Instance.PlayCue(AudioCueId.UIResultsOpen);
```

Avoid wiring both `UIButtonAudio` and another click handler to the same cue.

### Settings panel

`AudioSettingsRuntimePresenter` installs the panel without editing teammate scenes. The existing Garage `Settings` button opens it. In the production `Race` scene, the presenter adds an `AUDIO SETTINGS` entry to the teammate-owned `RacePauseMenu`; it does not create another Escape handler or another Pause button. The teammate pause menu remains responsible for pausing/unpausing, while Audio observes `Time.timeScale` and applies/restores the `Paused` snapshot. Test/prototype race scenes without a pause menu receive only a small settings launcher. The prefab contains:

- Master, Music, SFX, Ambience, and UI sliders
- Mute/unmute
- Reset defaults
- Percentage labels
- `PlayerPrefs` persistence

No MainMenu or TrackSelection settings button is added. The presenter only adds listeners; it does not remove or replace teammate listeners. Sliders update the Mixer without repeated preview sounds while dragging; releasing a slider plays one short preview where appropriate.

Public settings API:

```csharp
manager.SetBusVolume(AudioBus.Music, 0.1f);
manager.SetMuted(true);
manager.ResetAudioSettings();
```

## Music and map ambience

Current build flow is `MainMenu -> Garage -> TrackSelection -> Race -> complete_race`. Scene defaults:

- Scene names containing `Menu`, `Garage`, `Selection`, or `Lobby`: menu music.
- `Race`, vehicle test scenes, and `AudioSandbox`: race music.
- `complete_race`: Finish snapshot, result music, no ambience, and one Results Open cue.

Race scenes automatically apply ambience from `GameSelection.SelectedTrack`: `beach` loads `BeachAudioProfile`, while `desert` loads `DesertAudioProfile`. When no track is selected, the installer falls back to the scene name or the single active Beach/Desert map root. Returning to a menu scene stops map ambience. `AudioSandbox` remains manual so its Solo buttons are predictable.

`TrackSelection` plays the low-level menu music and map ambience together before Start Race: the active Beach/Desert 3D preview is observed by an audio-only runtime adapter and the matching profile fades in over 0.4 seconds. Changing the highlighted card crossfades only the ambience while menu music continues. This does not modify or depend on private fields in the teammate-owned `TrackSelectionUI`.
- Default Music volume is 10%. A one-time settings migration changes the previous local 70% default to 10%; users can adjust it normally afterward.
- Mixer-fix migration v3 restores Master/SFX/UI to 100%, Ambience to 70%, Music to 10%, and clears mute once. This prevents stale settings created while the exposed Mixer GUIDs were invalid from leaving all vehicle audio silent.

All Unity UI `Button` objects receive `UIButtonAudio` at scene load. MainMenu/Garage buttons get automatic click feedback without modifying teammate scene files. Hover and keyboard-selection sounds are intentionally disabled because the current hover clip is too mechanical and fatiguing.

Menu, Garage, Lobby, and Selection scenes never receive runtime vehicle emitters. If a preview prefab already contains a `VehicleAudioEmitter`, the runtime integration disables it and stops its sources so showroom cars remain silent.

The runtime integration does not create or modify a display camera. If a scene has no `AudioListener`, it adds one to an existing camera (including a RenderTexture preview camera), or creates an audio-listener-only object when the scene has no camera. This keeps the teammate's camera and Canvas setup untouched.

Lobby scenes whose preview camera renders only to a RenderTexture receive a runtime Display camera with `cullingMask = 0` plus an `AudioListener`. This removes Unity's `No cameras rendering`/missing-listener state without changing the teammate's preview-camera setup.

Apply ambience when the selected map is known:

```csharp
MapAudioProfile mapAudio = Resources.Load<MapAudioProfile>("BeachAudioProfile");
GameAudioManager.Instance.ApplyMapProfile(mapAudio);
```

Available profiles:

- Beach: waves are primary; wind is subtle.
- Desert: dry wind plus a stronger sand-gust layer.

The sandbox buttons intentionally solo Beach, Desert, or Race to make comparison easy. In actual gameplay, music and ambience may run together.

## Mixer and snapshots

Mixer hierarchy:

```text
Master
├── Music
├── SFX
│   ├── Vehicle
│   │   ├── Engine
│   │   ├── Tires
│   │   └── Collision
│   ├── Race
│   └── UI
└── Ambience
```

Snapshots:

- `Default`: normal race mix.
- `Countdown`: ducks music.
- `Paused`: reduces music/ambience/SFX and applies a light 5.5 kHz low-pass to manager and vehicle sources.
- `Finish`: emphasizes result feedback.

Exposed parameters are `MasterVolume`, `MusicVolume`, `SfxVolume`, `AmbienceVolume`, and `UiVolume`.

## Replacing a clip safely

1. Verify license before downloading. Do not use CC-BY-NC or audio extracted from another game.
2. Put the selected file in the appropriate `Clips` subfolder.
3. Name it with its event and `_CHOSEN` or `_REALISTIC_CHOSEN`.
4. Update `AudioCatalog.asset` or the relevant vehicle/surface/map profile.
5. Update `AudioProjectSetup.cs` so rebuilding assets does not restore an old reference.
6. Update `Documentation/AudioManifest.csv` with author, source URL, license, candidate names, and SHA-256.
7. Update `Documentation/AUDIO_ATTRIBUTION.md`.
8. Test the clip in `AudioSandbox`, then in the production `Race` scene at realistic volume.

Unity references assets by GUID. Replacing the contents of an existing asset while preserving its `.meta` file keeps scene/prefab references intact.

## Current audio decisions

### Runtime cue coverage

| Area | Runtime trigger |
|---|---|
| Engine idle/low/mid/high, load/off-load, gear shift, backfire | `VehicleAudioEmitter` reads controller telemetry; Rigidbody fallback remains available. |
| Tire roll/skid and Space brake | Continuous speed/slip/brake mix with Asphalt/Sand/Grass profile switching. |
| Collision light/medium/heavy, landing, respawn | Impulse/ground/teleport detection with thresholds and cooldowns. |
| Countdown, GO, valid/invalid checkpoint, lap, finish/new record | `RaceAudioBinder` plus the audio-only invalid-checkpoint observer. |
| Restart | Detected when the production race scene reloads. |
| MainMenu/Garage/TrackSelection/result UI | Runtime semantic binding gives every button exactly one typed UI cue. |
| Menu/race/result music and Beach/Desert ambience | Scene and selected/highlighted map adapters; music and map ambience play together in TrackSelection and Race. |

Two catalog clips are deliberately not automatic: `UIHover` is disabled because its current recording is fatiguing, and `EngineStart` is disabled at spawn because its transient was mistaken for an unwanted landing thump. Both remain available in `AudioSandbox` for audition/replacement. `UIError` is ready and automatically chosen for any button whose name or persistent method contains `error`, `invalid`, or `denied`; the current production screens have no dedicated invalid-action button.

The GO cue uses the short spoken `EVT_Race_StartedGo_VOICE_NORMALIZED_CHOSEN.wav`, synchronized to `RaceStarted`. It is a preloaded PCM one-shot (0.457 s, measured RMS 0.200) so its first playback is not lost to streaming/decode latency. The countdown music duck remains active until the voice finishes. The source MP3 and old Kenney confirmation tone remain in the Clips folder only as rollback candidates and are not referenced by the Catalog.

`Town Square` is detected from track id `town_square`, runtime map roots, and its Track Selection preview. Its audio-only map profile uses a restrained two-layer outdoor wind bed so the newly added teammate map is never silent; Beach and Desert profiles remain unchanged.

- Light, medium, and heavy collision use independent curated `*_TRIMMED_CHOSEN.wav` recordings (0.38s / 0.44s / 0.55s), impulse thresholds, random pitch, and a 0.55-second anti-spam cooldown. Each edit removes leading/trailing silence, normalizes safely, and ends with a 35 ms fade to avoid clicks. Quiet/plastic placeholders are excluded from runtime. Valid impacts play at 72–92% source gain, the Collision mixer group adds +6 dB during normal gameplay, and one-shots use 5% spatial blend so a third-person camera does not lose the transient.
- Asphalt, Sand, and Grass use different real recordings. Roll fades with speed; skid fades in only above each profile's slip threshold. The old repeating `scratch_002` placeholder is not referenced.
- Tire roll/skid are foregrounded with lower slip thresholds and stronger real-recording gains; skid uses 55% of its surface profile gain instead of the previous 32%. Landing is armed only after 0.25 seconds of stable ground contact, preventing the spawn-settling “thump” while preserving genuine jump landings.
- Holding Space uses a dedicated brake-skid source separate from natural corner/wheel slip. It starts fading in above 8 km/h, is already audible around 20 km/h, reaches 95% blend by 45 km/h, attacks quickly, and uses only 2% spatial blend so the chase camera does not lose it. On Asphalt at 40 km/h its target source gain is about 0.59; normal tire roll and corner skid keep their previous levels. Quieter Sand/Grass recordings retain their surface-specific gain. Automatic EngineStart playback is disabled at spawn because its low transient was perceived as the remaining start-of-scene “thump"; the cue stays available in the Catalog and AudioSandbox.
- Dedicated mechanical recordings are used for presentation-only gear shifts. The Race Restart cue is never reused as a shift.
- Backfire support is active on all three vehicle profiles using the short CC0 `EVT_Vehicle_Backfire_REAL_CHOSEN` exhaust cue. It triggers only after a sharp throttle release above the profile RPM threshold and has a 0.8-second cooldown.
- Collision filtering ignores ground-like contacts and uses speed/impulse plus cooldown to prevent suspension vibration spam.
- Master is capped/defaulted at 70%, Music defaults to 10%, and Ambience defaults to 70% with no extra mixer headroom (0% remains fully silent). Settings migration v8 applies this balance to existing installs. Compared with the previous mix, global output gains about 2.9 dB while ambience drops about 6.2 dB overall, so waves/wind remain present without masking the vehicle. Runtime uses `*_NORMALIZED_CHOSEN.wav` derivatives: Beach waves RMS 0.09, Beach/Desert wind RMS 0.08, and Desert gust RMS 0.07, all peak-limited to 0.9. Beach uses 100% waves + 75% wind; Desert uses 100% wind + 65% sand gust. Mixer-routed sources do not apply the same bus volume a second time.
- Vehicle mix is intentionally foregrounded over the 10% music bed: the Vehicle mixer branch receives +10 dB in Default/Countdown, +8 dB while paused, and +4 dB at Finish. RPM layers use 84–90% profile gain, throttle load uses 42–46%, and vehicle one-shots have dedicated higher gains. UI and race cues are unchanged.

## Debugging

Runtime vehicle isolation keys, after clicking Game view:

- `F6` or `Fn+F6`: engine mute/on.
- `F7` or `Fn+F7`: tire mute/on.
- `F8` or `Fn+F8`: vehicle one-shots mute/on.

Console messages begin with `[VehicleAudio]`. Each vehicle one-shot logs its clip name, count, and gain.

Common checks:

- No music: confirm Music PlayerPrefs/slider is above zero and the manager is not muted.
- Duplicate cue: ensure only one gameplay event path calls it.
- No vehicle audio: root object needs a Rigidbody and either vehicle interface.
- Wrong surface: collider name/tag should contain `sand` or `grass`; otherwise Asphalt is used.
- Null catalog: confirm `Assets/Game/Audio/Resources/AudioCatalog.asset` exists.

## Test checklist

1. Run EditMode tests from `Window > General > Test Runner`.
2. Run PlayMode tests.
3. Test all buttons in `AudioSandbox`.
4. Test the full build flow: `MainMenu -> Garage -> TrackSelection -> Race -> complete_race`.
5. In `Race`, test idle, accelerate, release throttle, steer, Space brake, impact, and a real landing.
6. Confirm no audio exception, repeated collision spam, clipping, or abrupt loop transition.
7. Pause via the teammate menu, open `AUDIO SETTINGS`, and verify all five saved volume controls.

For the exact merge footprint on teammate gameplay code, see `Documentation/TEAMMATE_MERGE_NOTES.md`. For legal/source details, see `Documentation/AUDIO_ATTRIBUTION.md` and `Documentation/AudioManifest.csv`.
