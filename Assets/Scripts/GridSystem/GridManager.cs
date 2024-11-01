using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pure_unity_methods.Abstraction;
using pure_unity_methods.StateManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GridSystem
{
    [ExecuteInEditMode]
    public class GridManager : Singleton<GridManager>
    {
        public static Action<GridItem> OnItemClick;
        private bool generatingGrid;
        private const string GridTag = "Grid";
        private readonly List<GridItem> gridItems = new ();
        private GameObject gridObject;
        [SerializeField] private Transform gridArea;
        [Header("Prefabs")]
        [SerializeField] private GameObject gridPrefab;
        [SerializeField] private GameObject rowPrefab;
        [SerializeField] private GameObject cardPrefab;
        [Header("Colors")]
        [SerializeField] private Color aliveColour = Color.black;
        [SerializeField] private Color deadColour = Color.white;
        public bool RandomisedColour { get; private set; }
        [Header("Settings")]
        private const int MinimumItems = 3;
        private const int MaximumRows = 60;
        private const int MaximumColumns = 30;
        [SerializeField] [Range(MinimumItems, MaximumRows)] public int amountOfRows;
        [SerializeField] [Range(MinimumItems, MaximumColumns)] public int amountOfColumns;
        [SerializeField] public Slider rowsSlider;
        [SerializeField] public Slider columnsSlider;

        public IEnumerator Initialise(Action completeCallback)
        {
            SetSliders();
            yield return new WaitUntil(() => !generatingGrid);
            GenerateGrid(completeCallback);
        }
        
        public void ReInitialise()
        {
            StartCoroutine(Initialise(()=>
            {
                EnableInteractions();
            }));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SequentialStateManager.OnStateChange += OnStateChange;
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            SequentialStateManager.OnStateChange -= OnStateChange;
        }

        private void SetSliders()
        {
            rowsSlider.wholeNumbers = false;
            rowsSlider.value = amountOfRows;
            rowsSlider.minValue = MinimumItems;
            rowsSlider.maxValue = MaximumRows;
            rowsSlider.onValueChanged.AddListener(OnRowsSliderValueChanged);
            
            rowsSlider.wholeNumbers = false;
            columnsSlider.value = amountOfColumns;
            columnsSlider.minValue = MinimumItems;
            columnsSlider.maxValue = MaximumColumns;
            columnsSlider.onValueChanged.AddListener(OnColumnsSliderValueChanged);
        }

        private void OnStateChange()
        {
            foreach (var item in gridItems)
            {
                item.OnStateChange();
            }
        }
        
        public void OnRowsSliderValueChanged(float f)
        {
            amountOfRows = (int)f;
            GenerateGrid();
        }
        
        public void OnColumnsSliderValueChanged(float f)
        {
            amountOfColumns = (int)f;
            GenerateGrid();
        }

        private static void OnGridItemClick(GridItem gridItem)
        {
            //Debug.Log($"{gridItem.Value.GetRank()} of {gridItem.Value.GetSuit()} revealed({gridItem.Revealed})"); //Debug tool
            OnItemClick?.Invoke(gridItem);
        }
        
        private void SetNeighbors()
        {
            foreach (var item in gridItems)
            {
                item.SetNeighbors();
            }
        }
        
        public void OptimiseGrid()
        {
            EnableInteractions();
        }
        
        public void EnableInteractions(bool state = true)
        {
            foreach (var item in gridItems)
            {
                item.EnableInteractions(state);
            }
        }

        public void EnableHighlight(bool state = true)
        {
            foreach (var item in gridItems)
            {
                item.EnableHighlight(state);
            }
        }

        public void EnableRandomisedColour(bool state = true)
        {
            RandomisedColour = state;
            foreach (var item in gridItems)
            {
                item.EnableRandomisedColour(state);
            }
        }

        public GridItem GetItem(KeyValuePair<int, int> indices)
        {
            var x = indices.Key; //Cache for LINQs hidden multiple access.
            var y = indices.Value;
            return gridItems.Where(gridItem => gridItem.Indices.Key == x).FirstOrDefault(gridItem => gridItem.Indices.Value == y);
        }

        public bool IsGameValid()
        {
            return gridItems.Any(x => x.Populated);
        }

        #region Generate Grid
        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            if (generatingGrid) return;
            GenerateGrid();
        }

        [ContextMenu(nameof(ResetGridGenerationSystem))]
        public void ResetGridGenerationSystem()
        {
            generatingGrid = false;
            amountOfRows = MinimumItems;
            amountOfColumns = MinimumItems;
            GenerateGrid();
        }
        #endif
        
        private void GenerateGrid(Action completeCallback = null)
        {
            if (generatingGrid) return;
            generatingGrid = true;
            ResetGrid(()=>CreateGrid(() =>
            {
                gridObject = GameObject.FindWithTag(GridTag);
                generatingGrid = false;
                completeCallback?.Invoke();
            }));
        }

        private void CreateGrid(Action completeCallback = null)
        {
            gridObject = Instantiate(gridPrefab, gridArea);
            for (var i = 0; i < amountOfRows; i++)
            {
                var row = Instantiate(rowPrefab, gridObject.transform).GetComponent<GridRow>();
                for (var j = 0; j < amountOfColumns; j++)
                {
                    if (Application.isPlaying)
                    {
                        var item = Instantiate(cardPrefab, row.transform).GetComponent<GridItem>();
                        item.Initialise(new KeyValuePair<int, int>(j, i), aliveColour, deadColour, OnGridItemClick);
                        gridItems.Add(item);
                    }
                    else
                    {
                        Instantiate(cardPrefab, row.transform);
                    }
                }
            }
            SetNeighbors();
            completeCallback?.Invoke();
        }

        private void ResetGrid(Action completeCallback = null)
        {
            ResetGridItems();
            gridObject = GameObject.FindWithTag(GridTag);
            if (!gridObject)
            {
                completeCallback?.Invoke();
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(gridObject);
                completeCallback?.Invoke();
            }
            else
            {
                UnityEditor.EditorApplication.delayCall+=()=>
                {
                    DestroyImmediate(gridObject);
                    completeCallback?.Invoke();
                };
            }
        }
    
        private void ResetGridItems()
        {
            foreach (var item in gridItems)
            {
                item.ResetItem();
            }
            gridItems.Clear();
        }
        #endregion Generate Grid
    }
}

