using GameOfLife.Audio;
using GameOfLife.Core;
using GameOfLife.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Clicks and hides the screen this button belongs to, instantly and without the press animation, so dialog prefabs need no wiring.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class CloseScreenButton : MonoBehaviour
    {
        private ScreenPanel owningScreen;
        private AudioManager audioManager;

        /// <summary>Finds the screen this button sits in and listens for clicks.</summary>
        private void Awake()
        {
            owningScreen = GetComponentInParent<ScreenPanel>(true);
            GetComponent<Button>().onClick.AddListener(CloseScreen);
        }

        /// <summary>Plays the click and hides the owning screen.</summary>
        private void CloseScreen()
        {
            if (!audioManager)
            {
                audioManager = ServiceLocator.Get<AudioManager>();
            }

            audioManager.Play(SoundEffect.ButtonPress);
            owningScreen.Hide();
        }
    }
}
