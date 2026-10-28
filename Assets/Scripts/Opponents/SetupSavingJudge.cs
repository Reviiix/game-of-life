using GameOfLife.Simulation;

namespace GameOfLife.Opponents
{
    /// <summary>Decides, when the player ends setup early, whether the app saves its remaining setup squares too or keeps placing, by playing the board a few generations ahead to see who would be winning.</summary>
    public sealed class SetupSavingJudge
    {
        private readonly CellGrid projection;
        private readonly ClusterAbsorber absorber;
        private readonly int generationsAhead;

        /// <summary>Allocates the projection board and absorber once at the largest board size.</summary>
        public SetupSavingJudge(int maximumRows, int maximumColumns, int generationsAhead)
        {
            projection = new CellGrid(maximumRows, maximumColumns);
            absorber = new ClusterAbsorber(projection.MaximumCellCount);
            this.generationsAhead = generationsAhead;
        }

        /// <summary>Returns true when, after the projected generations and absorptions, the app would have at least as many squares as the player, so it can afford to save squares for the match; false means it should keep placing to catch up.</summary>
        public bool ShouldSaveSquares(CellGrid board)
        {
            projection.CopyFrom(board);
            for (var generation = 0; generation < generationsAhead; generation++)
            {
                projection.AdvanceGeneration();
                absorber.ResolveAbsorptions(projection);
            }

            return projection.CountCells(CellOwner.Opponent) >= projection.CountCells(CellOwner.Player);
        }
    }
}
