namespace GameOfLife.HowToPlay
{
    /// <summary>What one step of a how-to-play demo does: place or remove cells, or run one generation; stored by number in HowToPlayGuide.asset, so add new kinds at the end.</summary>
    public enum DemoStepKind
    {
        Place,
        Generation,
    }
}
