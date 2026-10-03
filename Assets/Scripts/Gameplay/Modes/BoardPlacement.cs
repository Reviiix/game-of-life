using UnityEngine;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>Remembers where the board's RectTransform sits so a mode can move the board and put it back exactly.</summary>
    public readonly struct BoardPlacement
    {
        private readonly Transform parent;
        private readonly int siblingIndex;
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 offsetMin;
        private readonly Vector2 offsetMax;
        private readonly Vector2 pivot;

        /// <summary>Stores every value needed to restore a RectTransform's parent and layout.</summary>
        private BoardPlacement(RectTransform board)
        {
            parent = board.parent;
            siblingIndex = board.GetSiblingIndex();
            anchorMin = board.anchorMin;
            anchorMax = board.anchorMax;
            offsetMin = board.offsetMin;
            offsetMax = board.offsetMax;
            pivot = board.pivot;
        }

        /// <summary>Records where the board currently sits.</summary>
        public static BoardPlacement Capture(RectTransform board)
        {
            return new BoardPlacement(board);
        }

        /// <summary>Makes a RectTransform fill its parent exactly.</summary>
        public static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>Moves the board back to the recorded parent, order and layout.</summary>
        public void Restore(RectTransform board)
        {
            board.SetParent(parent, false);
            board.SetSiblingIndex(siblingIndex);
            board.anchorMin = anchorMin;
            board.anchorMax = anchorMax;
            board.pivot = pivot;
            board.offsetMin = offsetMin;
            board.offsetMax = offsetMax;
        }
    }
}
