using GameOfLife.Motion;
using GameOfLife.Theming;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Screens
{
    /// <summary>Draws a Toggle as a sliding switch: the knob springs across and the track and knob blend between their off and on colours.</summary>
    [RequireComponent(typeof(Toggle))]
    public sealed class ToggleSwitch : MonoBehaviour
    {
        [SerializeField] private Graphic track;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Graphic knobGraphic;
        [SerializeField] private ThemeColour trackOffColour = ThemeColour.SurfaceStrong;
        [SerializeField] private ThemeColour trackOnColour = ThemeColour.Ink;
        [SerializeField] private ThemeColour knobOffColour = ThemeColour.InkMuted;
        [SerializeField] private ThemeColour knobOnColour = ThemeColour.Background;

        private Toggle toggle;
        private ThemeService themeService;
        private ReplayableTween slide;
        private float knobTravel;
        private float shownOnAmount;
        private float slideStartAmount;
        private float slideEndAmount;

        /// <summary>Caches the toggle, builds the slide tween, measures how far the knob travels inside the fixed-size track and listens for value and palette changes; SettingsPanel calls this before SnapToValue.</summary>
        public void Initialise(ThemeService theme)
        {
            toggle = GetComponent<Toggle>();
            themeService = theme;
            slide = new ReplayableTween(themeService.Theme.SwitchSlide, ShowSlideProgress);
            toggle.onValueChanged.AddListener(SlideToValue);
            themeService.PaletteChanged += Repaint;
            var trackBounds = ((RectTransform)track.transform).rect;
            knobTravel = (trackBounds.width - trackBounds.height) * 0.5f;
        }

        /// <summary>Stops following palette changes and removes the slide tween when destroyed.</summary>
        private void OnDestroy()
        {
            if (themeService != null)
            {
                themeService.PaletteChanged -= Repaint;
            }

            slide?.Kill();
        }

        /// <summary>Jumps to the toggle's value without animating, for values set without notification.</summary>
        public void SnapToValue()
        {
            slide.Stop();
            ShowOnAmount(toggle.isOn ? 1f : 0f);
        }

        /// <summary>Springs the knob to the toggle's new value.</summary>
        private void SlideToValue(bool isOn)
        {
            slideStartAmount = shownOnAmount;
            slideEndAmount = isOn ? 1f : 0f;
            slide.Play();
        }

        /// <summary>Applies the slide tween's progress.</summary>
        private void ShowSlideProgress(float progress)
        {
            ShowOnAmount(Mathf.LerpUnclamped(slideStartAmount, slideEndAmount, progress));
        }

        /// <summary>Repaints at the current position after the palette changes.</summary>
        private void Repaint(ThemePalette palette)
        {
            ShowOnAmount(shownOnAmount);
        }

        /// <summary>Places the knob and blends the colours, where 0 is off and 1 is on; overshoot moves the knob past its end for the bounce.</summary>
        private void ShowOnAmount(float onAmount)
        {
            shownOnAmount = onAmount;
            var palette = themeService.Palette;
            var colourAmount = Mathf.Clamp01(onAmount);
            knob.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-knobTravel, knobTravel, onAmount), knob.anchoredPosition.y);
            track.color = Color.Lerp(palette.Get(trackOffColour), palette.Get(trackOnColour), colourAmount);
            knobGraphic.color = Color.Lerp(palette.Get(knobOffColour), palette.Get(knobOnColour), colourAmount);
        }
    }
}
