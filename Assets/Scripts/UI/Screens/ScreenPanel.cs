using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Screens
{
    /// <summary>A full-screen UI layer that is hidden by disabling its Canvas and raycaster, so hidden screens cost nothing to draw or hit-test.</summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class ScreenPanel : MonoBehaviour
    {
        private Canvas screenCanvas;
        private GraphicRaycaster screenRaycaster;

        public bool IsVisible => screenCanvas.enabled;

        public event Action Hidden;

        /// <summary>Caches the canvas and raycaster that are toggled to show and hide this screen.</summary>
        private void Awake()
        {
            screenCanvas = GetComponent<Canvas>();
            screenRaycaster = GetComponent<GraphicRaycaster>();
        }

        /// <summary>Makes this screen visible and able to receive taps.</summary>
        public void Show()
        {
            SetVisible(true);
        }

        /// <summary>Hides this screen and stops it receiving taps.</summary>
        public void Hide()
        {
            SetVisible(false);
            Hidden?.Invoke();
        }

        /// <summary>Shows or hides this screen without raising the Hidden event.</summary>
        public void SetVisible(bool visible)
        {
            screenCanvas.enabled = visible;
            if (screenRaycaster)
            {
                screenRaycaster.enabled = visible;
            }
        }
    }
}
