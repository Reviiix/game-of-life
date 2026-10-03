using System;

namespace GameOfLife.Simulation
{
    /// <summary>Lets each larger single-colour cluster convert the smaller enemy clusters it touches, repeating until nothing changes.</summary>
    public sealed class ClusterAbsorber
    {
        private const int NoCluster = -1;
        private const int NoCell = -1;
        private const int SmallestClusterThatCanAbsorb = 2;
        private const int ClusterIdsPerCell = 2;

        private readonly int maximumCellCount;
        private readonly int[] clusterIdOfCell;
        private readonly int[] nextCellInCluster;
        private readonly int[] floodFillStack;
        private readonly int[] clusterFirstCells;
        private readonly int[] clusterSizes;
        private readonly CellOwner[] clusterOwners;
        private readonly int[] clusterSmallestCellIndices;
        private readonly bool[] doesClusterTouchSmallerEnemy;
        private readonly bool[] isClusterInTouchingList;
        private readonly int[] touchingClusterIds;
        private int clusterCount;

        /// <summary>Allocates every labelling and flood-fill buffer for the largest allowed grid up front so resolving never allocates.</summary>
        public ClusterAbsorber(int maximumCellCount)
        {
            if (maximumCellCount < 0 || (long)maximumCellCount * ClusterIdsPerCell > int.MaxValue)
            {
                throw new ArgumentException($"An absorber for {maximumCellCount} cells cannot be allocated.", nameof(maximumCellCount));
            }

            // Every round retires at least two clusters and creates one merged cluster, so a resolve uses fewer than twice as many ids as cells.
            var maximumClusterCount = maximumCellCount * ClusterIdsPerCell;
            this.maximumCellCount = maximumCellCount;
            clusterIdOfCell = new int[maximumCellCount];
            nextCellInCluster = new int[maximumCellCount];
            floodFillStack = new int[maximumCellCount];
            clusterFirstCells = new int[maximumClusterCount];
            clusterSizes = new int[maximumClusterCount];
            clusterOwners = new CellOwner[maximumClusterCount];
            clusterSmallestCellIndices = new int[maximumClusterCount];
            doesClusterTouchSmallerEnemy = new bool[maximumClusterCount];
            isClusterInTouchingList = new bool[maximumClusterCount];
            touchingClusterIds = new int[maximumClusterCount];
        }

        /// <summary>Converts smaller touching enemy clusters, largest absorber first, until stable, and returns how many cells changed owner.</summary>
        public int ResolveAbsorptions(CellGrid grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (grid.CellCount > maximumCellCount)
            {
                throw new ArgumentException($"A grid of {grid.CellCount} cells does not fit in {maximumCellCount} cells.", nameof(grid));
            }

            LabelClusters(grid);
            var convertedCellCount = 0;
            var absorbingClusterId = FindAbsorbingCluster();
            while (absorbingClusterId != NoCluster)
            {
                convertedCellCount += ConvertSmallerEnemiesTouching(grid, absorbingClusterId);
                var mergedClusterId = FloodFillCluster(grid, clusterFirstCells[absorbingClusterId]);
                RefreshTouchFlagsAround(grid, mergedClusterId);
                absorbingClusterId = FindAbsorbingCluster();
            }

            return convertedCellCount;
        }

