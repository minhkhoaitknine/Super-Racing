using UnityEngine;

namespace SuperRacing.Audio
{
    [CreateAssetMenu(fileName = "MapAudioProfile", menuName = "Super Racing/Map Audio Profile")]
    public sealed class MapAudioProfile : ScriptableObject
    {
        public string displayName = "Map";
        public AudioClip primaryAmbience;
        public AudioClip secondaryAmbience;
        [Range(0f, 1f)] public float primaryVolume = 0.35f;
        [Range(0f, 1f)] public float secondaryVolume = 0.15f;
    }
}
