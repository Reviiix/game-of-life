using System;
using GameOfLife.Audio;
using GameOfLife.Core;
using GameOfLife.Patterns;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Patterns
{
    /// <summary>One tappable catalogue row, for a pattern or for random placement: thumbnail, name and one-line description; greys itself and says why when it cannot be used now.</summary>
    [RequireComponent(typeof(Button), typeof(CanvasGroup))]
    public sealed class PatternRow : MonoBehaviour
    {
        private const string TooBigForBoardText = "TOO BIG FOR THIS BOARD";
        private const string TooBigForHalfText = "TOO BIG FOR YOUR HALF";
        private const string NotEnoughSquaresText = "NOT ENOUGH SQUARES LEFT";
        private const string NoEmptyCellsText = "NO EMPTY CELLS LEFT";

        [SerializeField] private RawImage thumbnail;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [Tooltip("How faint a pattern that cannot be placed right now is drawn.")]
        [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.4f;
        [Tooltip("The square the thumbnail fits inside, in canvas units.")]
        [SerializeField, Min(1f)] private float thumbnailBox = 112f;

        private Button button;
        private CanvasGroup canvasGroup;
        private string description;
        private int rowIndex;

        public event Action<int> Chosen;

        /// <summary>Shows the thumbnail, name and description and starts reporting taps with the given index.</summary>
        public void Initialise(int index, string rowName, string rowDescription, Texture2D thumbnailTexture)
        {
            button = GetComponent<Button>();
            canvasGroup = GetComponent<CanvasGroup>();
            rowIndex = index;
            description = rowDescription;
            nameLabel.SetText(rowName);
            descriptionLabel.SetText(description);
            thumbnail.texture = thumbnailTexture;
            thumbnail.rectTransform.sizeDelta = FitInBox(thumbnailTexture.width, thumbnailTexture.height);
            button.onClick.AddListener(Choose);
        }

        /// <summary>Changes the description shown while the row is available, such as random placement's wording for each mode.</summary>
        public void SetDescription(string rowDescription)
        {
            description = rowDescription;
        }

        /// <summary>Lets the row be tapped, or fades it and replaces the description with the reason it cannot be used.</summary>
        public void SetAvailable(PatternUnavailableReason reason)
        {
            var available = reason == PatternUnavailableReason.None;
            canvasGroup.alpha = available ? 1f : unavailableAlpha;
            canvasGroup.interactable = available;
            descriptionLabel.SetText(available ? description : GetReasonText(reason));
        }

        /// <summary>Stops listening to the button when destroyed.</summary>
        private void OnDestroy()
        {
            if (button)
            {
                button.onClick.RemoveListener(Choose);
            }
        }

        /// <summary>Plays the press sound and reports the row.</summary>
        private void Choose()
        {
            ServiceLocator.Get<AudioManager>().Play(SoundEffect.ButtonPress);
            Chosen?.Invoke(rowIndex);
        }

        /// <summary>Returns the size that shows a texture at its own aspect inside the thumbnail box.</summary>
        private Vector2 FitInBox(int width, int height)
        {
            return width >= height
                ? new Vector2(thumbnailBox, thumbnailBox * height / width)
                : new Vector2(thumbnailBox * width / height, thumbnailBox);
        }

        /// <summary>Returns the line shown in place of the description for a pattern that cannot be placed.</summary>
        private static string GetReasonText(PatternUnavailableReason reason)
        {
            switch (reason)
            {
                case PatternUnavailableReason.TooBigForHalf:
                    return TooBigForHalfText;
                case PatternUnavailableReason.NotEnoughSquares:
                    return NotEnoughSquaresText;
                case PatternUnavailableReason.NoEmptyCells:
                    return NoEmptyCellsText;
                default:
                    return TooBigForBoardText;
            }
        }
    }
}
