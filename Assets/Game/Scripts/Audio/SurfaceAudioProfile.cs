using UnityEngine;

namespace SuperRacing.Audio
{
    [CreateAssetMenu(fileName = "SurfaceAudioProfile", menuName = "Super Racing/Audio/Surface Profile")]
    public sealed class SurfaceAudioProfile : ScriptableObject
    {
        public SurfaceType surface = SurfaceType.Asphalt;
        public AudioClip tireRoll;
        public AudioClip tireSkid;
        [Range(0f, 1f)] public float rollVolume = 0.35f;
        [Range(0f, 1f)] public float skidVolume = 0.7f;
        public float skidThreshold = 0.32f;
        public float pitchMultiplier = 1f;
    }
}
