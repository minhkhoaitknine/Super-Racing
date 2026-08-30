using UnityEngine;

namespace SuperRacing.Data
{
    [CreateAssetMenu(fileName = "TrackDefinition", menuName = "Super Racing/Track Definition")]
    public sealed class TrackDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string trackId = "track";
        [SerializeField] private string displayName = "New Track";
        [SerializeField] private string sceneName;
        [SerializeField] private Sprite previewSprite;

        [Header("Race Rules")]
        [Min(1)] [SerializeField] private int lapCount = 2;

        public string TrackId => trackId;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public Sprite PreviewSprite => previewSprite;
        public int LapCount => lapCount;

        private void OnValidate()
        {
            trackId = NormalizeId(trackId, "track");
            lapCount = Mathf.Max(1, lapCount);
        }

        private static string NormalizeId(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return value.Trim().ToLowerInvariant().Replace(' ', '_');
        }
    }
}
