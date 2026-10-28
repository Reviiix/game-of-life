using DG.Tweening;
using GameOfLife.Motion;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.Effects
{
    /// <summary>Full-screen black overlay that hides start-up, fades out with the theme's start-up fade, then switches itself off; it blocks taps while visible.</summary>
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

        /// <summary>Stops the fade if the overlay is destroyed first.</summary>
        private void OnDestroy()
        {
            DOTween.Kill(this);
        }

        /// <summary>Fades the overlay from opaque to transparent with the given motion, then deactivates it; runs once at start-up.</summary>
        public void FadeIn(EasedMotion motion)
        {
            var fade = DOTween.To(GetOverlayAlpha, SetOverlayAlpha, 0f, motion.Duration)
                .SetTarget(this)
                .OnComplete(Deactivate);
            motion.ApplyEaseTo(fade);
        }

        /// <summary>Returns the overlay's alpha for DOTween.</summary>
        private float GetOverlayAlpha()
        {
            return overlay.color.a;
        }

        /// <summary>Changes only the overlay's alpha, keeping its colour.</summary>
        private void SetOverlayAlpha(float alpha)
        {
            var overlayColour = overlay.color;
            overlayColour.a = alpha;
            overlay.color = overlayColour;
        }

        /// <summary>Switches the faded-out overlay off so it no longer draws or blocks taps.</summary>
        private void Deactivate()
        {
            gameObject.SetActive(false);
        }
    }
}
