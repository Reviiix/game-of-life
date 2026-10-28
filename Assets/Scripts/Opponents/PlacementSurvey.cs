using System;
using GameOfLife.Simulation;

namespace GameOfLife.Opponents
{
    /// <summary>Surveys the board once per choice (each cell's own and enemy neighbour counts and each living cell's cluster) so placements can be judged locally without simulating.</summary>
    public sealed class PlacementSurvey
    {
        private const int NoCluster = -1;
        private const int NoCell = -1;
        private const int MaximumAdjacentClusters = 8;
        private const int AbsorbedCellLossWeight = 2;
        private const int SmallestClusterThatCanAbsorb = 2;

        private readonly byte[] ownNeighbourCounts;
        private readonly byte[] enemyNeighbourCounts;
        private readonly int[] clusterIdOfCell;
        private readonly int[] clusterSizes;
        private readonly CellOwner[] clusterOwners;
        private readonly int[] floodFillStack;
        private readonly int[] nextCellInCluster;
        private readonly int[] clusterFirstCells;
        private readonly int[] touchingEnemyClusterIds;
        private readonly int[] clusterFirstTouchingEnemies;
        private readonly int[] clusterTouchingEnemyEnds;
        private readonly int[] enemyListedForCluster;
        private readonly int[] enemyCountedInScore;
        private readonly int[] adjacentClusterIds = new int[MaximumAdjacentClusters];
        private CellGrid grid;
        private CellOwner self;
        private int clusterCount;
        private int scoreNumber;

        /// <summary>Allocates every buffer for the largest board up front so surveying never allocates.</summary>
        public PlacementSurvey(int maximumCellCount)
        {
            ownNeighbourCounts = new byte[maximumCellCount];
            enemyNeighbourCounts = new byte[maximumCellCount];
            clusterIdOfCell = new int[maximumCellCount];
            clusterSizes = new int[maximumCellCount];
            clusterOwners = new CellOwner[maximumCellCount];
            floodFillStack = new int[maximumCellCount];
            nextCellInCluster = new int[maximumCellCount];
            clusterFirstCells = new int[maximumCellCount];
            // Each own cluster lists an enemy cluster once, and every listing needs a different living neighbour of one of its cells.
            touchingEnemyClusterIds = new int[maximumCellCount * MaximumAdjacentClusters];
            clusterFirstTouchingEnemies = new int[maximumCellCount];
            clusterTouchingEnemyEnds = new int[maximumCellCount];
            enemyListedForCluster = new int[maximumCellCount];
            enemyCountedInScore = new int[maximumCellCount];
        }

        /// <summary>Counts every cell's living neighbours for and against the side, labels every same-colour 8-way cluster with its size, and lists the enemy clusters each of the side's clusters touches.</summary>
        public void Survey(CellGrid surveyedGrid, CellOwner side)
        {
            grid = surveyedGrid;
            self = side;
            CountNeighbours();
            LabelClusters();
            ListEnemiesTouchingOwnClusters();
        }

        /// <summary>Returns exactly how many more of the side's cells the board will have one generation after a square goes on the empty cell, before absorption: support, rescues, overcrowding and own-colour births in one sum.</summary>
        public int NextGenerationOwnGain(int cellIndex)
        {
            int own = ownNeighbourCounts[cellIndex];
            int enemy = enemyNeighbourCounts[cellIndex];
            var gain = Survives(own + enemy) - IsBornAs(own, enemy);
            var neighbourCount = grid.GetNeighbourCount(cellIndex);
            for (var slot = 0; slot < neighbourCount; slot++)
            {
                var neighbourIndex = grid.GetNeighbourIndex(cellIndex, slot);
                int neighbourOwn = ownNeighbourCounts[neighbourIndex];
                int neighbourEnemy = enemyNeighbourCounts[neighbourIndex];
                var owner = grid.GetOwner(neighbourIndex);
                if (owner == self)
                {
                    gain += Survives(neighbourOwn + neighbourEnemy + 1) - Survives(neighbourOwn + neighbourEnemy);
                }
                else if (owner == CellOwner.None)
                {
                    gain += IsBornAs(neighbourOwn + 1, neighbourEnemy) - IsBornAs(neighbourOwn, neighbourEnemy);
                }
            }

            return gain;
        }

