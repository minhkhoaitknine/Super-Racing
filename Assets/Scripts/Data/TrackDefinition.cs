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
        [SerializeField] private GameObject previewPrefab;

        [Header("Race Rules")]
        [Min(1)] [SerializeField] private int lapCount = 2;
        [SerializeField] private bool requireOrderedCheckpoints = true;

        [Header("Economy Rewards")]
        [Min(0)] [SerializeField] private int completionReward = 600;
        [Min(0)] [SerializeField] private int newRecordBonus = 200;
        [Min(0)] [SerializeField] private int driftCoinsPerSecond = 20;
        [Min(0)] [SerializeField] private int maximumDriftReward = 500;

        public string TrackId => trackId;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public Sprite PreviewSprite => previewSprite;
        public GameObject PreviewPrefab => previewPrefab;
        public int LapCount => lapCount;
        public bool RequireOrderedCheckpoints => requireOrderedCheckpoints;
        public int CompletionReward => completionReward;
        public int NewRecordBonus => newRecordBonus;
        public int DriftCoinsPerSecond => driftCoinsPerSecond;
        public int MaximumDriftReward => maximumDriftReward;

        private void OnValidate()
        {
            trackId = NormalizeId(trackId, "track");
            lapCount = Mathf.Max(1, lapCount);
            completionReward = Mathf.Max(0, completionReward);
            newRecordBonus = Mathf.Max(0, newRecordBonus);
            driftCoinsPerSecond = Mathf.Max(0, driftCoinsPerSecond);
            maximumDriftReward = Mathf.Max(0, maximumDriftReward);
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
