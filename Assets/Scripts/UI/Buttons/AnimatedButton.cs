using GameOfLife.Audio;
using GameOfLife.Core;
using GameOfLife.Motion;
using GameOfLife.Theming;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Base for buttons that squeeze while held, click and spring back with a bounce, then perform their action; repeat taps during the spring are ignored.</summary>
    [RequireComponent(typeof(Button))]
    public abstract class AnimatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Button button;
        private GraphicRaycaster screenRaycaster;
        private LayeredScale layeredScale;
        private AudioManager audioManager;
        private ReplayableTween squeeze;
        private ReplayableTween springBack;
        private float pressedScale;
        private float shownPressScale = 1f;
        private float pressScaleAtTweenStart;
        private float pressScaleAtTweenEnd;
        private bool isActingAfterSpring;

        /// <summary>Runs once the spring-back animation has finished, or straight away for a button that acts immediately.</summary>
        protected abstract void OnPressed();

        /// <summary>Whether the button acts the moment it is clicked, still squeezing and springing, so quick repeat taps all count, as a colour cycler needs; most buttons wait for the spring to settle.</summary>
        protected virtual bool ActsImmediately => false;

        /// <summary>Caches the button, the raycaster of the screen it sits on and its layered scale, builds the press tweens from the ThemeService (registered before any Awake) and listens for clicks.</summary>
        protected virtual void Awake()
        {
            button = GetComponent<Button>();
            screenRaycaster = GetComponentInParent<GraphicRaycaster>(true);
            layeredScale = LayeredScale.GetOrAdd(gameObject);
            var theme = ServiceLocator.Get<ThemeService>().Theme;
            pressedScale = theme.ButtonPressedScale;
            squeeze = new ReplayableTween(theme.ButtonSqueeze, ShowPressProgress);
            springBack = new ReplayableTween(theme.ButtonRelease, ShowPressProgress, FinishSpringBack);
            button.onClick.AddListener(StartPress);
        }

        /// <summary>Resets the press if the button is hidden mid-animation so it never gets stuck squeezed or enlarged.</summary>
        protected virtual void OnDisable()
        {
            if (squeeze == null)
            {
                return;
            }

            squeeze.Stop();
            springBack.Stop();
            isActingAfterSpring = false;
            SetPressScale(1f);
        }

        /// <summary>Removes the press tweens with the button.</summary>
        protected virtual void OnDestroy()
        {
            squeeze?.Kill();
            springBack?.Kill();
        }

        /// <summary>Squeezes the button while it is held down.</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (isActingAfterSpring || !button.IsInteractable())
            {
                return;
            }

            springBack.Stop();
            AnimatePress(squeeze, pressedScale);
        }

        /// <summary>Springs back when released without a click, such as when the finger slides off or the button stopped being interactable while held.</summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (isActingAfterSpring)
            {
                return;
            }

            squeeze.Stop();
            AnimatePress(springBack, 1f);
        }

        /// <summary>Clicks and springs back from the squeezed size, acting once the spring settles, or at once for a button that acts immediately, unless a press is already being acted on.</summary>
        private void StartPress()
        {
            if (isActingAfterSpring)
            {
                return;
            }

            if (!audioManager)
            {
                audioManager = ServiceLocator.Get<AudioManager>();
            }

            audioManager.Play(SoundEffect.ButtonPress);
            isActingAfterSpring = !ActsImmediately;
            squeeze.Stop();
            SetPressScale(Mathf.Min(shownPressScale, pressedScale));
            AnimatePress(springBack, 1f);
            if (ActsImmediately)
            {
                OnPressed();
            }
        }

        /// <summary>Plays a press tween from the current press scale to the given one.</summary>
        private void AnimatePress(ReplayableTween tween, float targetScale)
        {
            pressScaleAtTweenStart = shownPressScale;
            pressScaleAtTweenEnd = targetScale;
            tween.Play();
        }

        /// <summary>Applies tween progress to the press scale; overshoot past 1 gives the bounce.</summary>
        private void ShowPressProgress(float progress)
        {
            SetPressScale(Mathf.LerpUnclamped(pressScaleAtTweenStart, pressScaleAtTweenEnd, progress));
        }

        /// <summary>Remembers and applies the press scale.</summary>
        private void SetPressScale(float scale)
        {
            shownPressScale = scale;
            layeredScale.SetPressScale(scale);
        }

        /// <summary>Acts on the click once the spring has settled; a spring from a cancelled press does nothing, and neither does one whose screen stopped taking taps meanwhile, such as a second menu button tapped before the first one's page opened.</summary>
        private void FinishSpringBack()
        {
            SetPressScale(1f);
            if (!isActingAfterSpring)
            {
                return;
            }

            isActingAfterSpring = false;
            if (screenRaycaster && !screenRaycaster.enabled)
            {
                return;
            }

            OnPressed();
        }
    }
}
