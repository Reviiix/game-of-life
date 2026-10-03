using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameOfLife.Grid
{
    /// <summary>Turns taps on the grid into cell indices; a tap only counts when it starts and ends on the same cell, and the active mode decides what it does.</summary>
    [RequireComponent(typeof(GridView))]
    public sealed class GridTouchInput : MonoBehaviour, IPointerClickHandler
    {
        private GridView gridView;

        public event Action<int> CellTapped;

        /// <summary>Caches the grid view this component reads cell positions from.</summary>
        private void Awake()
        {
            gridView = GetComponent<GridView>();
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
    }
}
