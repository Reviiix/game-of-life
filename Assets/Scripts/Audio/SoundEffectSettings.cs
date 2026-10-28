using System;
using UnityEngine;

namespace GameOfLife.Audio
{
    /// <summary>The clip, volume and repeat limit for one sound effect, edited in the SoundLibrary asset.</summary>
    [Serializable]
    public sealed class SoundEffectSettings
    {
        [SerializeField] private SoundEffect effect;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [Tooltip("Plays closer together than this are skipped, so rapid events such as fast generations never pile up.")]
        [SerializeField, Min(0f)] private float minimumSecondsBetweenPlays;

        public SoundEffect Effect => effect;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float MinimumSecondsBetweenPlays => minimumSecondsBetweenPlays;
    }
}
