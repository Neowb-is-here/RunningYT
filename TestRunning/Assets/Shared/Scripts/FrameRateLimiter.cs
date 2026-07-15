using UnityEngine;

namespace HyperCasual.Runner
{
    public static class FrameRateLimiter
    {
        const int k_TargetFrameRate = 120;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyBeforeSceneLoad()
        {
            Apply();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ApplyAfterSceneLoad()
        {
            Apply();
        }

        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = k_TargetFrameRate;
        }
    }
}