        /// <summary>Scores the absorption a square on the empty cell causes: minus twice the merged cluster if a larger enemy touching it would convert it, else the cells of the smaller enemies touching the merged cluster, which it absorbs at once (later cascades are not counted).</summary>
        public int ScoreAbsorption(int cellIndex)
        {
            var mergedSize = 1;
            var adjacentCount = CollectAdjacentClusters(cellIndex);
            for (var position = 0; position < adjacentCount; position++)
            {
                var clusterId = adjacentClusterIds[position];
                if (clusterOwners[clusterId] == self)
                {
                    mergedSize += clusterSizes[clusterId];
                }
            }

            scoreNumber++;
            var largestEnemySize = 0;
            var smallerEnemyCells = 0;
            for (var position = 0; position < adjacentCount; position++)
            {
                var clusterId = adjacentClusterIds[position];
                if (clusterOwners[clusterId] != self)
                {
                    smallerEnemyCells += TallyTouchingEnemy(clusterId, mergedSize, ref largestEnemySize);
                    continue;
                }

                for (var listing = clusterFirstTouchingEnemies[clusterId]; listing < clusterTouchingEnemyEnds[clusterId]; listing++)
                {
                    smallerEnemyCells += TallyTouchingEnemy(touchingEnemyClusterIds[listing], mergedSize, ref largestEnemySize);
                }
            }

            if (largestEnemySize > mergedSize)
            {
                return -AbsorbedCellLossWeight * mergedSize;
            }

            return mergedSize < SmallestClusterThatCanAbsorb ? 0 : smallerEnemyCells;
        }

        /// <summary>Notes an enemy cluster touching the merged cluster: raises the largest size seen, and returns its cells if it is smaller and not yet counted in this score, else 0.</summary>
        private int TallyTouchingEnemy(int enemyClusterId, int mergedSize, ref int largestEnemySize)
        {
            var enemySize = clusterSizes[enemyClusterId];
            largestEnemySize = Math.Max(largestEnemySize, enemySize);
            if (enemySize >= mergedSize || enemyCountedInScore[enemyClusterId] == scoreNumber)
            {
                return 0;
            }

            enemyCountedInScore[enemyClusterId] = scoreNumber;
            return enemySize;
        }

        /// <summary>Returns 1 if a cell with this many living neighbours stays or comes alive, else 0.</summary>
        private static int Survives(int livingNeighbours)
        {
            return livingNeighbours == 2 || livingNeighbours == 3 ? 1 : 0;
        }

        /// <summary>Returns 1 if an empty cell with these neighbours is born in the first colour (three parents, at least two of that colour), else 0.</summary>
        private static int IsBornAs(int parentsOfColour, int otherParents)
        {
            return parentsOfColour + otherParents == 3 && parentsOfColour >= 2 ? 1 : 0;
        }

        /// <summary>Counts, for every cell, its living neighbours that belong to the side and to its enemy.</summary>
        private void CountNeighbours()
        {
            var cellCount = grid.CellCount;
            Array.Clear(ownNeighbourCounts, 0, cellCount);
            Array.Clear(enemyNeighbourCounts, 0, cellCount);
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                var owner = grid.GetOwner(cellIndex);
                if (owner == CellOwner.None)
                {
                    continue;
                }

                var counts = owner == self ? ownNeighbourCounts : enemyNeighbourCounts;
                var neighbourCount = grid.GetNeighbourCount(cellIndex);
                for (var slot = 0; slot < neighbourCount; slot++)
                {
                    counts[grid.GetNeighbourIndex(cellIndex, slot)]++;
                }
            }
        }

        /// <summary>Labels every living cell with its same-colour 8-way cluster and records each cluster's size and owner.</summary>
        private void LabelClusters()
        {
            var cellCount = grid.CellCount;
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                clusterIdOfCell[cellIndex] = NoCluster;
            }

