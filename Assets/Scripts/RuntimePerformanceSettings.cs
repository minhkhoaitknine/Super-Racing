using UnityEngine;

namespace SuperRacing
{
    public static class RuntimePerformanceSettings
    {
        private const int TargetFrameRate = 60;
        private const float MaximumDeltaTime = 0.1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            QualitySettings.vSyncCount = Application.isMobilePlatform ? 0 : 1;
            Application.targetFrameRate = Application.isMobilePlatform ? TargetFrameRate : -1;
            Time.maximumDeltaTime = MaximumDeltaTime;
        }
    }
}
