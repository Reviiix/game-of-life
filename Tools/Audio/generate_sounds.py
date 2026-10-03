"""Generates the game's sound effects and background music as 16-bit WAV files.

Everything is synthesised from simple tones in C major pentatonic, so any sounds that overlap
stay in tune with each other and with the music. Run from the repository root:

    python3 Tools/Audio/generate_sounds.py

The files are written to Assets/Art/Audio. Unity reimports them automatically; their import
settings live in the .meta files and are not touched by this script.
"""

import array
import math
import os
import random
import struct
import wave

SAMPLE_RATE = 44100
OUTPUT_ROOT = os.path.join("Assets", "Art", "Audio")
SOUND_EFFECTS_FOLDER = os.path.join(OUTPUT_ROOT, "SoundEffects")
MUSIC_FOLDER = os.path.join(OUTPUT_ROOT, "Music")
PEAK_LEVEL = 0.89  # about -1 dBFS, leaving headroom for Unity's mixer

NOTE_FREQUENCIES = {
    "C3": 130.81, "D3": 146.83, "E3": 164.81, "F3": 174.61, "G3": 196.00, "A3": 220.00,
    "C4": 261.63, "D4": 293.66, "E4": 329.63, "F4": 349.23, "G4": 392.00, "A4": 440.00, "B4": 493.88,
    "C5": 523.25, "D5": 587.33, "E5": 659.25, "F5": 698.46, "G5": 783.99, "A5": 880.00,
    "C6": 1046.50, "D6": 1174.66, "E6": 1318.51, "G6": 1567.98,
}


# ---------- Building blocks ----------

def silence(seconds):
    """Returns a buffer of silent samples lasting the given number of seconds."""
    return array.array("d", [0.0] * int(seconds * SAMPLE_RATE))


def mix_into(target, source, start_seconds=0.0, gain=1.0, wrap=False):
    """Adds source into target starting at a time offset; wrap makes tails continue from the start, for seamless loops."""
    offset = int(start_seconds * SAMPLE_RATE)
    length = len(target)
    for index, value in enumerate(source):
        position = offset + index
        if position >= length:
            if not wrap:
                break
            position %= length
        target[position] += value * gain


def mallet(frequency, seconds, brightness=1.0, decay=6.0):
    """A marimba-like struck tone: a fundamental plus a quickly fading fourth harmonic and a soft attack click."""
    samples = array.array("d")
    for index in range(int(seconds * SAMPLE_RATE)):
        time = index / SAMPLE_RATE
        attack = min(1.0, time / 0.003)
        body = math.sin(2 * math.pi * frequency * time) * math.exp(-decay * time)
        overtone = 0.35 * brightness * math.sin(2 * math.pi * frequency * 3.99 * time) * math.exp(-decay * 4 * time)
        samples.append(attack * (body + overtone))
    return samples


def bell(frequency, seconds, decay=3.0):
    """A kalimba/bell tone using slightly inharmonic partials for a glassy shimmer."""
    partials = ((1.0, 1.0, 1.0), (2.76, 0.32, 2.2), (5.40, 0.12, 3.5))
    samples = array.array("d")
    for index in range(int(seconds * SAMPLE_RATE)):
        time = index / SAMPLE_RATE
        attack = min(1.0, time / 0.004)
        value = 0.0
        for ratio, level, extra_decay in partials:
            value += level * math.sin(2 * math.pi * frequency * ratio * time) * math.exp(-decay * extra_decay * time)
        samples.append(attack * value)
    return samples


def soft_pad(frequencies, seconds, attack=0.8, release=1.2):
    """A warm sustained chord from slightly detuned triangle-ish waves with slow attack and release."""
    samples = array.array("d")
    total = int(seconds * SAMPLE_RATE)
    for index in range(total):
        time = index / SAMPLE_RATE
        envelope = min(1.0, time / attack) * min(1.0, max(0.0, (seconds - time) / release))
        value = 0.0
        for frequency in frequencies:
            for detune in (0.997, 1.003):
                phase = frequency * detune * time
                value += math.sin(2 * math.pi * phase) + 0.12 * math.sin(2 * math.pi * 3 * phase)
        samples.append(envelope * value / (len(frequencies) * 2))
    return samples


def glide(start_frequency, end_frequency, seconds, tremolo=0.0):
    """A sine that slides smoothly between two pitches, optionally with tremolo, for sweeps."""
    samples = array.array("d")
    phase = 0.0
    total = int(seconds * SAMPLE_RATE)
    for index in range(total):
        progress = index / total
        frequency = start_frequency * (end_frequency / start_frequency) ** progress
        phase += frequency / SAMPLE_RATE
        envelope = math.sin(math.pi * progress) ** 0.7
        wobble = 1.0 - tremolo * 0.5 * (1 + math.sin(2 * math.pi * 18 * index / SAMPLE_RATE))
        samples.append(envelope * wobble * math.sin(2 * math.pi * phase))
    return samples


