using System.Collections;
using GameOfLife.Grid;
using GameOfLife.HowToPlay;
using GameOfLife.Simulation;
using GameOfLife.Theming;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.HowToPlay
{
    /// <summary>A small looping board for one guide step: draws the demo's frames, built once from the real engine, with the board's own tiles and cell animations, and repeats them while its tab is showing.</summary>
    [RequireComponent(typeof(GridView), typeof(LayoutElement))]
    public sealed class DemoBoardView : MonoBehaviour
    {
        [Tooltip("Shades the app's half for Versus setup demos; sits over the board.")]
        [SerializeField] private RectTransform appHalfShade;
        [Tooltip("The square the board fits inside, in canvas units.")]
        [SerializeField, Min(1f)] private float boardBox = 240f;

        private GridView gridView;
        private CellPaint[][] framePaints;
        private CellPaint[] shownPaints;
        private WaitForSeconds stepWait;
        private WaitForSeconds repeatWait;
        private Coroutine loopRoutine;

        /// <summary>Plays the demo through the engine once, sizes the board to its shape, shades the app's half if asked and shows the first frame.</summary>
        public void Initialise(GuideDemo demo, ThemeService theme)
        {
            gridView = GetComponent<GridView>();
            var frames = demo.BuildFrames();
            framePaints = new CellPaint[frames.Length][];
            for (var frame = 0; frame < frames.Length; frame++)
            {
                framePaints[frame] = ToPaints(frames[frame], demo.VersusColours);
            }

            shownPaints = new CellPaint[demo.Rows * demo.Columns];
            stepWait = new WaitForSeconds(demo.SecondsPerStep);
            repeatWait = new WaitForSeconds(demo.SecondsBeforeRepeat);
            gridView.Initialise(demo.Rows, demo.Columns, theme, true);
            gridView.Resize(demo.Rows, demo.Columns);
            SizeToShape(demo.Rows, demo.Columns);
            ShadeAppHalf(demo);
            ShowFirstFrameImmediately();
        }

        /// <summary>Puts the first frame back without animating, then plays the demo and repeats it until stopped.</summary>
        public void Play()
        {
            Stop();
            ShowFirstFrameImmediately();
            loopRoutine = StartCoroutine(PlayFramesInALoop());
        }

        /// <summary>Stops the demo where it is, so a closing page fades out without its boards jumping.</summary>
        public void Stop()
        {
            if (loopRoutine == null)
            {
                return;
            }

            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        /// <summary>Forgets the loop when the board is deactivated, which stops its coroutine.</summary>
        private void OnDisable()
        {
            loopRoutine = null;
        }

        /// <summary>Shows each frame in turn, holds the last one, goes back to the first and starts again.</summary>
        private IEnumerator PlayFramesInALoop()
        {
            while (true)
            {
                for (var frame = 1; frame < framePaints.Length; frame++)
                {
                    yield return stepWait;
                    ShowFrame(frame, false);
                }

                yield return repeatWait;
                ShowFrame(0, true);
            }
        }

        /// <summary>Animates every cell whose look differs between the shown frame and the given one, uploading the board once; when starting over, a living cell that only changes colour switches at once, so the restart never looks like an absorption.</summary>
        private void ShowFrame(int frame, bool isStartingOver)
        {
            var paints = framePaints[frame];
            for (var cellIndex = 0; cellIndex < paints.Length; cellIndex++)
            {
                var shown = shownPaints[cellIndex];
                var target = paints[cellIndex];
                if (target == shown)
                {
                    continue;
                }

                shownPaints[cellIndex] = target;
                if (isStartingOver && shown != CellPaint.Empty && target != CellPaint.Empty)
                {
                    gridView.PaintCellImmediately(cellIndex, target);
                }
                else
                {
                    gridView.PaintCell(cellIndex, target);
                }
            }

            gridView.ApplyPaintedCells();
        }

        /// <summary>Draws the first frame at once, with no cell animation.</summary>
        private void ShowFirstFrameImmediately()
        {
            if (framePaints == null)
            {
                return;
            }

            var paints = framePaints[0];
            for (var cellIndex = 0; cellIndex < paints.Length; cellIndex++)
            {
                shownPaints[cellIndex] = paints[cellIndex];
                gridView.PaintCellImmediately(cellIndex, paints[cellIndex]);
            }

            gridView.ApplyPaintedCells();
        }

        /// <summary>Fits the board inside its box at the demo's own aspect, so tiles stay square.</summary>
        private void SizeToShape(int rows, int columns)
        {
            var layout = GetComponent<LayoutElement>();
            var cellSize = boardBox / Mathf.Max(rows, columns);
            layout.preferredWidth = cellSize * columns;
            layout.preferredHeight = cellSize * rows;
            layout.minWidth = layout.preferredWidth;
            layout.minHeight = layout.preferredHeight;
        }

        /// <summary>Shades the bottom half, the app's setup side, for demos that ask for it.</summary>
        private void ShadeAppHalf(GuideDemo demo)
        {
            appHalfShade.gameObject.SetActive(demo.ShadeAppHalf);
            if (!demo.ShadeAppHalf)
            {
                return;
            }

            var appRows = demo.Rows - demo.Rows / 2;
            appHalfShade.anchorMin = Vector2.zero;
            appHalfShade.anchorMax = new Vector2(1f, appRows / (float)demo.Rows);
            appHalfShade.offsetMin = Vector2.zero;
            appHalfShade.offsetMax = Vector2.zero;
        }

        /// <summary>Returns how each owner is drawn: in Classic's colours in Classic demos, and in each side's colour in Versus demos.</summary>
        private static CellPaint[] ToPaints(CellOwner[] owners, bool versusColours)
        {
            var paints = new CellPaint[owners.Length];
            for (var cellIndex = 0; cellIndex < owners.Length; cellIndex++)
            {
                paints[cellIndex] = versusColours ? ToVersusPaint(owners[cellIndex]) : ClassicCellColours.ToPaint(owners[cellIndex]);
            }

            return paints;
        }

        /// <summary>Returns how a Versus owner is drawn: the player blue, the app red, and anything else empty.</summary>
        private static CellPaint ToVersusPaint(CellOwner owner)
        {
            switch (owner)
            {
                case CellOwner.Player:
                    return CellPaint.Player;
                case CellOwner.Opponent:
                    return CellPaint.Opponent;
                default:
                    return CellPaint.Empty;
            }
        }
    }
}
