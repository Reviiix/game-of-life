namespace GameOfLife.Patterns
{
    /// <summary>Why a catalogue row cannot be used right now, or None when it can; NoEmptyCells is for random placement on a full board.</summary>
    public enum PatternUnavailableReason
    {
        None,
        TooBigForBoard,
        TooBigForHalf,
        NotEnoughSquares,
        NoEmptyCells,
    }
}
