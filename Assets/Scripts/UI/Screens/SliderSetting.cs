using System.Text;
using GameOfLife.Audio;
using GameOfLife.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameOfLife.UI.Screens
{
    /// <summary>A settings row with a slider, a label showing its value and a tick as it moves; lives on the SliderSetting prefab.</summary>
    public sealed class SliderSetting : MonoBehaviour
    {
        private const string WholeNumberFormat = "{0}";
        private const string SecondsFormat = "{0:1}S";
        private const int SecondsPerMinute = 60;

        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private SliderValueFormat valueFormat;

        private readonly StringBuilder valueBuilder = new();
        private AudioManager audioManager;
        private float lastTickedDisplayValue;

        /// <summary>Sets the range and starting value without raising change events, shows the value, and keeps the label in sync.</summary>
        public void Configure(float minimum, float maximum, float startingValue, bool wholeNumbers)
        {
            slider.wholeNumbers = wholeNumbers;
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.SetValueWithoutNotify(startingValue);
            slider.onValueChanged.AddListener(ShowValue);
            slider.onValueChanged.AddListener(PlayTick);
            ShowValue(slider.value);
            lastTickedDisplayValue = RoundToDisplayedPrecision(slider.value);
        }

        /// <summary>Calls the listener whenever the player moves the slider.</summary>
        public void AddValueChangedListener(UnityAction<float> listener)
        {
            slider.onValueChanged.AddListener(listener);
        }

        /// <summary>Ticks as the player drags, only when the value shown in the label changes; the SoundLibrary also limits how often.</summary>
        private void PlayTick(float value)
        {
            var displayValue = RoundToDisplayedPrecision(value);
            if (Mathf.Approximately(displayValue, lastTickedDisplayValue))
            {
                return;
            }

            lastTickedDisplayValue = displayValue;
            if (!audioManager)
            {
                audioManager = ServiceLocator.Get<AudioManager>();
            }

            audioManager.Play(SoundEffect.SliderTick);
        }

        /// <summary>Rounds a value to the precision this row's label shows: tenths for seconds, whole numbers otherwise.</summary>
        private float RoundToDisplayedPrecision(float value)
        {
            const float TenthsPerSecond = 10f;
            return valueFormat == SliderValueFormat.Seconds ? Mathf.Round(value * TenthsPerSecond) / TenthsPerSecond : Mathf.Round(value);
        }

        /// <summary>Writes the slider value into the label in this row's format.</summary>
        private void ShowValue(float value)
        {
            switch (valueFormat)
            {
                case SliderValueFormat.Seconds:
                    valueLabel.SetText(SecondsFormat, value);
                    break;
                case SliderValueFormat.MinutesAndSeconds:
                    var totalSeconds = Mathf.RoundToInt(value);
                    var seconds = totalSeconds % SecondsPerMinute;
                    valueBuilder.Clear().Append(totalSeconds / SecondsPerMinute).Append(':');
                    if (seconds < 10)
                    {
                        valueBuilder.Append('0');
                    }

                    valueLabel.SetText(valueBuilder.Append(seconds));
                    break;
                default:
                    valueLabel.SetText(WholeNumberFormat, value);
                    break;
            }
        }
    }
}
