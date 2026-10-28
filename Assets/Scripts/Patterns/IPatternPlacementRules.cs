using System.Collections.Generic;

namespace GameOfLife.Patterns
{
    /// <summary>What a positioning session asks of the board: where patterns may go now, whether a set of cells may be placed, and the one call that places them.</summary>
    public interface IPatternPlacementRules
    {
        /// <summary>Reports where and with how many cells a pattern may be placed right now; false when no pattern may be placed.</summary>
        bool TryGetPatternPlacementBounds(out PlacementBounds bounds);

        /// <summary>Returns whether every listed cell may be placed as one move right now.</summary>
        bool CanPlacePattern(IReadOnlyList<int> cellIndices);

        /// <summary>Places every listed cell as one move if allowed, and returns whether it did.</summary>
        bool TryPlacePattern(IReadOnlyList<int> cellIndices);
    }
}
