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

`UIButtonAudio` implements `IPointerEnterHandler` and `ISelectHandler`, so hover/keyboard selection feedback is automatic. Start-race and results cues are called explicitly:

```csharp
GameAudioManager.Instance.PlayCue(AudioCueId.UIStartRace);
GameAudioManager.Instance.PlayCue(AudioCueId.UIResultsOpen);
```

Avoid wiring both `UIButtonAudio` and another click handler to the same cue.

### Settings panel

Drag `Assets/Game/Audio/Prefabs/AudioSettingsPanel.prefab` under a menu Canvas. It contains:

- Master, Music, SFX, Ambience, and UI sliders
- Mute/unmute
- Reset defaults
- Percentage labels
- `PlayerPrefs` persistence

Public settings API:

```csharp
manager.SetBusVolume(AudioBus.Music, 0.7f);
manager.SetMuted(true);
manager.ResetAudioSettings();
```

## Music and map ambience

Scene defaults:

- `MainMenu` / `Garage`: menu music.
- `Test_Vehicle`, `Test_Race`, `AudioSandbox`, and race-named scenes: race music.

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
- `Paused`: reduces music/ambience/SFX.
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
8. Test the clip in `AudioSandbox`, then in `Test_Vehicle` at realistic volume.

Unity references assets by GUID. Replacing the contents of an existing asset while preserving its `.meta` file keeps scene/prefab references intact.

## Current known audio decisions

- Realistic light collision: OpenGameArt `qubodup-crash.ogg`, CC0.
- Realistic medium/heavy collision: DRAGON-STUDIO Pixabay car crash.
- Heavy collision currently plays only its first 50%, with a short fade-out.
- Collision filtering ignores ground-like contacts, requires impact speed, and has a cooldown to prevent suspension vibration spam.
- `TireRoll` is enabled for evaluation but still uses a concrete-footstep placeholder; replace it with a true continuous tire recording.
- `TireSkid` is disabled because its old `scratch_002.ogg` UI sample caused the confirmed harsh repeating sound. Slip telemetry remains implemented.
- Gear-shift audio is disabled because the generated profile previously reused the Race Restart cue. Add a dedicated gear-shift sample before enabling it.
- Legacy Kenney collision clips remain as rollback candidates but are not referenced by the catalog.

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
4. Drive `Test_Vehicle`: idle, accelerate, release throttle, steer, brake, impact, and landing.
5. Confirm no audio exception, repeated collision spam, clipping, or abrupt loop transition.
6. Test Master at 70–80% and verify all five saved volume controls.

For the exact merge footprint on teammate gameplay code, see `Documentation/TEAMMATE_MERGE_NOTES.md`. For legal/source details, see `Documentation/AUDIO_ATTRIBUTION.md` and `Documentation/AudioManifest.csv`.
