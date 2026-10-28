using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace GameOfLife.Ads
{
    /// <summary>Starts the LevelPlay SDK once, for non-personalised ads only, and tells listeners when it is ready to load ads.</summary>
    public sealed class LevelPlayInitialiser : MonoBehaviour
    {
        private bool hasStarted;

        public bool IsInitialised { get; private set; }

        public event Action Initialised;

        /// <summary>Asks for non-personalised ads, registers the callbacks, then initialises LevelPlay with the platform's app key; later calls do nothing.</summary>
        public void Initialise(AdSettings settings)
        {
            if (hasStarted)
            {
                return;
            }

            hasStarted = true;
            // Privacy choices must be set before Init: no consent to personalised ads, and opted out of data sale.
            LevelPlayPrivacySettings.SetGDPRConsent(false);
            LevelPlayPrivacySettings.SetCCPA(true);
            LevelPlay.OnInitSuccess += HandleInitSuccess;
            LevelPlay.OnInitFailed += HandleInitFailed;
            LevelPlay.Init(settings.AppKey);
        }

        /// <summary>Stops listening to LevelPlay when destroyed.</summary>
        private void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;
        }

        /// <summary>Marks the SDK ready and tells listeners they can create and load ads.</summary>
        private void HandleInitSuccess(LevelPlayConfiguration configuration)
        {
            Debug.Log("LevelPlay SDK initialized successfully");
            IsInitialised = true;
            Initialised?.Invoke();
        }

        /// <summary>Logs why LevelPlay failed to start; the game carries on without ads.</summary>
        private void HandleInitFailed(LevelPlayInitError error)
        {
            Debug.LogError($"LevelPlay initialization failed: {error.ErrorMessage}");
        }
    }
}
