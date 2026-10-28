using UnityEngine;

namespace GameOfLife.Ads
{
    /// <summary>Every ad setting in one asset: the LevelPlay app keys and interstitial ad unit IDs per platform, and when ads show.</summary>
    [CreateAssetMenu(fileName = "AdSettings", menuName = "Game of Life/Ad Settings")]
    public sealed class AdSettings : ScriptableObject
    {
        [Header("LevelPlay Credentials (the defaults are the test IDs from the Ads Mediation package sample; replace them before release)")]
        [Tooltip("App key from the LevelPlay dashboard for the Android app.")]
        [SerializeField] private string androidAppKey = "25b63cf85";
        [Tooltip("Interstitial ad unit ID from the LevelPlay dashboard for the Android app.")]
        [SerializeField] private string androidInterstitialAdUnitId = "h3xw38h9214adgxo";
        [Tooltip("App key from the LevelPlay dashboard for the iOS app.")]
        [SerializeField] private string iosAppKey = "25c43a4a5";
        [Tooltip("Interstitial ad unit ID from the LevelPlay dashboard for the iOS app.")]
        [SerializeField] private string iosInterstitialAdUnitId = "obg6ohwts3y690ks";

        [Header("Timing (real-time seconds)")]
        [Tooltip("The start-up ad shows only if it has loaded within this long of the app starting; later, it is skipped rather than interrupting.")]
        [Min(0f)] [SerializeField] private float startupWindowSeconds = 10f;
        [Tooltip("A timed ad falls due this long after the last ad, and waits for a moment when no game is being played.")]
        [Min(1f)] [SerializeField] private float timedIntervalSeconds = 1800f;
        [Tooltip("An ad before a game is skipped if another ad showed less than this long ago.")]
        [Min(0f)] [SerializeField] private float minimumSecondsBetweenAds = 120f;
        [Tooltip("How long to wait before loading again after an ad fails to load.")]
        [Min(1f)] [SerializeField] private float loadRetrySeconds = 30f;

        public string AndroidAppKey => androidAppKey;
        public string AndroidInterstitialAdUnitId => androidInterstitialAdUnitId;
        public string IosAppKey => iosAppKey;
        public string IosInterstitialAdUnitId => iosInterstitialAdUnitId;
        public float StartupWindowSeconds => startupWindowSeconds;
        public float TimedIntervalSeconds => timedIntervalSeconds;
        public float MinimumSecondsBetweenAds => minimumSecondsBetweenAds;
        public float LoadRetrySeconds => loadRetrySeconds;

        /// <summary>The app key for the platform being built.</summary>
        public string AppKey =>
#if UNITY_IOS
            iosAppKey;
#else
            androidAppKey;
#endif

        /// <summary>The interstitial ad unit ID for the platform being built.</summary>
        public string InterstitialAdUnitId =>
#if UNITY_IOS
            iosInterstitialAdUnitId;
#else
            androidInterstitialAdUnitId;
#endif
    }
}
