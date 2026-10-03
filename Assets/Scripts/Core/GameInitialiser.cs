using System.Collections;
using GameOfLife.Audio;
using GameOfLife.Configuration;
using GameOfLife.Effects;
using GameOfLife.Gameplay;
using GameOfLife.Purchasing;
using GameOfLife.UI.Buttons;
using GameOfLife.UI.Screens;
using GameOfLife.UI.Store;
using UnityEngine;

namespace GameOfLife.Core
{
    /// <summary>The game's single entry point: registers every service, then initialises them in a fixed order behind the start-up fade.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameInitialiser : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;
        [SerializeField] private SoundLibrary soundLibrary;

        private const string AdPassOwnershipStorageKey = "Store.AdPassOwned";

        [Header("Services")]
        [SerializeField] private GameModeDirector gameModeDirector;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private ScreenNavigator screenNavigator;
        [SerializeField] private PurchaseManager purchaseManager;

        [Header("Start-up Dependents")]
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private PlayPauseButton playPauseButton;
        [SerializeField] private ScreenFader screenFader;
        [SerializeField] private AdPassButton adPassButton;
        [SerializeField] private RestorePurchasesButton restorePurchasesButton;
        [SerializeField] private PurchaseFeedback purchaseFeedback;

        private GameOptions options;
        private AdPassOwnership adPassOwnership;

        /// <summary>Applies platform settings, creates the player's options and registers services before any other script's Awake can run.</summary>
        private void Awake()
        {
            Application.targetFrameRate = settings.TargetFrameRate;
            options = new GameOptions(settings);
            adPassOwnership = new AdPassOwnership(AdPassOwnershipStorageKey);
            RegisterServices();
        }

        /// <summary>Initialises every system in dependency order, waits one frame for load hitches to pass, then fades in.</summary>
        private IEnumerator Start()
        {
            InitialiseSystems();
            yield return null;
            yield return screenFader.FadeIn(settings.StartupFadeDuration);
        }

        /// <summary>Empties the service registry so nothing points at destroyed objects after the scene unloads.</summary>
        private void OnDestroy()
        {
            ServiceLocator.Clear();
        }

        /// <summary>Makes each service reachable through the ServiceLocator.</summary>
        private void RegisterServices()
        {
            ServiceLocator.Register(settings);
            ServiceLocator.Register(gameModeDirector);
            ServiceLocator.Register(audioManager);
            ServiceLocator.Register(screenNavigator);
            ServiceLocator.Register(purchaseManager);
            ServiceLocator.Register(adPassOwnership);
        }

        /// <summary>Sets up systems so each one's dependencies are ready before it starts.</summary>
        private void InitialiseSystems()
        {
            audioManager.Initialise(soundLibrary, options);
            purchaseManager.Initialise(settings.AdPassProductId, adPassOwnership);
            screenNavigator.Initialise();
            gameModeDirector.Initialise(settings, options, screenNavigator, audioManager);
            settingsPanel.Initialise(settings, options, audioManager);
            playPauseButton.Initialise(gameModeDirector);
            adPassButton.Initialise(purchaseManager, adPassOwnership);
            restorePurchasesButton.Initialise(purchaseManager, adPassOwnership);
            purchaseFeedback.Initialise(purchaseManager, adPassOwnership, audioManager);
        }
    }
}
