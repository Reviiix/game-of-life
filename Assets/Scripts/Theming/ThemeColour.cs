namespace GameOfLife.Theming
{
    /// <summary>The roles a colour plays in the interface; each palette gives every role a colour. Add new roles at the end, because scenes and prefabs store roles by number.</summary>
    public enum ThemeColour
    {
        Background,
        Surface,
        SurfaceStrong,
        Ink,
        InkMuted,
        InkInverse,
        Player,
        Opponent,
        Danger,
        Scrim,
        Shade,
        Raised
    }
}
