using System;
using System.Collections;
using GameOfLife.Gameplay;
using GameOfLife.Purchasing;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace GameOfLife.Ads
{
    /// <summary>Shows LevelPlay interstitials at app start, before every game except the session's first, and every interval at a moment when no game is being played; shows none once the ad pass is owned.</summary>
    public sealed class AdManager : MonoBehaviour, IAdBreaks
    {
        private AdSettings settings;
        private LevelPlayInitialiser initialiser;
        private AdPassOwnership adPassOwnership;
        private GameModeDirector gameModeDirector;
        private AdSchedule schedule;
        private LevelPlayInterstitialAd interstitialAd;
        private WaitForSecondsRealtime timedAdWait;
        private WaitForSecondsRealtime loadRetryWait;
        private Coroutine timedAdRoutine;
        private Coroutine loadRetryRoutine;
        private Action pendingGameStart;
        private bool isShowingAd;
        private bool isHeldBack;
        private float startTime;

        private bool AreAdsEnabled => !adPassOwnership.IsOwned;
        private bool IsPlaying => gameModeDirector.IsSimulationActive;
        private bool IsAdReady => interstitialAd != null && interstitialAd.IsAdReady();
        private float Now => Time.realtimeSinceStartup - startTime;

        /// <summary>LevelPlay's Editor mock ad closes from an IMGUI button, which the Device Simulator only clicks through the legacy Input Manager, so with the Input System alone it could never close there.</summary>
#if UNITY_EDITOR && !ENABLE_LEGACY_INPUT_MANAGER
        private static bool IsMockAdUnclosable => UnityEditor.PlayModeWindow.GetViewType() == UnityEditor.PlayModeWindow.PlayModeViewTypes.SimulatorView;
#else
        private static bool IsMockAdUnclosable => false;
#endif

        /// <summary>Starts the ad schedule and, unless the ad pass is owned, creates and loads the interstitial once LevelPlay is ready.</summary>
        public void Initialise(AdSettings adSettings, LevelPlayInitialiser levelPlayInitialiser, AdPassOwnership ownership, GameModeDirector director)
        {
            startTime = Time.realtimeSinceStartup;
            settings = adSettings;
            initialiser = levelPlayInitialiser;
            adPassOwnership = ownership;
            gameModeDirector = director;
            schedule = new AdSchedule(settings.StartupWindowSeconds, settings.TimedIntervalSeconds, settings.MinimumSecondsBetweenAds);
            timedAdWait = new WaitForSecondsRealtime(settings.TimedIntervalSeconds);
            loadRetryWait = new WaitForSecondsRealtime(settings.LoadRetrySeconds);
            adPassOwnership.OwnershipChanged += HandleAdPassOwnershipChanged;
            gameModeDirector.SimulationActivityChanged += HandleSimulationActivityChanged;
            initialiser.Initialised += CreateInterstitial;
            if (AreAdsEnabled)
            {
                EnableAds();
            }
        }

        /// <summary>Keeps every ad back, such as while the first-run guide is open; the start-up ad still counts its window from launch.</summary>
        public void HoldBack()
        {
            isHeldBack = true;
        }

        /// <summary>Lets ads show again: the start-up ad if it is still within its window and loaded, otherwise a timed ad that fell due meanwhile.</summary>
        public void Release()
        {
            if (!isHeldBack)
            {
                return;
            }

            isHeldBack = false;
            if (AreAdsEnabled && !isShowingAd && IsAdReady && schedule.IsStartupAdDue(Now, IsPlaying))
            {
                ShowAd();
                return;
            }

            TryShowTimedAd();
        }

        /// <summary>Shows the pre-game ad if one is due and ready, then starts the game; otherwise starts it at once.</summary>
        public void RunBeforeGame(Action startGame)
        {
            if (isShowingAd)
            {
                pendingGameStart = startGame;
                return;
            }

            var isAdDue = schedule.ShouldShowBeforeGame(Now);
            if (!AreAdsEnabled || !isAdDue || !IsAdReady)
            {
                startGame();
                return;
            }

            pendingGameStart = startGame;
            ShowAd();
        }

        /// <summary>Stops listening to the game and LevelPlay, and frees the native ad.</summary>
        private void OnDestroy()
        {
            if (adPassOwnership != null)
            {
                adPassOwnership.OwnershipChanged -= HandleAdPassOwnershipChanged;
            }

            if (gameModeDirector)
            {
                gameModeDirector.SimulationActivityChanged -= HandleSimulationActivityChanged;
            }

            if (initialiser)
            {
                initialiser.Initialised -= CreateInterstitial;
            }

            DestroyInterstitial();
        }

        /// <summary>Starts LevelPlay if needed, creates the interstitial once it is ready, and starts waiting for the next timed ad.</summary>
        private void EnableAds()
        {
            initialiser.Initialise(settings);
            if (initialiser.IsInitialised)
            {
                CreateInterstitial();
            }

            ScheduleTimedAd();
        }

        /// <summary>Creates the interstitial for this platform's ad unit, listens to it and loads the first ad.</summary>
        private void CreateInterstitial()
        {
            if (interstitialAd != null || !AreAdsEnabled)
            {
                return;
            }

            interstitialAd = new LevelPlayInterstitialAd(settings.InterstitialAdUnitId);
            interstitialAd.OnAdLoaded += HandleAdLoaded;
            interstitialAd.OnAdLoadFailed += HandleAdLoadFailed;
            interstitialAd.OnAdDisplayed += HandleAdDisplayed;
            interstitialAd.OnAdDisplayFailed += HandleAdDisplayFailed;
            interstitialAd.OnAdClosed += HandleAdClosed;
            LoadAd();
        }

        /// <summary>Stops listening to the interstitial and frees it, if one exists.</summary>
        private void DestroyInterstitial()
        {
            if (interstitialAd == null)
            {
                return;
            }

            interstitialAd.OnAdLoaded -= HandleAdLoaded;
            interstitialAd.OnAdLoadFailed -= HandleAdLoadFailed;
            interstitialAd.OnAdDisplayed -= HandleAdDisplayed;
            interstitialAd.OnAdDisplayFailed -= HandleAdDisplayFailed;
            interstitialAd.OnAdClosed -= HandleAdClosed;
            interstitialAd.DestroyAd();
            interstitialAd = null;
        }

        /// <summary>Requests the next interstitial.</summary>
        private void LoadAd()
        {
            if (interstitialAd != null && AreAdsEnabled)
            {
                Debug.Log("Loading interstitial ad");
                interstitialAd.LoadAd();
            }
        }

        /// <summary>Shows the loaded interstitial and restarts the timed interval; in the Device Simulator the unclosable mock ad is skipped as if closed at once.</summary>
        private void ShowAd()
        {
            isShowingAd = true;
            schedule.RecordAdShown(Now);
            ScheduleTimedAd();
            if (IsMockAdUnclosable)
            {
                Debug.Log("Skipping the mock interstitial: the Device Simulator cannot click its Close button while only the Input System handles input. Use the Game view to see it.");
                FinishAd();
                return;
            }

            Debug.Log("Showing interstitial ad");
            interstitialAd.ShowAd();
        }

        /// <summary>Shows the start-up ad if it is still wanted, or a timed ad that was waiting for one to load; nothing while ads are held back.</summary>
        private void HandleAdLoaded(LevelPlayAdInfo adInfo)
        {
            Debug.Log("Interstitial ad loaded");
            if (isHeldBack)
            {
                return;
            }

            if (schedule.IsStartupAdDue(Now, IsPlaying) && !isShowingAd)
            {
                ShowAd();
                return;
            }

            TryShowTimedAd();
        }

        /// <summary>Logs the failure and tries loading again after a pause.</summary>
        private void HandleAdLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"Interstitial ad failed to load: {error.ErrorMessage}");
            if (loadRetryRoutine == null && AreAdsEnabled)
            {
                loadRetryRoutine = StartCoroutine(LoadAgainAfterDelay());
            }
        }

        /// <summary>Logs that the interstitial is on screen.</summary>
        private void HandleAdDisplayed(LevelPlayAdInfo adInfo)
        {
            Debug.Log("Interstitial ad displayed");
        }

        /// <summary>Logs the failure, then carries on as if the ad had closed so the game is never held up.</summary>
        private void HandleAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            Debug.LogError($"Interstitial ad failed to display: {error.ErrorMessage}");
            FinishAd();
        }

        /// <summary>Loads the next ad and starts any game that was waiting for this one.</summary>
        private void HandleAdClosed(LevelPlayAdInfo adInfo)
        {
            Debug.Log("Interstitial ad closed");
            FinishAd();
        }

        /// <summary>Ends the ad break: loads the next ad and starts the game that was waiting, if any.</summary>
        private void FinishAd()
        {
            isShowingAd = false;
            LoadAd();
            StartPendingGame();
        }

        /// <summary>Starts the game that was waiting for an ad break, if any.</summary>
        private void StartPendingGame()
        {
            var startGame = pendingGameStart;
            pendingGameStart = null;
            startGame?.Invoke();
        }

        /// <summary>Shows a timed ad that fell due while a game was being played, now that play has stopped.</summary>
        private void HandleSimulationActivityChanged(bool isActive)
        {
            if (!isActive)
            {
                TryShowTimedAd();
            }
        }

        /// <summary>Shows the timed ad if it is due, ads are not held back, no game is being played, and an ad is ready; otherwise the next load, pause or release tries again.</summary>
        private void TryShowTimedAd()
        {
            if (AreAdsEnabled && !isHeldBack && !isShowingAd && IsAdReady && schedule.IsTimedAdDue(Now, IsPlaying))
            {
                ShowAd();
            }
        }

        /// <summary>Restarts the wait for the next timed ad from the schedule.</summary>
        private void ScheduleTimedAd()
        {
            if (timedAdRoutine != null)
            {
                StopCoroutine(timedAdRoutine);
            }

            timedAdRoutine = StartCoroutine(ShowTimedAdWhenDue());
        }

        /// <summary>Waits until the timed ad falls due, then tries to show it.</summary>
        private IEnumerator ShowTimedAdWhenDue()
        {
            timedAdWait.waitTime = Mathf.Max(0f, schedule.NextTimedAdTime - Now);
            yield return timedAdWait;
            timedAdRoutine = null;
            TryShowTimedAd();
        }

        /// <summary>Waits the retry delay, then loads again.</summary>
        private IEnumerator LoadAgainAfterDelay()
        {
            yield return loadRetryWait;
            loadRetryRoutine = null;
            LoadAd();
        }

        /// <summary>Turns every ad off once the ad pass is owned, starting any game that was waiting, or back on if the purchase is refunded.</summary>
        private void HandleAdPassOwnershipChanged(bool isOwned)
        {
            if (!isOwned)
            {
                EnableAds();
                return;
            }

            if (timedAdRoutine != null)
            {
                StopCoroutine(timedAdRoutine);
                timedAdRoutine = null;
            }

            if (loadRetryRoutine != null)
            {
                StopCoroutine(loadRetryRoutine);
                loadRetryRoutine = null;
            }

            DestroyInterstitial();
            isShowingAd = false;
            StartPendingGame();
        }
    }
}
