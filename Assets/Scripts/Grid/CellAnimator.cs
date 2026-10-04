using System;
using GameOfLife.Motion;
using Unity.Collections;
using UnityEngine;

namespace GameOfLife.Grid
{
    /// <summary>Animates cells growing in, shrinking away and changing colour by writing each changing cell's colour and tile size into its board texture pixel.</summary>
    public sealed class CellAnimator
    {
        /// <summary>The largest tile size a pixel's alpha can hold; the headroom above 1 is for bouncy overshoot. GridCells.shader must use the same value.</summary>
        private const float MaximumEncodedScale = 1.5f;
        private const byte OpaqueAlpha = byte.MaxValue;
        private static readonly Color32 EmptyPixel = new(0, 0, 0, 0);

        private readonly Color32[] targetColours;
        private readonly Color32[] startColours;
        private readonly float[] startScales;
        private readonly CellMotionKind[] motionKinds;
        private readonly float[] motionStartTimes;
        private readonly bool[] isAnimating;
        private readonly bool[] isListed;
        private readonly int[] animatingCellIndices;
        private readonly EasedMotion appear;
        private readonly EasedMotion disappear;
        private readonly EasedMotion convert;
        private readonly float convertSquashScale;
        private readonly float convertColourShare;
        private int animatingCellCount;
        private float longestDuration = float.MaxValue;

        /// <summary>The pixel alpha that draws a tile at its normal size.</summary>
        public static byte FullSizeAlpha { get; } = EncodeScale(1f);

        public bool HasActiveAnimations => animatingCellCount > 0;

        /// <summary>Allocates per-cell state for the largest board up front so animating never allocates.</summary>
        public CellAnimator(int maximumCellCount, EasedMotion appear, EasedMotion disappear, EasedMotion convert, float convertSquashScale, float convertColourShare)
        {
            if (maximumCellCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCellCount), maximumCellCount, "The cell count cannot be negative.");
            }

