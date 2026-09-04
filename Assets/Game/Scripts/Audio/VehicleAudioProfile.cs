using UnityEngine;

namespace SuperRacing.Audio
{
    [CreateAssetMenu(fileName = "VehicleAudioProfile", menuName = "Super Racing/Audio/Vehicle Profile")]
    public sealed class VehicleAudioProfile : ScriptableObject
    {
        public string displayName = "Balanced";
        [Header("One shots")]
        public AudioClip engineStart;
        public AudioClip gearShift;
        public AudioClip backfire;
        public AudioClip[] gearShiftVariants;
        public AudioClip[] backfireVariants;
        [Header("RPM loops")]
        public AudioClip idle;
        public AudioClip lowRpm;
        public AudioClip midRpm;
        public AudioClip highRpm;
        public AudioClip onLoad;
        public AudioClip offLoad;
        [Header("Tuning")]
        [Range(1, 8)] public int gearCount = 5;
        public float maxSpeedKmh = 140f;
        public float minPitch = 0.72f;
        public float maxPitch = 1.65f;
        public float shiftDuration = 0.16f;
        [Range(0f, 1f)] public float engineVolume = 0.65f;
        [Range(0f, 1f)] public float loadVolume = 0.28f;
        [Range(0f, 1f)] public float backfireThrottleDrop = 0.62f;
        [Range(0f, 1f)] public float backfireMinimumRpm = 0.7f;
        public AnimationCurve rpmFromSpeed = AnimationCurve.Linear(0f, 0.1f, 1f, 1f);

        public int GearForSpeed(float speedKmh)
        {
            float normalized = Mathf.Clamp01(speedKmh / Mathf.Max(1f, maxSpeedKmh));
            return Mathf.Clamp(Mathf.FloorToInt(normalized * gearCount) + 1, 1, gearCount);
        }

        public float RpmForSpeed(float speedKmh, int gear, float throttle)
        {
            float gearWidth = maxSpeedKmh / Mathf.Max(1, gearCount);
            float withinGear = Mathf.Repeat(Mathf.Max(0f, speedKmh), gearWidth) / gearWidth;
            if (speedKmh >= maxSpeedKmh) withinGear = 1f;
            return Mathf.Clamp01(Mathf.Lerp(0.2f, 1f, rpmFromSpeed.Evaluate(withinGear)) + Mathf.Clamp01(throttle) * 0.08f);
        }
    }
}
