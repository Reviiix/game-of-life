using System.Collections;
using GameOfLife.Configuration;
using GameOfLife.Effects;
using GameOfLife.Gameplay;
using GameOfLife.UI.Buttons;
using GameOfLife.UI.Screens;
using UnityEngine;

namespace GameOfLife.Core
{
    /// <summary>The game's single entry point: registers every service, then initialises them in a fixed order behind the start-up fade.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameInitialiser : MonoBehaviour
    {
        [SerializeField] private GameSettings settings;

        [Header("Services")]
        [SerializeField] private GameController gameController;
        [SerializeField] private ScreenNavigator screenNavigator;

        [Header("Start-up Dependents")]
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private PlayPauseButton playPauseButton;
        [SerializeField] private ScreenFader screenFader;

        /// <summary>Applies platform settings and registers services before any other script's Awake can run.</summary>
        private void Awake()
        {
            Application.targetFrameRate = settings.TargetFrameRate;
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
            ServiceLocator.Register(gameController);
            ServiceLocator.Register(screenNavigator);
        }

        /// <summary>Sets up systems so each one's dependencies are ready before it starts.</summary>
        private void InitialiseSystems()
        {
            screenNavigator.Initialise();
            gameController.Initialise(settings, screenNavigator);
            settingsPanel.Initialise(settings, gameController);
            playPauseButton.Initialise(gameController);
        }
    }
}
