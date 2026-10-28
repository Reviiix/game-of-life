using UnityEngine;

namespace GameOfLife.Audio
{
    /// <summary>Designer-editable list of the music and every sound effect, with their volumes and repeat limits.</summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Game of Life/Sound Library")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.45f;
        [SerializeField, Min(0f)] private float musicFadeInSeconds = 2f;

        [Header("Sound Effects")]
        [Tooltip("How many sound effects can play at once; the oldest is reused when all are busy.")]
        [SerializeField, Min(1)] private int simultaneousSoundEffects = 8;
        [SerializeField] private SoundEffectSettings[] soundEffects;

        public AudioClip BackgroundMusic => backgroundMusic;
        public float MusicVolume => musicVolume;
        public float MusicFadeInSeconds => musicFadeInSeconds;
        public int SimultaneousSoundEffects => simultaneousSoundEffects;
        public SoundEffectSettings[] SoundEffects => soundEffects;
    }
}
