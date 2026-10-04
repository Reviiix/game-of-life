using System;
using DG.Tweening;
using GameOfLife.Motion;
using GameOfLife.Theming;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Screens
{
    /// <summary>A full-screen UI layer that fades and pops in when shown and fades out when hidden; once hidden its Canvas and raycaster are disabled, so it costs nothing to draw or hit-test.</summary>
    [RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
    public sealed class ScreenPanel : MonoBehaviour
    {
        private const float FadeInShareOfShow = 0.35f;

        [Tooltip("Optional: the card or column that pops up to full size as the screen appears.")]
        [SerializeField] private RectTransform poppedContent;
        [Tooltip("Optional: items that pop in one after another, such as menu buttons.")]
        [SerializeField] private RectTransform[] staggeredItems;

        private Canvas screenCanvas;
        private GraphicRaycaster screenRaycaster;
        private CanvasGroup canvasGroup;
        private Theme theme;
        private LayeredScale poppedContentScale;
        private LayeredScale[] staggeredItemScales;
        private ReplayableTween showTween;
        private ReplayableTween hideTween;
        private float showDuration;
        private float alphaAtHideStart;
        private bool isVisible;

        public event Action Shown;
        public event Action Hidden;

        /// <summary>Builds the show and hide tweens from the theme and finds what each animated item scales; until this runs, Show and Hide switch instantly.</summary>
        public void Initialise(Theme screenTheme)
        {
            CacheComponentsIfNeeded();
            theme = screenTheme;
            poppedContentScale = poppedContent ? LayeredScale.GetOrAdd(poppedContent.gameObject) : null;
            var itemCount = staggeredItems?.Length ?? 0;
            staggeredItemScales = new LayeredScale[itemCount];
            for (var itemIndex = 0; itemIndex < itemCount; itemIndex++)
            {
                staggeredItemScales[itemIndex] = LayeredScale.GetOrAdd(staggeredItems[itemIndex].gameObject);
            }

            showDuration = theme.ScreenShow.Duration + Mathf.Max(0, itemCount - 1) * theme.ScreenStaggerDelay;
            showTween = new ReplayableTween(new EasedMotion(showDuration, Ease.Linear), ShowShowProgress, RaiseShown);
            hideTween = new ReplayableTween(theme.ScreenHide, ShowHideProgress, FinishHiding);
        }

        /// <summary>Makes this screen visible and able to receive taps, fading and popping it in; raises Shown once it has fully appeared.</summary>
        public void Show()
        {
            SetVisible(true);
            if (showTween == null)
            {
                RaiseShown();
                return;
            }

            ShowShowProgress(0f);
            showTween.Play();
        }

        /// <summary>Stops this screen receiving taps at once and fades it out from wherever its fade is, then disables its canvas; raises Hidden straight away.</summary>
        public void Hide()
        {
            if (hideTween == null || !isVisible)
            {
                SetVisible(false);
            }
            else
            {
                isVisible = false;
                SetRaycastable(false);
                showTween.Stop();
                alphaAtHideStart = canvasGroup.alpha;
                hideTween.Play();
            }

            Hidden?.Invoke();
        }

        /// <summary>Shows or hides this screen at once, without animating or raising the Shown and Hidden events.</summary>
        public void SetVisible(bool visible)
        {
            CacheComponentsIfNeeded();
            showTween?.Stop();
            hideTween?.Stop();
            isVisible = visible;
            screenCanvas.enabled = visible;
            SetRaycastable(visible);
            canvasGroup.alpha = 1f;
            ResetScales();
        }

        /// <summary>Removes the transition tweens with the screen.</summary>
        private void OnDestroy()
        {
            showTween?.Kill();
            hideTween?.Kill();
        }

        /// <summary>Fades in quickly while the content and each staggered item pop up to full size in turn.</summary>
        private void ShowShowProgress(float linearProgress)
        {
            var elapsed = linearProgress * showDuration;
            var show = theme.ScreenShow;
            var fadeDuration = show.Duration * FadeInShareOfShow;
            canvasGroup.alpha = fadeDuration > 0f ? Mathf.Clamp01(elapsed / fadeDuration) : 1f;
            if (poppedContentScale)
            {
                poppedContentScale.SetRevealScale(GetPopScale(show, elapsed));
            }

            for (var itemIndex = 0; itemIndex < staggeredItemScales.Length; itemIndex++)
            {
                var itemElapsed = elapsed - itemIndex * theme.ScreenStaggerDelay;
                staggeredItemScales[itemIndex].SetRevealScale(GetPopScale(show, itemElapsed));
            }
        }

        /// <summary>Returns the eased scale of something that started popping in the given seconds ago; a zero-length pop is instantly full size.</summary>
        private float GetPopScale(EasedMotion show, float elapsed)
        {
            if (elapsed >= show.Duration)
            {
                return 1f;
            }

            if (elapsed <= 0f)
            {
                return theme.ScreenShowStartScale;
            }

            return Mathf.LerpUnclamped(theme.ScreenShowStartScale, 1f, show.Evaluate(elapsed, show.Duration));
        }

        /// <summary>Tells listeners the screen has finished appearing.</summary>
        private void RaiseShown()
        {
            Shown?.Invoke();
        }

        /// <summary>Fades the screen out from the alpha it had when hiding began.</summary>
        private void ShowHideProgress(float progress)
        {
            canvasGroup.alpha = Mathf.Lerp(alphaAtHideStart, 0f, progress);
        }

        /// <summary>Disables the faded-out screen's canvas so it costs nothing while hidden.</summary>
        private void FinishHiding()
        {
            screenCanvas.enabled = false;
            canvasGroup.alpha = 1f;
            ResetScales();
        }

        /// <summary>Puts the popped content and items back at full size.</summary>
        private void ResetScales()
        {
            if (poppedContentScale)
            {
                poppedContentScale.SetRevealScale(1f);
            }

            if (staggeredItemScales == null)
            {
                return;
            }

            foreach (var itemScale in staggeredItemScales)
            {
                itemScale.SetRevealScale(1f);
            }
        }

        /// <summary>Lets the screen receive taps or not; screens without a raycaster never do.</summary>
        private void SetRaycastable(bool raycastable)
        {
            if (screenRaycaster)
            {
                screenRaycaster.enabled = raycastable;
            }
        }

        /// <summary>Caches the canvas, raycaster and canvas group on first use, so it also works if the screen was saved inactive.</summary>
        private void CacheComponentsIfNeeded()
        {
            if (screenCanvas)
            {
                return;
            }

            screenCanvas = GetComponent<Canvas>();
            screenRaycaster = GetComponent<GraphicRaycaster>();
            canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