        /// <summary>Splits the living cells into maximal same-owner clusters joined by 8-way adjacency and flags each one that touches a smaller enemy.</summary>
        private void LabelClusters(CellGrid grid)
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
                    FloodFillCluster(grid, cellIndex);
                }
            }

            for (var clusterId = 0; clusterId < clusterCount; clusterId++)
            {
                doesClusterTouchSmallerEnemy[clusterId] = TouchesSmallerEnemy(grid, clusterId);
            }
        }

        /// <summary>Gathers every same-owner cell connected to the start cell into one new cluster, retiring any cluster it takes cells from, and returns its id.</summary>
        private int FloodFillCluster(CellGrid grid, int startCellIndex)
        {
            var clusterId = clusterCount;
            clusterCount++;
            var owner = grid.GetOwner(startCellIndex);
            var size = 0;
            var smallestCellIndex = startCellIndex;
            var firstCellIndex = NoCell;
            var stackSize = ClaimCell(startCellIndex, clusterId, 0);
            while (stackSize > 0)
            {
                stackSize--;
                var cellIndex = floodFillStack[stackSize];
                nextCellInCluster[cellIndex] = firstCellIndex;
                firstCellIndex = cellIndex;
                size++;
                smallestCellIndex = Math.Min(smallestCellIndex, cellIndex);
                var neighbourCount = grid.GetNeighbourCount(cellIndex);
                for (var neighbourSlot = 0; neighbourSlot < neighbourCount; neighbourSlot++)
                {
                    var neighbourIndex = grid.GetNeighbourIndex(cellIndex, neighbourSlot);
                    if (clusterIdOfCell[neighbourIndex] != clusterId && grid.GetOwner(neighbourIndex) == owner)
                    {
                        stackSize = ClaimCell(neighbourIndex, clusterId, stackSize);
                    }
                }
            }

            clusterOwners[clusterId] = owner;
            clusterSizes[clusterId] = size;
            clusterSmallestCellIndices[clusterId] = smallestCellIndex;
            clusterFirstCells[clusterId] = firstCellIndex;
            doesClusterTouchSmallerEnemy[clusterId] = false;
            return clusterId;
        }

        /// <summary>Moves a cell into the cluster being filled, retires the cluster it came from and pushes it on the flood-fill stack, returning the new stack size.</summary>
        private int ClaimCell(int cellIndex, int clusterId, int stackSize)
        {
            var previousClusterId = clusterIdOfCell[cellIndex];
            if (previousClusterId != NoCluster)
            {
                clusterSizes[previousClusterId] = 0;
                doesClusterTouchSmallerEnemy[previousClusterId] = false;
            }

            clusterIdOfCell[cellIndex] = clusterId;
            floodFillStack[stackSize] = cellIndex;
            return stackSize + 1;
        }

        /// <summary>Returns the flagged cluster that acts first: the largest, then the one with the lowest smallest cell index, or NoCluster.</summary>
        private int FindAbsorbingCluster()
        {
            var bestClusterId = NoCluster;
            for (var clusterId = 0; clusterId < clusterCount; clusterId++)
            {
                if (doesClusterTouchSmallerEnemy[clusterId] && (bestClusterId == NoCluster || ShouldAbsorbBefore(clusterId, bestClusterId)))
                {
                    bestClusterId = clusterId;
                }
            }

            return bestClusterId;
        }

        /// <summary>Returns whether the first cluster takes priority over the second: bigger first, then the lower smallest cell index.</summary>
        private bool ShouldAbsorbBefore(int clusterId, int otherClusterId)
        {
            var size = clusterSizes[clusterId];
            var otherSize = clusterSizes[otherClusterId];
            if (size != otherSize)
            {
                return size > otherSize;
            }

            return clusterSmallestCellIndices[clusterId] < clusterSmallestCellIndices[otherClusterId];
        }

        /// <summary>Returns whether any cell of the cluster is an 8-way neighbour of a strictly smaller enemy cluster.</summary>
        private bool TouchesSmallerEnemy(CellGrid grid, int clusterId)
        {
            if (clusterSizes[clusterId] < SmallestClusterThatCanAbsorb)
            {
                return false;
            }

            for (var cellIndex = clusterFirstCells[clusterId]; cellIndex != NoCell; cellIndex = nextCellInCluster[cellIndex])
            {
                var neighbourCount = grid.GetNeighbourCount(cellIndex);
                for (var neighbourSlot = 0; neighbourSlot < neighbourCount; neighbourSlot++)
                {
                    if (IsSmallerEnemy(clusterId, clusterIdOfCell[grid.GetNeighbourIndex(cellIndex, neighbourSlot)]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Returns whether the other cluster exists, belongs to a different side and is strictly smaller.</summary>
        private bool IsSmallerEnemy(int clusterId, int otherClusterId)
        {
            return otherClusterId != NoCluster
                && clusterOwners[otherClusterId] != clusterOwners[clusterId]
                && clusterSizes[otherClusterId] < clusterSizes[clusterId];
        }

        /// <summary>Converts every strictly smaller enemy cluster touching the absorber to its owner and returns how many cells changed.</summary>
        private int ConvertSmallerEnemiesTouching(CellGrid grid, int absorbingClusterId)
        {
            var convertedCellCount = 0;
            for (var cellIndex = clusterFirstCells[absorbingClusterId]; cellIndex != NoCell; cellIndex = nextCellInCluster[cellIndex])
            {
                var neighbourCount = grid.GetNeighbourCount(cellIndex);
                for (var neighbourSlot = 0; neighbourSlot < neighbourCount; neighbourSlot++)
                {
                    var neighbourClusterId = clusterIdOfCell[grid.GetNeighbourIndex(cellIndex, neighbourSlot)];
                    if (IsSmallerEnemy(absorbingClusterId, neighbourClusterId))
                    {
                        convertedCellCount += ConvertCluster(grid, neighbourClusterId, clusterOwners[absorbingClusterId]);
                    }
                }
            }

            return convertedCellCount;
        }

        /// <summary>Gives every cell of a cluster to a new owner, so it no longer counts as an enemy, and returns how many cells changed.</summary>
        private int ConvertCluster(CellGrid grid, int clusterId, CellOwner newOwner)
        {
            for (var cellIndex = clusterFirstCells[clusterId]; cellIndex != NoCell; cellIndex = nextCellInCluster[cellIndex])
            {
                grid.SetOwner(cellIndex, newOwner);
            }

            clusterOwners[clusterId] = newOwner;
            return clusterSizes[clusterId];
        }

        /// <summary>Re-flags the merged cluster and every enemy cluster touching it, the only clusters whose touching sizes changed.</summary>
        private void RefreshTouchFlagsAround(CellGrid grid, int mergedClusterId)
        {
            var touchingClusterCount = 0;
            for (var cellIndex = clusterFirstCells[mergedClusterId]; cellIndex != NoCell; cellIndex = nextCellInCluster[cellIndex])
            {
                var neighbourCount = grid.GetNeighbourCount(cellIndex);
                for (var neighbourSlot = 0; neighbourSlot < neighbourCount; neighbourSlot++)
                {
                    var neighbourClusterId = clusterIdOfCell[grid.GetNeighbourIndex(cellIndex, neighbourSlot)];
                    if (neighbourClusterId == NoCluster || neighbourClusterId == mergedClusterId || isClusterInTouchingList[neighbourClusterId])
                    {
                        continue;
                    }

                    isClusterInTouchingList[neighbourClusterId] = true;
                    touchingClusterIds[touchingClusterCount] = neighbourClusterId;
                    touchingClusterCount++;
                }
            }

            var mergedTouchesSmallerEnemy = false;
            for (var listPosition = 0; listPosition < touchingClusterCount; listPosition++)
            {
                var touchingClusterId = touchingClusterIds[listPosition];
                isClusterInTouchingList[touchingClusterId] = false;
                mergedTouchesSmallerEnemy |= IsSmallerEnemy(mergedClusterId, touchingClusterId);
                doesClusterTouchSmallerEnemy[touchingClusterId] = TouchesSmallerEnemy(grid, touchingClusterId);
            }

            doesClusterTouchSmallerEnemy[mergedClusterId] = mergedTouchesSmallerEnemy;
        }
    }
}
