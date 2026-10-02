using GameOfLife.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Hides the screen this button belongs to, so dialog prefabs need no extra wiring.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class CloseScreenButton : MonoBehaviour
    {
        private ScreenPanel owningScreen;

        /// <summary>Finds the screen this button sits in and listens for clicks.</summary>
        private void Awake()
        {
            owningScreen = GetComponentInParent<ScreenPanel>(true);
            GetComponent<Button>().onClick.AddListener(owningScreen.Hide);
        }
    }
}
