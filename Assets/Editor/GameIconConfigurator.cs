using UnityEditor;
using UnityEngine;

namespace SuperRacing.Editor
{
    public static class GameIconConfigurator
    {
        private const string IconPath = "Assets/UI/Branding/SuperRacingIcon.png";

        [InitializeOnLoadMethod]
        private static void ScheduleConfiguration()
        {
            EditorApplication.delayCall += Configure;
        }

        [MenuItem("Super Racing/Configure Game Icon")]
        public static void Configure()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return;

            SetIcon(BuildTargetGroup.Unknown, icon);
            SetIcon(BuildTargetGroup.Standalone, icon);
            SetIcon(BuildTargetGroup.Android, icon);
            SetIcon(BuildTargetGroup.iOS, icon);
        }

        private static void SetIcon(BuildTargetGroup group, Texture2D icon)
        {
            Texture2D[] current = PlayerSettings.GetIconsForTargetGroup(group);
            if (current != null && current.Length > 0 && current[0] == icon) return;
            PlayerSettings.SetIconsForTargetGroup(group, new[] { icon });
        }
    }
}
