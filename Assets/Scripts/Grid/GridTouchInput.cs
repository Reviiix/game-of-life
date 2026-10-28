using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameOfLife.Grid
{
    /// <summary>Turns touches on the grid into cell indices: a tap counts only when it starts and ends on the same cell, while the first finger held on the board reports every cell it presses and drags over for positioning a pattern; the active mode decides what a tap does.</summary>
    [RequireComponent(typeof(GridView))]
    public sealed class GridTouchInput : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        private const int NoCell = -1;
        private const int NoPointer = int.MinValue;

        private GridView gridView;
        private int lastDraggedCellIndex = NoCell;
        private int heldPointerId = NoPointer;

        public event Action<int> CellTapped;
        public event Action<int> CellPressed;
        public event Action<int> CellDragged;

        /// <summary>Caches the grid view this component reads cell positions from.</summary>
        private void Awake()
        {
            gridView = GetComponent<GridView>();
        }

        /// <summary>Forgets a held finger when the board is disabled mid-press, so the next touch is not ignored.</summary>
        private void OnDisable()
        {
            heldPointerId = NoPointer;
        }

        /// <summary>Reports the tapped cell when the press and release land on the same cell.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            var eventCamera = eventData.pressEventCamera;
            var pressedOnCell = gridView.TryGetCellIndexAtScreenPosition(eventData.pressPosition, eventCamera, out var pressedCellIndex);
            var releasedOnCell = gridView.TryGetCellIndexAtScreenPosition(eventData.position, eventCamera, out var releasedCellIndex);
            if (pressedOnCell && releasedOnCell && pressedCellIndex == releasedCellIndex)
            {
                CellTapped?.Invoke(releasedCellIndex);
            }
        }

        /// <summary>Reports the cell a touch starts on, ignoring further fingers while one is already held on the board.</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (heldPointerId != NoPointer)
            {
                return;
            }

            heldPointerId = eventData.pointerId;
            lastDraggedCellIndex = NoCell;
            if (gridView.TryGetCellIndexAtScreenPosition(eventData.position, eventData.pressEventCamera, out var cellIndex))
            {
                lastDraggedCellIndex = cellIndex;
                CellPressed?.Invoke(cellIndex);
            }
        }

        /// <summary>Frees the board for the next finger when the held one lifts.</summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == heldPointerId)
            {
                heldPointerId = NoPointer;
            }
        }

        /// <summary>Reports each new cell the held finger drags onto, treating positions past the board's edges as the nearest edge cell; other fingers are ignored.</summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != heldPointerId)
            {
                return;
            }

            if (gridView.TryGetNearestCellIndexAtScreenPosition(eventData.position, eventData.pressEventCamera, out var cellIndex) && cellIndex != lastDraggedCellIndex)
            {
                lastDraggedCellIndex = cellIndex;
                CellDragged?.Invoke(cellIndex);
            }
        }
    }
}
