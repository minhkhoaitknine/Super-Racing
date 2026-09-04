# Audio merge notes

Current baseline: teammate work on `origin/main` at commit `bb18f1c` (includes the completed production race flow).

## Historical integration already present in the baseline

### `Assets/Game/Scripts/Vehicle/VehicleController.cs`

- Added `IVehicleAudioTelemetrySource` to the existing controller.
- Added a read-only `AudioTelemetry` property.
- Reads the controller's existing speed/input and WheelCollider ground hits for RPM, throttle, brake, grounded state, tire slip, and surface audio.
- Does not change motor torque, steering, braking, drift assistance, Rigidbody configuration, car stats, or input bindings.

### `Assets/Scripts/Race/LapTracker.cs`

- Added the `CheckpointPassed` event.
- Raises it only after an expected checkpoint has been accepted.
- Does not change checkpoint order, lap counting, completion, or timing behavior.

### `Assets/Tests/EditMode/SuperRacing.EditModeTests.asmdef`

- Adds the audio assembly reference required by the new audio tests.
- Does not alter teammate test cases.

## Runtime integration

- `AudioRuntimeInstaller` creates the global audio manager when a scene loads.
- It adds one `VehicleAudioEmitter` to vehicles that expose `IVehicleController` or `IVehicleAudioTelemetrySource` and have a Rigidbody.
- The selected production vehicle in `Race` is supported through controller telemetry or the emitter's Rigidbody fallback without modifying that controller.
- Existing emitters are detected, so one vehicle cannot receive a duplicate emitter.
- Vehicle names containing `Speedster` or `Control` select those profiles; all other names use `Balanced`.
- Audio reads gameplay state but does not write to the vehicle controller or race state.

The current audio completion pass does not edit any of those files. All new integration is implemented by runtime adapters under `Assets/Game/Scripts/Audio` and data/assets under `Assets/Game/Audio`.

## Verification status

- Unity Editor imported the merged assets and compiled all project/audio assemblies without C# errors.
- No Git merge conflicts remain.
- Post-merge Unity batch verification passes 45/45 EditMode and 13/13 PlayMode tests. The final pass adds coverage for the settings prefab layout, snapshot reset, the teammate pause-menu integration, and absence of a duplicate audio pause system.
- Asphalt, Sand, and Grass now use separate real recordings for roll/skid. The old scratch/concrete placeholders remain only as unused rollback assets.
- Gear-shift uses two dedicated CC0 mechanical recordings and never reuses `EVT_Race_Restart_CHOSEN`.
- Collision and landing one-shots were restored after isolation testing identified the tire placeholder as the harsh continuous sound. Ground-contact filtering, minimum impact speed, cooldown, and reduced collision gain remain enabled to prevent vibration spam.
- Music follows the production build flow: menu music in `MainMenu`, `Garage`, and `TrackSelection`; race music in `Race`; result music plus the Finish snapshot in `complete_race`.
- Light, medium, and heavy collision use distinct variant sets. Runtime no longer stops the shared source halfway through a heavy cue.
- AudioSandbox Beach/Desert/Race buttons now solo their category. Beach emphasizes waves, Desert emphasizes sand gust, and Race stops ambience before playing music so the three choices are clearly distinguishable.
- Tire skid is enabled with conservative slip thresholds and low gain. It remains silent during normal straight driving.

## Runtime-owned menu/settings integration

- Garage: the existing `Settings` button receives an additional runtime listener; existing listeners are preserved.
- Race: the runtime adapter adds one `AUDIO SETTINGS` entry to the teammate-owned `RacePauseMenu`. It creates no competing Pause button or Escape handler. Audio observes the teammate pause state and pushes/pops the `Paused` snapshot without owning gameplay pause state.
- MainMenu and TrackSelection do not receive a Settings button.
- No teammate scene, controller, race, or UI source file is changed by this pass.

## Manual vehicle audio test

1. Open `Assets/Scenes/Race.unity` (or enter through `MainMenu -> Garage -> TrackSelection`).
2. Press Play, then click inside the Game view.
3. Drive with W/S or Up/Down; steer with A/D or Left/Right; hold Space while steering to brake/drift.
4. Listen for engine start, continuous RPM crossfades, simulated gear shifts, tire roll/skid, collisions, and landing.
5. Open `Assets/Game/Scenes/AudioSandbox.unity` for individual Race/UI/Music/Ambience cues and mixer controls.
