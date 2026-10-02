using System.Collections;
using GameOfLife.Configuration;
using GameOfLife.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Buttons
{
    /// <summary>Base for buttons that play the press animation and then perform their action; repeat taps during the animation are ignored.</summary>
    [RequireComponent(typeof(Button))]
    public abstract class AnimatedButton : MonoBehaviour
    {
        private Transform animatedTransform;
        private Coroutine pressRoutine;
        private GameSettings settings;

        /// <summary>Runs once the press animation has finished.</summary>
        protected abstract void OnPressed();

        /// <summary>Caches the transform and listens for clicks.</summary>
        protected virtual void Awake()
        {
            animatedTransform = transform;
            GetComponent<Button>().onClick.AddListener(StartPress);
        }

        /// <summary>Resets the scale if the button is hidden mid-animation so it never gets stuck enlarged.</summary>
        protected virtual void OnDisable()
        {
            if (pressRoutine == null)
            {
                return;
            }

            StopCoroutine(pressRoutine);
            pressRoutine = null;
            animatedTransform.localScale = Vector3.one;
        }

        /// <summary>Starts the press animation unless one is already playing.</summary>
        private void StartPress()
        {
            if (pressRoutine == null)
            {
                pressRoutine = StartCoroutine(PlayPressAnimationThenAct());
            }
        }

        /// <summary>Scales the button along the press curve from GameSettings, then calls OnPressed.</summary>
        private IEnumerator PlayPressAnimationThenAct()
        {
            if (!settings)
            {
                settings = ServiceLocator.Get<GameSettings>();
            }

            var duration = settings.ButtonPressDuration;
            var scaleCurve = settings.ButtonPressScale;
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                animatedTransform.localScale = Vector3.one * scaleCurve.Evaluate(elapsed / duration);
                yield return null;
            }

            animatedTransform.localScale = Vector3.one;
            pressRoutine = null;
            OnPressed();
        }
    }
}
