using System.Collections.Generic;
using GameOfLife.Grid;
using GameOfLife.Simulation;
using GameOfLife.Theming;
using UnityEngine;

namespace GameOfLife.Effects
{
    /// <summary>Bursts tiles out of every absorbed cell in its new owner's colour; the particle systems pool their own particles, so a burst never instantiates anything.</summary>
    public sealed class AbsorptionEffect : MonoBehaviour
    {
        [Tooltip("Emits one soft tile per absorbed cell that swells and fades.")]
        [SerializeField] private ParticleSystem flashParticles;
        [Tooltip("Emits small tiles that fly out of each absorbed cell.")]
        [SerializeField] private ParticleSystem sparkParticles;
        [SerializeField, Min(0)] private int sparksPerCell = 3;
        [Tooltip("Large absorptions only burst from this many evenly spread cells, so huge clusters stay cheap.")]
        [SerializeField, Min(1)] private int maximumCellsPerBurst = 60;
        [Tooltip("Flash size as a multiple of the cell size.")]
        [SerializeField, Min(0f)] private float flashSizeInCells = 1f;
        [Tooltip("Spark size as a multiple of the cell size.")]
        [SerializeField, Min(0f)] private float sparkSizeInCells = 0.35f;
        [Tooltip("Spark speed range in cells per second.")]
        [SerializeField] private Vector2 sparkSpeedInCells = new(3f, 8f);
        [SerializeField] private Vector2 sparkLifetime = new(0.35f, 0.65f);

        private GridView gridView;
        private ThemeService themeService;

        /// <summary>Connects to the board whose cells the bursts come from and to the palette their colours come from.</summary>
        public void Initialise(GridView board, ThemeService theme)
        {
            gridView = board;
            themeService = theme;
        }

        /// <summary>Bursts from each converted cell in its owner's colour, thinning the cells evenly past the burst limit or the particles still free, so overlapping bursts are spread out rather than cut short.</summary>
        public void Play(IReadOnlyList<int> convertedCellIndices, CellGrid grid)
        {
            var cellCount = convertedCellIndices.Count;
            var burstCellCount = Mathf.Min(cellCount, Mathf.Min(maximumCellsPerBurst, CountCellsThatFit()));
            if (burstCellCount <= 0)
            {
                return;
            }

            var cellSize = gridView.CellSize;
            var cellLength = Mathf.Min(cellSize.x, cellSize.y);
            var stride = cellCount / (float)burstCellCount;
            var palette = themeService.Palette;
            for (var burstIndex = 0; burstIndex < burstCellCount; burstIndex++)
            {
                var cellIndex = convertedCellIndices[(int)(burstIndex * stride)];
                var colour = palette.Get(grid.GetOwner(cellIndex) == CellOwner.Player ? ThemeColour.Player : ThemeColour.Opponent);
                BurstFromCell(gridView.GetCellCentre(cellIndex), colour, cellLength);
            }
        }

        /// <summary>Removes every particle at once, such as when a match restarts or the mode changes.</summary>
        public void Clear()
        {
            flashParticles.Clear();
            sparkParticles.Clear();
        }

        /// <summary>Returns how many cells can burst before either particle system runs out of free particles.</summary>
        private int CountCellsThatFit()
        {
            var freeFlashes = flashParticles.main.maxParticles - flashParticles.particleCount;
            if (sparksPerCell == 0)
            {
                return freeFlashes;
            }

            var freeSparks = sparkParticles.main.maxParticles - sparkParticles.particleCount;
            return Mathf.Min(freeFlashes, freeSparks / sparksPerCell);
        }

        /// <summary>Emits one flash and a ring of sparks from a cell centre.</summary>
        private void BurstFromCell(Vector2 cellCentre, Color colour, float cellLength)
        {
            var flash = new ParticleSystem.EmitParams
            {
                position = cellCentre,
                startColor = colour,
                startSize = cellLength * flashSizeInCells,
                velocity = Vector3.zero
            };
            flashParticles.Emit(flash, 1);

            var firstAngle = Random.value * Mathf.PI * 2f;
            for (var sparkIndex = 0; sparkIndex < sparksPerCell; sparkIndex++)
            {
                var angle = firstAngle + sparkIndex * Mathf.PI * 2f / sparksPerCell + Random.Range(-0.4f, 0.4f);
                var speed = Random.Range(sparkSpeedInCells.x, sparkSpeedInCells.y) * cellLength;
                var spark = new ParticleSystem.EmitParams
                {
                    position = cellCentre,
                    velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed,
                    startColor = colour,
                    startSize = cellLength * sparkSizeInCells * Random.Range(0.7f, 1.2f),
                    startLifetime = Random.Range(sparkLifetime.x, sparkLifetime.y),
                    rotation = Random.Range(0f, 360f)
                };
                sparkParticles.Emit(spark, 1);
            }
        }
    }
}