            targetColours = new Color32[maximumCellCount];
            startColours = new Color32[maximumCellCount];
            startScales = new float[maximumCellCount];
            motionKinds = new CellMotionKind[maximumCellCount];
            motionStartTimes = new float[maximumCellCount];
            isAnimating = new bool[maximumCellCount];
            isListed = new bool[maximumCellCount];
            animatingCellIndices = new int[maximumCellCount];
            this.appear = appear;
            this.disappear = disappear;
            this.convert = convert;
            this.convertSquashScale = convertSquashScale;
            this.convertColourShare = Mathf.Max(convertColourShare, Mathf.Epsilon);
        }

        /// <summary>Caps every animation at this many seconds, so cells settle before the next generation changes them again.</summary>
        public void LimitDurationTo(float seconds)
        {
            longestDuration = Mathf.Max(0f, seconds);
        }

        /// <summary>Returns the pixel alpha that stores a tile size.</summary>
        public static byte EncodeScale(float scale)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp(scale, 0f, MaximumEncodedScale) / MaximumEncodedScale * OpaqueAlpha);
        }

        /// <summary>Returns the tile size stored in a pixel's alpha.</summary>
        public static float DecodeScale(byte alpha)
        {
            return alpha / (float)OpaqueAlpha * MaximumEncodedScale;
        }

        /// <summary>Empties the first cells of the board at once, stopping every animation.</summary>
        public void Clear(int cellCount, NativeArray<Color32> pixels)
        {
            StopAllAnimations();
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                targetColours[cellIndex] = EmptyPixel;
                pixels[cellIndex] = EmptyPixel;
            }
        }

        /// <summary>Records a cell's new look and starts the matching animation from the given time, continuing from the size and colour its pixel shows now; changes that look the same do nothing.</summary>
        public void SetCell(int cellIndex, Color32 colour, bool alive, float now, NativeArray<Color32> pixels)
        {
            var previousColour = targetColours[cellIndex];
            var wasAlive = previousColour.a != 0;
            targetColours[cellIndex] = alive ? WithAlpha(colour, OpaqueAlpha) : EmptyPixel;
            if (!wasAlive && !alive || wasAlive && alive && HasSameColour(previousColour, colour))
            {
                return;
            }

            var shownPixel = pixels[cellIndex];
            motionKinds[cellIndex] = !wasAlive ? CellMotionKind.Appear : alive ? CellMotionKind.Convert : CellMotionKind.Disappear;
            startColours[cellIndex] = shownPixel.a != 0 ? WithAlpha(shownPixel, OpaqueAlpha) : previousColour;
            startScales[cellIndex] = DecodeScale(shownPixel.a);
            motionStartTimes[cellIndex] = now;
            isAnimating[cellIndex] = true;
            if (!isListed[cellIndex])
            {
                isListed[cellIndex] = true;
                animatingCellIndices[animatingCellCount] = cellIndex;
                animatingCellCount++;
            }
        }

        /// <summary>Changes a cell's look at once without animating, such as when the palette changes; its list entry is dropped on the next Advance.</summary>
        public void SetCellImmediately(int cellIndex, Color32 colour, bool alive, NativeArray<Color32> pixels)
        {
            targetColours[cellIndex] = alive ? WithAlpha(colour, OpaqueAlpha) : EmptyPixel;
            isAnimating[cellIndex] = false;
            pixels[cellIndex] = GetSettledPixel(cellIndex);
        }

        /// <summary>Writes every animating cell's pixel for the given time, settles finished ones, and returns whether any are still animating.</summary>
        public bool Advance(float now, NativeArray<Color32> pixels)
        {
            var stillAnimatingCount = 0;
            for (var listPosition = 0; listPosition < animatingCellCount; listPosition++)
            {
                var cellIndex = animatingCellIndices[listPosition];
                if (isAnimating[cellIndex] && TryWriteAnimatedPixel(cellIndex, now, pixels))
                {
                    animatingCellIndices[stillAnimatingCount] = cellIndex;
                    stillAnimatingCount++;
                    continue;
                }

                if (isAnimating[cellIndex])
                {
                    pixels[cellIndex] = GetSettledPixel(cellIndex);
                }

                isAnimating[cellIndex] = false;
                isListed[cellIndex] = false;
            }

            animatingCellCount = stillAnimatingCount;
            return HasActiveAnimations;
        }

        /// <summary>Jumps every animating cell to its final look.</summary>
        public void FinishAll(NativeArray<Color32> pixels)
        {
            for (var listPosition = 0; listPosition < animatingCellCount; listPosition++)
            {
                var cellIndex = animatingCellIndices[listPosition];
                if (isAnimating[cellIndex])
                {
                    pixels[cellIndex] = GetSettledPixel(cellIndex);
                }

                isAnimating[cellIndex] = false;
                isListed[cellIndex] = false;
            }

            animatingCellCount = 0;
        }

        /// <summary>Writes a cell's pixel part-way through its animation; returns false once the animation has finished.</summary>
        private bool TryWriteAnimatedPixel(int cellIndex, float now, NativeArray<Color32> pixels)
        {
            var kind = motionKinds[cellIndex];
            var motion = GetMotion(kind);
            var duration = Mathf.Min(motion.Duration, longestDuration);
            var elapsed = now - motionStartTimes[cellIndex];
            if (elapsed >= duration)
            {
                return false;
            }

            elapsed = Mathf.Max(elapsed, 0f);
            var progress = motion.Evaluate(elapsed, duration);
            var startScale = startScales[cellIndex];
            switch (kind)
            {
                case CellMotionKind.Appear:
                    pixels[cellIndex] = WithAlpha(targetColours[cellIndex], EncodeScale(Mathf.LerpUnclamped(startScale, 1f, progress)));
                    break;
                case CellMotionKind.Disappear:
                    pixels[cellIndex] = WithAlpha(startColours[cellIndex], EncodeScale(Mathf.LerpUnclamped(startScale, 0f, progress)));
                    break;
                default:
                    var colourChange = Mathf.Clamp01(elapsed / (duration * convertColourShare));
                    var colour = Color32.Lerp(startColours[cellIndex], targetColours[cellIndex], colourChange);
                    var squashedScale = Mathf.Min(startScale, convertSquashScale);
                    pixels[cellIndex] = WithAlpha(colour, EncodeScale(Mathf.LerpUnclamped(squashedScale, 1f, progress)));
                    break;
            }

            return true;
        }

        /// <summary>Returns the eased motion for an animation kind.</summary>
        private EasedMotion GetMotion(CellMotionKind kind)
        {
            switch (kind)
            {
                case CellMotionKind.Appear:
                    return appear;
                case CellMotionKind.Disappear:
                    return disappear;
                default:
                    return convert;
            }
        }

        /// <summary>Returns the pixel for a cell at rest: its colour at full size, or nothing when empty.</summary>
        private Color32 GetSettledPixel(int cellIndex)
        {
            var target = targetColours[cellIndex];
            return target.a == 0 ? EmptyPixel : WithAlpha(target, FullSizeAlpha);
        }

        /// <summary>Forgets every running animation without touching any pixel.</summary>
        private void StopAllAnimations()
        {
            for (var listPosition = 0; listPosition < animatingCellCount; listPosition++)
            {
                var cellIndex = animatingCellIndices[listPosition];
                isAnimating[cellIndex] = false;
                isListed[cellIndex] = false;
            }

            animatingCellCount = 0;
        }

        /// <summary>Returns whether two colours have the same red, green and blue.</summary>
        private static bool HasSameColour(Color32 first, Color32 second)
        {
            return first.r == second.r && first.g == second.g && first.b == second.b;
        }

        /// <summary>Returns a colour with its alpha replaced.</summary>
        private static Color32 WithAlpha(Color32 colour, byte alpha)
        {
            colour.a = alpha;
            return colour;
        }
    }
}