            clusterCount = 0;
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                if (clusterIdOfCell[cellIndex] == NoCluster && grid.IsAlive(cellIndex))
                {
                    FloodFillCluster(cellIndex);
                }
            }
        }

        /// <summary>Lists, once each, the enemy clusters touching every cluster of the side, so a score can find what a merged cluster touches without walking its cells.</summary>
        private void ListEnemiesTouchingOwnClusters()
        {
            scoreNumber = 0;
            for (var clusterId = 0; clusterId < clusterCount; clusterId++)
            {
                enemyListedForCluster[clusterId] = NoCluster;
                enemyCountedInScore[clusterId] = 0;
            }

            var listingCount = 0;
            for (var clusterId = 0; clusterId < clusterCount; clusterId++)
            {
                clusterFirstTouchingEnemies[clusterId] = listingCount;
                if (clusterOwners[clusterId] == self)
                {
                    listingCount = ListEnemiesTouching(clusterId, listingCount);
                }

                clusterTouchingEnemyEnds[clusterId] = listingCount;
            }
        }

        /// <summary>Appends each enemy cluster touching the cluster to the listings once and returns the new listing count.</summary>
        private int ListEnemiesTouching(int clusterId, int listingCount)
        {
            for (var cellIndex = clusterFirstCells[clusterId]; cellIndex != NoCell; cellIndex = nextCellInCluster[cellIndex])
            {
                var neighbourCount = grid.GetNeighbourCount(cellIndex);
                for (var slot = 0; slot < neighbourCount; slot++)
                {
                    var neighbourClusterId = clusterIdOfCell[grid.GetNeighbourIndex(cellIndex, slot)];
                    if (neighbourClusterId == NoCluster || clusterOwners[neighbourClusterId] == self || enemyListedForCluster[neighbourClusterId] == clusterId)
                    {
                        continue;
                    }

                    enemyListedForCluster[neighbourClusterId] = clusterId;
                    touchingEnemyClusterIds[listingCount] = neighbourClusterId;
                    listingCount++;
                }
            }

            return listingCount;
        }

        /// <summary>Gathers every same-colour cell connected to the start cell into a new cluster, chaining its cells so they can be walked later.</summary>
        private void FloodFillCluster(int startCellIndex)
        {
            var clusterId = clusterCount;
            clusterCount++;
            var owner = grid.GetOwner(startCellIndex);
            var size = 0;
            var stackSize = 0;
            var firstCellIndex = NoCell;
            clusterIdOfCell[startCellIndex] = clusterId;
            floodFillStack[stackSize++] = startCellIndex;
            while (stackSize > 0)
            {
                var cellIndex = floodFillStack[--stackSize];
                nextCellInCluster[cellIndex] = firstCellIndex;
                firstCellIndex = cellIndex;
                size++;
                var neighbourCount = grid.GetNeighbourCount(cellIndex);
                for (var slot = 0; slot < neighbourCount; slot++)
                {
                    var neighbourIndex = grid.GetNeighbourIndex(cellIndex, slot);
                    if (clusterIdOfCell[neighbourIndex] == NoCluster && grid.GetOwner(neighbourIndex) == owner)
                    {
                        clusterIdOfCell[neighbourIndex] = clusterId;
                        floodFillStack[stackSize++] = neighbourIndex;
                    }
                }
            }

            clusterSizes[clusterId] = size;
            clusterOwners[clusterId] = owner;
            clusterFirstCells[clusterId] = firstCellIndex;
        }

        /// <summary>Writes the distinct clusters touching the cell into the adjacent-cluster buffer and returns how many there are.</summary>
        private int CollectAdjacentClusters(int cellIndex)
        {
            var adjacentCount = 0;
            var neighbourCount = grid.GetNeighbourCount(cellIndex);
            for (var slot = 0; slot < neighbourCount; slot++)
            {
                var clusterId = clusterIdOfCell[grid.GetNeighbourIndex(cellIndex, slot)];
                if (clusterId != NoCluster && !IsAlreadyCollected(clusterId, adjacentCount))
                {
                    adjacentClusterIds[adjacentCount] = clusterId;
                    adjacentCount++;
                }
            }

            return adjacentCount;
        }

        /// <summary>Returns whether the cluster is already among the first collected adjacent clusters.</summary>
        private bool IsAlreadyCollected(int clusterId, int collectedCount)
        {
            for (var position = 0; position < collectedCount; position++)
            {
                if (adjacentClusterIds[position] == clusterId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
