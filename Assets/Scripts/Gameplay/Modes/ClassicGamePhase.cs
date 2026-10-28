namespace GameOfLife.Gameplay.Modes
{
    /// <summary>The stages classic mode moves through between editing cells and running the simulation; Showcase runs a pulsating pattern behind the main menu at launch. Add new phases at the end.</summary>
    public enum ClassicGamePhase
    {
        Editing,
        CountingDown,
        Running,
        Showcase
    }
}
