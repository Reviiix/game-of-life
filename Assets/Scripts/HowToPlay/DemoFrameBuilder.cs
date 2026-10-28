using System;
using System.Collections.Generic;
using GameOfLife.Simulation;

namespace GameOfLife.HowToPlay
{
    /// <summary>Plays a how-to-play demo through the real engine once, at start-up, and returns every frame as each cell's owner: placements and generations follow the game's own rules, with absorption after each, as in both modes.</summary>
    public static class DemoFrameBuilder
    {
        private const char EmptyOrUnchangedSymbol = '.';
        private const char InkSymbol = 'O';
        private const char BlueSymbol = 'B';
        private const char RedSymbol = 'R';
        private const char YellowSymbol = 'Y';
        private const char RemoveSymbol = '-';

        /// <summary>Returns the start board followed by one frame per step; each frame lists every cell's owner row by row. Symbols name the colour shown: Versus has blue (you, also 'O') and red (the app); Classic has ink 'O', blue 'B', red 'R' and yellow 'Y'.</summary>
        public static CellOwner[][] Build(IReadOnlyList<string> start, IReadOnlyList<DemoStepKind> stepKinds, IReadOnlyList<IReadOnlyList<string>> stepCells, bool versusColours)
        {
            if (stepKinds == null || stepCells == null || stepKinds.Count != stepCells.Count)
            {
                throw new ArgumentException("Every demo step needs a kind and a (possibly empty) set of cells.", nameof(stepCells));
            }

            var rows = start?.Count ?? 0;
            var columns = rows > 0 ? start[0]?.Length ?? 0 : 0;
            if (rows == 0 || columns == 0)
            {
                throw new ArgumentException("A demo needs a start board of at least one row and one column.", nameof(start));
            }

            var grid = new CellGrid(rows, columns);
            grid.Resize(rows, columns);
            var absorber = new ClusterAbsorber(grid.MaximumCellCount);
            Apply(grid, start, false, versusColours);
            var frames = new CellOwner[stepKinds.Count + 1][];
            frames[0] = Capture(grid);
            for (var step = 0; step < stepKinds.Count; step++)
            {
                if (stepKinds[step] == DemoStepKind.Generation)
                {
                    grid.AdvanceGeneration();
                }
                else
                {
                    Apply(grid, stepCells[step], true, versusColours);
                }

                absorber.ResolveAbsorptions(grid);
                frames[step + 1] = Capture(grid);
            }

            return frames;
        }

        /// <summary>Writes rows of colour symbols onto the grid; '.' leaves a cell as it is (empty on the start board), and '-' removes a cell when placing.</summary>
        private static void Apply(CellGrid grid, IReadOnlyList<string> rows, bool allowRemoval, bool versusColours)
        {
            if (rows == null || rows.Count != grid.Rows)
            {
                throw new ArgumentException($"A demo board must have {grid.Rows} rows.", nameof(rows));
            }

            for (var row = 0; row < grid.Rows; row++)
            {
                var text = rows[row];
                if (text == null || text.Length != grid.Columns)
                {
                    throw new ArgumentException($"Row {row} of a demo board must be {grid.Columns} symbols wide.", nameof(rows));
                }

                for (var column = 0; column < grid.Columns; column++)
                {
                    var symbol = text[column];
                    if (symbol == EmptyOrUnchangedSymbol)
                    {
                        continue;
                    }

                    grid.SetOwner(grid.ToCellIndex(row, column), ToOwner(symbol, allowRemoval, versusColours, row));
                }
            }
        }

        /// <summary>Returns the owner a colour symbol stands for in Versus or Classic colours, rejecting removals on the start board, yellow in Versus and unknown symbols.</summary>
        private static CellOwner ToOwner(char symbol, bool allowRemoval, bool versusColours, int row)
        {
            switch (symbol)
            {
                case InkSymbol:
                    return CellOwner.Player;
                case BlueSymbol:
                    return versusColours ? CellOwner.Player : CellOwner.Third;
                case RedSymbol:
                    return CellOwner.Opponent;
                case YellowSymbol when !versusColours:
                    return CellOwner.Fourth;
                case RemoveSymbol when allowRemoval:
                    return CellOwner.None;
                default:
                    throw new ArgumentException($"Row {row} of a demo board holds '{symbol}', which is not a demo symbol here.");
            }
        }

        /// <summary>Copies every cell's owner, row by row.</summary>
        private static CellOwner[] Capture(CellGrid grid)
        {
            var owners = new CellOwner[grid.CellCount];
            for (var cellIndex = 0; cellIndex < owners.Length; cellIndex++)
            {
                owners[cellIndex] = grid.GetOwner(cellIndex);
            }

            return owners;
        }
    }
}
