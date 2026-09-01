# Audio Integration

## Ready-to-use assets

- Drag `Assets/Game/Audio/Prefabs/AudioRoot.prefab` into the bootstrap scene, or rely on the runtime installer.
- Drag `Assets/Game/Audio/Prefabs/AudioSettingsPanel.prefab` under an existing Canvas in the menu.
- Open `Assets/Game/Scenes/AudioSandbox.unity` to audition events, profiles, maps, snapshots and mixer buses.
- Vehicle audio defaults to Balanced. Call `VehicleAudioEmitter.SetProfile(...)` or assign Speedster/Control assets from `Audio/Resources`.

## Controller contract

Implement `IVehicleAudioTelemetrySource` to provide accurate throttle, WheelCollider slip and surface data. Without it, `VehicleAudioEmitter` safely derives speed, grounding and lateral slip from Rigidbody.

## Mixer buses

`SuperRacingAudioMixer.mixer` contains Master, Music, SFX, Vehicle/Engine/Tires/Collision, Race, UI and Ambience groups plus Default, Countdown, Paused and Finish snapshots. Settings persist via PlayerPrefs.

1. Open `Assets/Game/Scenes/AudioSandbox.unity` and press Play to audition every `_CHOSEN` cue.
2. Add `Assets/Game/Audio/Prefabs/AudioRoot.prefab` to a race scene, or rely on the runtime installer.
3. Add `VehicleAudioEmitter` to the root vehicle object containing its `Rigidbody`.
4. Existing race events are connected by `RaceAudioBinder`.
5. Add `UIButtonAudio` to UI controls and connect `PlayClick`, `PlayConfirm`, or `PlayBack` to Button `OnClick`.
6. Select `BeachAudioProfile` or `DesertAudioProfile` with `GameAudioManager.ApplyMapProfile`.

Optional controller integration:

```csharp
vehicleAudioEmitter.SetThrottle(throttle);
vehicleAudioEmitter.SetBrake(isBraking);
```

Without these calls the emitter still derives engine pitch, skid, and collision feedback from physics.
