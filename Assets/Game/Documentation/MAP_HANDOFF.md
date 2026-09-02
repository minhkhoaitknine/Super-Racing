# Map Handoff

## Prefabs

Use these prefabs for race scene integration:

| Track | Prefab | Notes |
| --- | --- | --- |
| Beach | `Assets/Game/Prefabs/Maps/BeachMap.prefab` | Built from `BeachMap_PhysicsPrototype`, with colliders and ordered marker suggestions. |
| Desert | `Assets/Game/Prefabs/Maps/DesertMap.prefab` | Built from `DesertMap_PhysicsPrototype`, with colliders and ordered marker suggestions. |

The `*_PhysicsPrototype` prefabs remain available as raw collider-only map prototypes.

## Marker Contract

Each handoff prefab contains a `Markers` child:

- `SpawnPoint`: suggested vehicle spawn transform.
- `FinishLine`: trigger volume with `SuperRacing.Race.Checkpoint` index `0`.
- `Checkpoint_01`, `Checkpoint_02`, ...: trigger volumes with increasing checkpoint indexes.

Member B can collect the `Checkpoint` components under `Markers` and pass their count into `LapTracker.Initialize(checkpointCount, lapCount)`.

## Checkpoint Counts

| Track | Checkpoint count passed to `LapTracker.Initialize` |
| --- | --- |
| Beach | `6` |
| Desert | `7` |

The count includes `FinishLine` as checkpoint index `0`.

## Integration Notes

The current MVP uses one shared `Race` scene. `RaceManager` reads `GameSelection`, instantiates the selected track prefab and vehicle prefab, then binds the selected map's `Markers` child into `RaceSetup`.

The race is configured as a one-lap time trial for both maps. `FinishLine` is checkpoint index `0`, and the race can finish when the active vehicle returns to the finish trigger after leaving the start area and after the minimum finish time has elapsed.

If the race scene uses a different racing line, move only the marker transforms/trigger sizes in the map prefab or an integrated prefab variant. The `Checkpoint` component already enforces trigger mode through `OnValidate`.

For temporary finish-line validation, `RaceFinishDebugOverlay` can be enabled with PlayerPrefs key `super_racing.finish_debug_enabled = 1`. It is disabled by default so the debug text does not appear in normal gameplay.
