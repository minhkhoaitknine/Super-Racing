# Audio merge notes

Baseline: teammate work on `origin/main` at commit `7ed2bdf`.

## Existing teammate files changed for audio integration

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
- The teammate's `PrototypeVehicleController` in `Test_Race` is supported through the emitter's Rigidbody fallback without modifying that controller.
- Existing emitters are detected, so one vehicle cannot receive a duplicate emitter.
- Vehicle names containing `Speedster` or `Control` select those profiles; all other names use `Balanced`.
- Audio reads gameplay state but does not write to the vehicle controller or race state.

## Verification status

- Unity Editor imported the merged assets and compiled all project/audio assemblies without C# errors.
- No Git merge conflicts remain.
- Automated batch tests could not start on this machine because Unity Licensing Client IPC timed out. Run them from Unity Test Runner after the Editor license session is active.
- Tire-roll was re-enabled for user testing. Its selected placeholder is still Kenney's `footstep_concrete_002.ogg`, so replace it with a true continuous tire recording if the repeated texture is unsuitable.
- Gear-shift is temporarily muted because the generated profiles incorrectly reused `EVT_Race_Restart_CHOSEN`. Runtime also rejects that legacy mapping. Add a dedicated vehicle shift sample before re-enabling it.
- Collision and landing one-shots were restored after isolation testing identified the tire placeholder as the harsh continuous sound. Ground-contact filtering, minimum impact speed, cooldown, and reduced collision gain remain enabled to prevent vibration spam.
- Music now starts by scene context: menu music in `MainMenu`/`Garage`, race music in `Test_Vehicle`, `Test_Race`, `AudioSandbox`, and race-named scenes. Race events still restart/crossfade race music at the proper start signal.
- Heavy collision playback is audition-trimmed to 50% with a short fade-out; the source asset remains intact for rollback/re-editing.
- AudioSandbox Beach/Desert/Race buttons now solo their category. Beach emphasizes waves, Desert emphasizes sand gust, and Race stops ambience before playing music so the three choices are clearly distinguishable.
- Tire-skid is now muted by default: its selected placeholder is Kenney Interface Sounds `scratch_002.ogg`, not a tire recording, and looping it caused the confirmed harsh vibration-synchronised sound. Slip telemetry remains intact for a future replacement clip.

## Manual vehicle audio test

1. Open `Assets/Game/Scenes/Test_Vehicle.unity`.
2. Press Play, then click inside the Game view.
3. Drive with W/S or Up/Down; steer with A/D or Left/Right; hold Space while steering to brake/drift.
4. Listen for engine start, continuous RPM crossfades, simulated gear shifts, tire roll/skid, collisions, and landing.
5. Open `Assets/Game/Scenes/AudioSandbox.unity` for individual Race/UI/Music/Ambience cues and mixer controls.
