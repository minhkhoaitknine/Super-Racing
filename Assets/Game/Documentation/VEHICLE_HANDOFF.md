# Vehicle Handoff

## Runtime Component

Use `SuperRacing.Vehicle.VehicleController` on the root GameObject of a vehicle prefab.

Public API currently available:

```csharp
public float SpeedKmh { get; }
public bool CanDrive { get; set; }
public void ResetVehicle(Vector3 position, Quaternion rotation);
```

`CanDrive = false` applies brake torque and blocks motor torque. This is intended for countdown, finish, and pause states.

`ResetVehicle` teleports the vehicle, clears linear/angular velocity, and resets the flip timer.

## Temporary Tuning

There is no shared `CarDefinition` contract in the project yet. Current tuning is serialized directly on `VehicleController`:

- `motorTorque`
- `brakeTorque`
- `maxSpeedKmh`
- `maxSteerAngle`
- `minSteerAngleAtTopSpeed`
- `lowSpeedSidewaysGrip`
- `highSpeedSidewaysGrip`

When Member B provides a `CarDefinition`, wire `ApplyStats(CarDefinition stats)` into this controller instead of creating a duplicate data class under the vehicle module.

## Camera

`SuperRacing.Vehicle.VehicleFollowCamera` is a simple custom follow camera. It auto-targets the first GameObject tagged `Player` when no target is assigned.
