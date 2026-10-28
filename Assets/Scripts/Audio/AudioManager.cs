using System;
using System.Collections;
using GameOfLife.Gameplay;
using UnityEngine;

namespace GameOfLife.Audio
{
    /// <summary>Plays the background music and every sound effect through a reused pool of AudioSources, honouring the player's music and sound settings.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int NotesPerOctave = 5;
        private const int PlacementNoteCount = 10;
        private const int LowestPlacementOctave = -1;
        private const float SemitonesPerOctave = 12f;
        private static readonly int[] PentatonicSemitones = { 0, 2, 4, 7, 9 };

        private SoundLibrary library;
        private GameOptions options;
        private AudioSource musicSource;
        private AudioSource[] effectSources;
        private int nextEffectSourceIndex;
        private SoundEffectSettings[] settingsByEffect;
        private float[] lastPlayTimeByEffect;
        private float[] placementNotePitches;
        private Coroutine musicFadeRoutine;

        /// <summary>Creates the music source and effect pool, indexes every sound by type, precomputes note pitches and starts the music.</summary>
        public void Initialise(SoundLibrary soundLibrary, GameOptions gameOptions)
        {
            library = soundLibrary;
            options = gameOptions;
            musicSource = CreateSource();
            musicSource.loop = true;
            musicSource.priority = 0;
            musicSource.clip = library.BackgroundMusic;
            effectSources = new AudioSource[library.SimultaneousSoundEffects];
            for (var sourceIndex = 0; sourceIndex < effectSources.Length; sourceIndex++)
            {
                effectSources[sourceIndex] = CreateSource();
            }

            IndexSoundEffects();
            placementNotePitches = BuildPlacementNotePitches();
            options.MusicEnabledChanged += SetMusicPlaying;
            options.SoundEffectsEnabledChanged += OnSoundEffectsEnabledChanged;
            SetMusicPlaying(options.MusicEnabled);
        }

        /// <summary>Plays a sound effect at its normal pitch, unless sound effects are off or it played too recently.</summary>
        public void Play(SoundEffect effect)
        {
            Play(effect, 1f);
        }

        /// <summary>Plays a sound effect retuned to a pentatonic note that rises towards the top row, so building a pattern plays a tune.</summary>
        public void PlayNoteForRow(SoundEffect effect, int row, int rowCount)
        {
            var heightFromBottom = rowCount - 1 - row;
            var noteIndex = Mathf.Clamp(heightFromBottom * PlacementNoteCount / Mathf.Max(1, rowCount), 0, PlacementNoteCount - 1);
            Play(effect, placementNotePitches[noteIndex]);
        }

        /// <summary>Plays a sound effect at the given pitch on the next pooled source, skipping it if sound is off, it has no clip, or it played too recently.</summary>
        public void Play(SoundEffect effect, float pitch)
        {
            var effectIndex = (int)effect;
            var settings = settingsByEffect[effectIndex];
            if (!options.SoundEffectsEnabled || settings == null || !settings.Clip)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now - lastPlayTimeByEffect[effectIndex] < settings.MinimumSecondsBetweenPlays)
            {
                return;
            }

            lastPlayTimeByEffect[effectIndex] = now;
            var source = effectSources[nextEffectSourceIndex];
            nextEffectSourceIndex = (nextEffectSourceIndex + 1) % effectSources.Length;
            source.clip = settings.Clip;
            source.volume = settings.Volume;
            source.pitch = pitch;
            source.Play();
        }

        /// <summary>Stops listening to option changes when destroyed.</summary>
        private void OnDestroy()
        {
            if (options != null)
            {
                options.MusicEnabledChanged -= SetMusicPlaying;
                options.SoundEffectsEnabledChanged -= OnSoundEffectsEnabledChanged;
            }
        }

        /// <summary>Starts the music with a fade-in, resuming where it stopped, or pauses it.</summary>
        private void SetMusicPlaying(bool playing)
        {
            if (musicFadeRoutine != null)
            {
                StopCoroutine(musicFadeRoutine);
                musicFadeRoutine = null;
            }

            if (!playing || !musicSource.clip)
            {
                musicSource.Pause();
                return;
            }

            musicSource.volume = 0f;
            if (musicSource.time > 0f)
            {
                musicSource.UnPause();
            }
            else
            {
                musicSource.Play();
            }

            musicFadeRoutine = StartCoroutine(FadeInMusic());
        }

        /// <summary>Raises the music to its library volume over the fade-in time.</summary>
        private IEnumerator FadeInMusic()
        {
            var duration = library.MusicFadeInSeconds;
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                musicSource.volume = library.MusicVolume * elapsed / duration;
                yield return null;
            }

            musicSource.volume = library.MusicVolume;
            musicFadeRoutine = null;
        }

        /// <summary>Silences any sound effect still playing when the player turns sound effects off.</summary>
        private void OnSoundEffectsEnabledChanged(bool enabled)
        {
            if (enabled)
            {
                return;
            }

            foreach (var source in effectSources)
            {
                source.Stop();
            }
        }

        /// <summary>Builds a lookup from each SoundEffect to its settings so playing a sound never searches the library.</summary>
        private void IndexSoundEffects()
        {
            var effectCount = Enum.GetValues(typeof(SoundEffect)).Length;
            settingsByEffect = new SoundEffectSettings[effectCount];
            lastPlayTimeByEffect = new float[effectCount];
            for (var effectIndex = 0; effectIndex < effectCount; effectIndex++)
            {
                lastPlayTimeByEffect[effectIndex] = float.NegativeInfinity;
            }

            foreach (var settings in library.SoundEffects)
            {
                var effectIndex = (int)settings.Effect;
                if (settingsByEffect[effectIndex] != null)
                {
                    Debug.LogWarning($"{settings.Effect} is listed more than once in {library.name}; the first entry is used.", library);
                    continue;
                }

                settingsByEffect[effectIndex] = settings;
            }

            for (var effectIndex = 0; effectIndex < effectCount; effectIndex++)
            {
                if (settingsByEffect[effectIndex] == null)
                {
                    Debug.LogWarning($"{(SoundEffect)effectIndex} has no entry in {library.name}, so it will be silent.", library);
                }
            }
        }

        /// <summary>Precomputes the pitch multipliers for two octaves of C major pentatonic, starting an octave below the placement clip's note.</summary>
        private static float[] BuildPlacementNotePitches()
        {
            var pitches = new float[PlacementNoteCount];
            for (var noteIndex = 0; noteIndex < PlacementNoteCount; noteIndex++)
            {
                var octave = LowestPlacementOctave + noteIndex / NotesPerOctave;
                var semitones = octave * SemitonesPerOctave + PentatonicSemitones[noteIndex % NotesPerOctave];
                pitches[noteIndex] = Mathf.Pow(2f, semitones / SemitonesPerOctave);
            }

            return pitches;
        }

        /// <summary>Adds a 2D AudioSource to this object that waits to be told what to play.</summary>
        private AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