def noise_burst(seconds, decay, seed):
    """A short burst of filtered white noise, for clicks and ticks."""
    generator = random.Random(seed)
    samples = array.array("d")
    previous = 0.0
    for index in range(int(seconds * SAMPLE_RATE)):
        time = index / SAMPLE_RATE
        previous = previous * 0.6 + generator.uniform(-1, 1) * 0.4
        samples.append(previous * math.exp(-decay * time))
    return samples


def low_pass(samples, cutoff):
    """Softens a buffer with a one-pole low-pass filter."""
    coefficient = 1 - math.exp(-2 * math.pi * cutoff / SAMPLE_RATE)
    filtered = array.array("d")
    previous = 0.0
    for value in samples:
        previous += coefficient * (value - previous)
        filtered.append(previous)
    return filtered


def fade_edges(samples, fade_in=0.002, fade_out=0.01):
    """Removes clicks at the very start and end of a one-shot sound."""
    length = len(samples)
    fade_in_samples = max(1, int(fade_in * SAMPLE_RATE))
    fade_out_samples = max(1, int(fade_out * SAMPLE_RATE))
    for index in range(min(fade_in_samples, length)):
        samples[index] *= index / fade_in_samples
    for index in range(min(fade_out_samples, length)):
        samples[length - 1 - index] *= index / fade_out_samples
    return samples


def normalise(samples, peak=PEAK_LEVEL):
    """Scales a buffer so its loudest sample sits at the given peak, removing any DC offset first."""
    mean = sum(samples) / len(samples)
    centred = array.array("d", (value - mean for value in samples))
    loudest = max(abs(value) for value in centred) or 1.0
    return array.array("d", (value * peak / loudest for value in centred))


