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

These markers are intentionally suggestions, not final race logic. If the race scene uses a different racing line, move only the marker transforms/trigger sizes in the integrated scene or prefab variant. The `Checkpoint` component already enforces trigger mode through `OnValidate`.

No changes were made to `Assets/Scripts/Race`, `Assets/Scripts/Data`, `Race_Beach`, or `Race_Desert`.
