using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.Effects
{
    /// <summary>Full-screen black overlay that hides start-up, fades out, then switches itself off; it blocks taps while visible.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class ScreenFader : MonoBehaviour
    {
        private Image overlay;

        /// <summary>Makes the overlay fully opaque before the first frame is drawn.</summary>
        private void Awake()
        {
            overlay = GetComponent<Image>();
            SetOverlayAlpha(1f);
        }

        /// <summary>Fades the overlay from opaque to transparent over the given seconds, then deactivates it.</summary>
        public IEnumerator FadeIn(float duration)
        {
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                SetOverlayAlpha(1f - elapsed / duration);
                yield return null;
            }

            SetOverlayAlpha(0f);
            gameObject.SetActive(false);
        }

        /// <summary>Changes only the overlay's alpha, keeping its colour.</summary>
        private void SetOverlayAlpha(float alpha)
        {
            var overlayColour = overlay.color;
            overlayColour.a = alpha;
            overlay.color = overlayColour;
        }
    }
}
