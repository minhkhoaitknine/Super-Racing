# Vehicle Handoff

## Runtime Component

Use `SuperRacing.Vehicle.VehicleController` on the root GameObject of a vehicle prefab.

Public API currently available:

```csharp
public float SpeedKmh { get; }
public bool CanDrive { get; set; }
public void ApplyStats(CarDefinition stats);
public void ResetVehicle(Vector3 position, Quaternion rotation);
```

`CanDrive = false` applies brake torque and blocks motor torque. This is intended for countdown, finish, and pause states.

`ResetVehicle` teleports the vehicle, clears linear/angular velocity, and resets the flip timer.

## Car Stats

`VehicleController` implements `SuperRacing.Contracts.IVehicleController` and accepts `SuperRacing.Data.CarDefinition`.

`ApplyStats(CarDefinition stats)` maps the shared data contract into WheelCollider tuning:

- `motorTorque`
- `brakeTorque`
- `maxSpeedKmh`
- `maxSteerAngle`
- `minSteerAngleAtTopSpeed`
- `lowSpeedSidewaysGrip`
- `highSpeedSidewaysGrip`

The `CarDefinition.Grip` value is treated as a baseline and converted into low-speed/high-speed sideways friction stiffness.

## Vehicle Prefabs

Three tuned vehicle prefabs are available:

| Role | Prefab | Source body | Default tuning |
| --- | --- | --- | --- |
| Speedster | `Assets/Game/Prefabs/Vehicles/Speedster.prefab` | Sport body, red paint, Wheel_H | 170 km/h, 1900 motor torque, 24 steering, lower high-speed grip |
| Balanced | `Assets/Game/Prefabs/Vehicles/Balanced.prefab` | Sedan body, yellow paint, Wheel_A | 140 km/h, 1600 motor torque, 30 steering, medium grip |
| Control | `Assets/Game/Prefabs/Vehicles/Control.prefab` | Compact body, blue paint, Wheel_C | 115 km/h, 1700 motor torque, 38 steering, high grip |

All three prefabs keep the dynamic physics setup on the root: `Rigidbody`, simple `BoxCollider`, `VehicleController`, `WheelVisualSync`, four child `WheelCollider` objects, and separate visual wheel meshes.

## Camera

`SuperRacing.Vehicle.VehicleFollowCamera` is a simple custom follow camera. It auto-targets the first GameObject tagged `Player` when no target is assigned.
