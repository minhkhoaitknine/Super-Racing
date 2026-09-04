using SuperRacing.Data;
using UnityEngine;

namespace SuperRacing.Contracts
{
    public interface IVehicleController
    {
        float SpeedKmh { get; }
        bool IsDrifting { get; }
        bool CanDrive { get; set; }

        void ApplyStats(CarDefinition stats);
        void ResetVehicle(Vector3 position, Quaternion rotation);
    }
}
