using GameOfLife.Theming;
using TMPro;
using UnityEngine;

namespace GameOfLife.UI.Patterns
{
    /// <summary>Classic's positioning hint: a label above the board that pops whenever its text changes and hides when positioning ends.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class PlacementHintLabel : MonoBehaviour
    {
        private TMP_Text label;
        private LabelPop pop;
        private string shownHint;

        /// <summary>Builds the pop animation from the theme and starts hidden.</summary>
        public void Initialise(Theme theme)
        {
            label = GetComponent<TMP_Text>();
            pop = new LabelPop(transform, theme);
            gameObject.SetActive(false);
        }

        /// <summary>Shows a precomputed hint, popping only when it differs from the one already shown.</summary>
        public void Show(string hint)
        {
            gameObject.SetActive(true);
            if (ReferenceEquals(hint, shownHint))
            {
                return;
            }

            shownHint = hint;
            label.SetText(hint);
            pop.Play();
        }

        /// <summary>Hides the hint and forgets its text, so the next one pops in.</summary>
        public void Hide()
        {
            shownHint = null;
            gameObject.SetActive(false);
        }

        /// <summary>Removes the pop tween with the label.</summary>
        private void OnDestroy()
        {
            pop?.Kill();
        }
    }
}