def write_wav(path, channels):
    """Writes one (mono) or two (stereo) equal-length buffers as a 16-bit PCM WAV file."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    frame_count = len(channels[0])
    frames = bytearray()
    for index in range(frame_count):
        for channel in channels:
            frames += struct.pack("<h", int(max(-1.0, min(1.0, channel[index])) * 32767))
    with wave.open(path, "wb") as output:
        output.setnchannels(len(channels))
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        output.writeframes(bytes(frames))
    print(f"wrote {path} ({frame_count / SAMPLE_RATE:.2f}s, {len(channels)} channel{'s' if len(channels) > 1 else ''})")


def save_effect(name, samples, peak=PEAK_LEVEL):
    """Fades, normalises and writes a mono sound effect."""
    write_wav(os.path.join(SOUND_EFFECTS_FOLDER, name + ".wav"), [normalise(fade_edges(samples), peak)])


def sequence(notes, voice, step_seconds, tail_seconds, **voice_options):
    """Plays a list of note names one after another with a given voice, for short jingles."""
    total = silence(step_seconds * len(notes) + tail_seconds)
    for position, note in enumerate(notes):
        mix_into(total, voice(NOTE_FREQUENCIES[note], step_seconds + tail_seconds, **voice_options), position * step_seconds)
    return total


# ---------- Sound effects ----------

def build_sound_effects():
    """Creates every one-shot sound the game plays."""
    click = glide(1100, 700, 0.06)
    mix_into(click, noise_burst(0.01, 300, seed=1), 0, 0.25)
    save_effect("button_press", low_pass(click, 6000))

    save_effect("slider_tick", low_pass(mallet(NOTE_FREQUENCIES["C6"], 0.05, brightness=0.2, decay=60), 8000), peak=0.6)

    # Placed cells are retuned by the AudioManager to a pentatonic note for their row, so this is pitched at C5.
    save_effect("cell_placed", mallet(NOTE_FREQUENCIES["C5"], 0.45, brightness=0.8, decay=7))

    removed = mallet(NOTE_FREQUENCIES["G4"], 0.2, brightness=0.2, decay=18)
    save_effect("cell_removed", low_pass(removed, 2500), peak=0.7)

    save_effect("opponent_placed", low_pass(bell(NOTE_FREQUENCIES["E5"], 0.4, decay=4.5), 5000), peak=0.8)

    tick = noise_burst(0.04, 90, seed=2)
    mix_into(tick, mallet(NOTE_FREQUENCIES["A3"], 0.04, brightness=0.0, decay=60), 0, 0.6)
    save_effect("generation_tick", low_pass(tick, 1800), peak=0.5)

    absorb = glide(330, 990, 0.32, tremolo=0.35)
    mix_into(absorb, bell(NOTE_FREQUENCIES["G6"], 0.25, decay=6), 0.18, 0.25)
    save_effect("absorb", low_pass(absorb, 7000), peak=0.8)

    save_effect("countdown_beep", bell(NOTE_FREQUENCIES["G5"], 0.22, decay=9), peak=0.75)

    go = silence(0.6)
    mix_into(go, bell(NOTE_FREQUENCIES["C6"], 0.6, decay=4))
    mix_into(go, bell(NOTE_FREQUENCIES["G6"], 0.45, decay=5), 0.07, 0.6)
    save_effect("countdown_go", go)

    invalid = sequence(["E4", "C4"], lambda f, s: low_pass(glide(f, f * 0.97, s), 1500), 0.12, 0.08)
    save_effect("invalid", invalid, peak=0.7)

    win = sequence(["C5", "E5", "G5", "C6"], bell, 0.12, 0.9, decay=2.2)
    mix_into(win, soft_pad([NOTE_FREQUENCIES["C4"], NOTE_FREQUENCIES["G4"], NOTE_FREQUENCIES["E5"]], 1.3, attack=0.05, release=0.8), 0.36, 0.5)
    save_effect("match_won", win)

    save_effect("match_lost", low_pass(sequence(["A4", "F4", "D4"], mallet, 0.22, 0.8, brightness=0.3, decay=3), 3000))

    save_effect("match_drawn", sequence(["G4", "D5", "G4"], bell, 0.18, 0.6, decay=3.5), peak=0.8)


# ---------- Music ----------

def build_music():
    """Creates a calm 16-bar loop in C major at 90 BPM whose tails wrap around so it loops without a seam."""
    beats_per_minute = 90
    beat = 60 / beats_per_minute
    bar = beat * 4
    progression = [
        (["C3", "G3", "E4"], ["C5", "E5", "G5", "E5", "D5", "G5", "E5", "C5"]),
        (["A3", "E4", "C5"], ["A4", "C5", "E5", "C5", "D5", "E5", "C5", "A4"]),
        (["F3", "C4", "A4"], ["A4", "C5", "D5", "C5", "A4", "C5", "G4", "A4"]),
        (["G3", "D4", "B4"], ["G4", "D5", "G5", "D5", "E5", "D5", "B4", "D5"]),
    ]
    bars_per_chord = 2
    loop_seconds = bar * bars_per_chord * len(progression) * 2
    left = silence(loop_seconds)
    right = silence(loop_seconds)
    generator = random.Random(90)

    for repeat in range(2):
        for chord_number, (pad_notes, arpeggio) in enumerate(progression):
            chord_start = (repeat * len(progression) + chord_number) * bar * bars_per_chord
            chord_length = bar * bars_per_chord
            pad = soft_pad([NOTE_FREQUENCIES[n] for n in pad_notes], chord_length + 1.2, attack=0.9, release=1.4)
            pad = low_pass(pad, 1400)
            mix_into(left, pad, chord_start, 0.32, wrap=True)
            mix_into(right, pad, chord_start, 0.32, wrap=True)

            bass = mallet(NOTE_FREQUENCIES[pad_notes[0]], beat * 1.6, brightness=0.0, decay=2.0)
            for beat_in_chord in (0, 2, 4, 6):
                mix_into(left, bass, chord_start + beat_in_chord * beat, 0.30, wrap=True)
                mix_into(right, bass, chord_start + beat_in_chord * beat, 0.30, wrap=True)

            # Eighth-note arpeggio; the second pass adds a little variation and stereo movement.
            for step in range(16):
                note = arpeggio[step % len(arpeggio)]
                if repeat == 1 and step % 8 == 7:
                    note = arpeggio[(step + 3) % len(arpeggio)]
                resting_in_first_pass = repeat == 0 and step in (5, 13)
                resting_in_second_pass = repeat == 1 and step in (3, 11) and generator.random() < 0.5
                if resting_in_first_pass or resting_in_second_pass:
                    continue
                voice = mallet if step % 4 else bell
                tone = voice(NOTE_FREQUENCIES[note], beat * 1.5, **({"brightness": 0.6, "decay": 5} if voice is mallet else {"decay": 3.5}))
                pan = 0.5 + 0.25 * math.sin(step * math.pi / 4 + repeat)
                start = chord_start + step * beat / 2
                mix_into(left, tone, start, 0.22 * (1 - pan) * 2, wrap=True)
                mix_into(right, tone, start, 0.22 * pan * 2, wrap=True)

    loudest = max(max(abs(v) for v in left), max(abs(v) for v in right))
    scale = 0.75 / loudest
    left = array.array("d", (v * scale for v in left))
    right = array.array("d", (v * scale for v in right))
    write_wav(os.path.join(MUSIC_FOLDER, "puzzle_loop.wav"), [left, right])


if __name__ == "__main__":
    build_sound_effects()
    build_music()
