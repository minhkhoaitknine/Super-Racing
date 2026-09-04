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
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            Time.maximumDeltaTime = MaximumDeltaTime;
        }
    }
}
