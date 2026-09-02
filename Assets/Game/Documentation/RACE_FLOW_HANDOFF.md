# Race Flow Handoff

## Current MVP Flow

The current playable flow is:

1. `MainMenu`
2. `Garage`
3. `TrackSelection`
4. `Race`
5. `complete_race`

`GameSelection` stores the selected car and track in memory and mirrors them to PlayerPrefs. `RaceManager` restores those selections from `GameCatalog` when the `Race` scene loads.

## Race Scene

`RaceManager` instantiates the selected `TrackDefinition.PreviewPrefab` and selected `CarDefinition.VehiclePrefab`, then configures:

- vehicle stats through `IVehicleController.ApplyStats`
- vehicle control gate through `IVehicleController.CanDrive`
- race HUD speed/lap/timer references
- selected map checkpoint markers through `RaceSetup`

Both Beach and Desert are currently one-lap races.

## Finish Flow

When the active vehicle reaches the selected map's `Markers/FinishLine` trigger after leaving the start area, `RaceManager` finishes the race, saves the best time for the selected `trackId + carId`, and stores the final result in `RaceCompletionState`.

The finish overlay shows:

- `COMPLETE`
- final time
- `Press any key or tap anywhere to continue`

It does not show restart/menu buttons. Any keyboard key, mouse click, or touchscreen press transitions to `complete_race`.

## Pause Flow

During active racing, `Esc` opens the pause window. The player can continue or return to `Garage`. Pause is disabled after the race has finished.

## Debug

`RaceFinishDebugOverlay` is disabled by default. To show the temporary finish detection indicator while adjusting finish markers, set:

```csharp
PlayerPrefs.SetInt("super_racing.finish_debug_enabled", 1);
```

Set the value back to `0` or delete the key before normal testing.
