using System;
using UnityEngine;

namespace SuperRacing.Audio
{
    public enum AudioBus { Master, Music, Sfx, Ambience, UI }
    public enum AudioSnapshotId { Default, Countdown, Paused, Finish }
    public enum AudioCueId
    {
        CountdownTick, StartedGo, CheckpointPassed, LapChanged, Finished, NewRecord,
        InvalidCheckpoint, Restart, Respawn, Landing, CollisionLight, CollisionMedium, CollisionHeavy,
        UIHover, UIClick, UIConfirm, UIBack, UISelectionChanged, UIError, UIStartRace, UIResultsOpen
    }
    public enum MusicId { Menu, Race, Result }
    public enum SurfaceType { Asphalt, Sand, Grass }

    [Serializable]
    public struct VehicleAudioTelemetry
    {
        public float SpeedKmh;
        public float NormalizedRpm;
        public int CurrentGear;
        public float Throttle;
        public float Brake;
        public bool IsGrounded;
        public float ForwardSlip;
        public float SidewaysSlip;
        public SurfaceType CurrentSurface;
    }

    public interface IVehicleAudioTelemetrySource
    {
        VehicleAudioTelemetry AudioTelemetry { get; }
    }
}
