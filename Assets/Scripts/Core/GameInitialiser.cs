using System.Collections;
using DG.Tweening;
using DG.Tweening.Core.Enums;
using GameOfLife.Audio;
using GameOfLife.Configuration;
using GameOfLife.Effects;
using GameOfLife.Gameplay;
using GameOfLife.Purchasing;
using GameOfLife.Theming;
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
        private const string AdPassOwnershipStorageKey = "Store.AdPassOwned";
        private const int TweenerCapacity = 100;
        private const int SequenceCapacity = 10;

        [SerializeField] private GameSettings settings;
        [SerializeField] private SoundLibrary soundLibrary;
        [SerializeField] private Theme theme;
        [Tooltip("Clears the screen behind the game in the theme's background colour.")]
        [SerializeField] private Camera mainCamera;

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
        private ThemeService themeService;

        /// <summary>Applies platform settings, starts DOTween, creates the player's options and the theme, and registers services before any other script's Awake can run.</summary>
        private void Awake()
        {
            Application.targetFrameRate = settings.TargetFrameRate;
            DOTween.Init(recycleAllByDefault: false, useSafeMode: true, LogBehaviour.ErrorsOnly).SetCapacity(TweenerCapacity, SequenceCapacity);
            DOTween.safeModeLogBehaviour = SafeModeLogBehaviour.Error;
            options = new GameOptions(settings);
            adPassOwnership = new AdPassOwnership(AdPassOwnershipStorageKey);
            themeService = new ThemeService(theme, options.DarkModeEnabled, mainCamera);
            options.DarkModeEnabledChanged += themeService.SetDarkMode;
            RegisterServices();
        }

        /// <summary>Initialises every system in dependency order, waits one frame for load hitches to pass, then fades in.</summary>
        private IEnumerator Start()
        {
            InitialiseSystems();
            yield return null;
            screenFader.FadeIn(theme.StartupFade);
        }

        /// <summary>Empties the service registry so nothing points at destroyed objects after the scene unloads.</summary>
        private void OnDestroy()
        {
            if (options != null)
            {
                options.DarkModeEnabledChanged -= themeService.SetDarkMode;
            }

            themeService?.Dispose();
            ServiceLocator.Clear();
        }

        /// <summary>Makes each service reachable through the ServiceLocator.</summary>
        private void RegisterServices()
        {
            ServiceLocator.Register(settings);
            ServiceLocator.Register(themeService);
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
            screenNavigator.Initialise(theme);
            gameModeDirector.Initialise(settings, options, screenNavigator, audioManager, themeService);
            settingsPanel.Initialise(settings, options, audioManager, themeService);
            playPauseButton.Initialise(gameModeDirector);
            adPassButton.Initialise(purchaseManager, adPassOwnership);
            restorePurchasesButton.Initialise(purchaseManager, adPassOwnership);
            purchaseFeedback.Initialise(purchaseManager, adPassOwnership, audioManager);
        }
    }
}
