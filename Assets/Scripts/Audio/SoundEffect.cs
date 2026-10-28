namespace GameOfLife.Audio
{
    /// <summary>Every one-shot sound the game plays; each one is assigned a clip in the SoundLibrary asset.</summary>
    public enum SoundEffect
    {
        ButtonPress,
        SliderTick,
        CellPlaced,
        CellRemoved,
        OpponentPlaced,
        GenerationTick,
        Absorb,
        CountdownBeep,
        CountdownGo,
        Invalid,
        MatchWon,
        MatchLost,
        MatchDrawn
    }
}
