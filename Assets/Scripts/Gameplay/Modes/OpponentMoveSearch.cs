using System.Collections;
using System.Threading.Tasks;
using GameOfLife.Opponents;
using GameOfLife.Simulation;
using UnityEngine;

namespace GameOfLife.Gameplay.Modes
{
    /// <summary>Runs the app's move search on a worker thread against a snapshot of the board, so slow decisions never stall a frame.</summary>
    public sealed class OpponentMoveSearch
    {
        private const int NoCell = -1;

        private readonly CellGrid boardSnapshot;
        private Task<int> runningSearch;

        public int ChosenCellIndex { get; private set; } = NoCell;
        public bool FoundCell => ChosenCellIndex != NoCell;

        /// <summary>Allocates the snapshot board once at the largest allowed size.</summary>
        public OpponentMoveSearch(int maximumRows, int maximumColumns)
        {
            boardSnapshot = new CellGrid(maximumRows, maximumColumns);
        }

        /// <summary>Coroutine: waits for any earlier search, snapshots the board, searches off the main thread, then sets ChosenCellIndex.</summary>
        public IEnumerator Search(CellGrid board, PlacementRegion region, IOpponentStrategy strategy)
        {
            ChosenCellIndex = NoCell;
            while (runningSearch != null && !runningSearch.IsCompleted)
            {
                yield return null;
            }

            boardSnapshot.CopyFrom(board);
            runningSearch = Task.Run(() => strategy.TryChooseCell(boardSnapshot, region, CellOwner.Opponent, out var cellIndex) ? cellIndex : NoCell);
            while (!runningSearch.IsCompleted)
            {
                yield return null;
            }

            if (runningSearch.IsFaulted)
            {
                Debug.LogException(runningSearch.Exception);
                yield break;
            }

            ChosenCellIndex = runningSearch.Result;
        }
    }
}
