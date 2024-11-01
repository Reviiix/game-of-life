using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

namespace GridSystem
{
    /// <summary>
    /// Items with 1 or 0 neighbors die due to solitude.
    /// Items with 3 or more neighbors die due to over population.
    /// Items with 2 or 3 neighbors survive.
    /// items with 3 neighbors becomes populated
    /// </summary>
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(Outline))]
    public class GridItem : MonoBehaviour
    {
        public KeyValuePair<int, int> Indices { get;  private set; } //Key = X, Value = Y
        private bool Initialised { get; set; }
        public bool Populated { get; private set; }
        private bool randomizeColour;
        private Action<GridItem> onClick;
        private Button button;
        private RawImage display;
        private Outline outline;
        private GridItem[] neighbors;
        private Color aliveColour;
        private Color deadColour;

        public void Initialise(KeyValuePair<int, int> indices, Color alive, Color dead, Action<GridItem> gridManagerOnClick)
        {
            if (Initialised)
            {
                Debug.LogError($"Do not initialise {nameof(GridItem)} more than once.");
                return;
            }

            button = GetComponent<Button>(); //Secured by the require component attribute.
            display = GetComponent<RawImage>();
            outline = GetComponent<Outline>();
            button.onClick.AddListener(OnClick);
            onClick += gridManagerOnClick;
            Indices = indices;
            aliveColour = alive;
            deadColour = dead;
            Initialised = true;
        }
        
        public void EnableInteractions(bool state = true)
        {
            button.enabled = state;
        }
        
        public void EnableHighlight(bool state = true)
        {
            outline.enabled = state;
        }
        
        public void EnableRandomisedColour(bool state = true)
        {
            randomizeColour = state;
        }

        public void SetNeighbors()
        {
            neighbors = new[]
            {
                //Row Above
                GridManager.Instance.GetItem(new KeyValuePair<int, int>(Indices.Key-1,Indices.Value-1)),
                GridManager.Instance.GetItem(new KeyValuePair<int, int>(Indices.Key-1,Indices.Value)),
                GridManager.Instance.GetItem(new KeyValuePair<int, int>(Indices.Key-1,Indices.Value+1)),
                //Same Row
                GridManager.Instance.GetItem(new KeyValuePair<int, int>(Indices.Key,Indices.Value-1)),
                GridManager.Instance.GetItem(new KeyValuePair<int, int>(Indices.Key,Indices.Value+1)),
                //Row Below
                GridManager.Instance.GetItem(new KeyValuePair<int, int>(Indices.Key+1,Indices.Value-1)),
                GridManager.Instance.GetItem(new KeyValuePair<int, int>(Indices.Key+1,Indices.Value)),
                GridManager.Instance.GetItem(new KeyValuePair<int, int>(Indices.Key+1,Indices.Value+1))
            };
            
            neighbors = neighbors.Where(x => x != null).ToArray(); //Remove edge piece neighbors
        }

        private void OnDisable()
        {
            if (onClick != null)
            {
                onClick -= GridManager.OnItemClick;
            }
        }

        private void OnClick()
        {
            if (!Initialised)
            {
                Debug.LogError($"{nameof(GridItem)}{Indices.Key}{Indices.Value} has not been initialised.");
                return;
            }
            SetState(!Populated);
            SetDisplay(Populated);
            onClick(this);
        }
        
        public void OnStateChange()
        {
            CheckPopulate();
            CheckDeath();
        }
        
        private void CheckPopulate()
        {
            if (Populated) return;
            
            if (!ShouldPopulate()) return;
            
            SetState(true);
            SetDisplay(true);
        }

        private void CheckDeath()
        {
            if (!Populated) return;

            if (!ShouldDie()) return;
            
            SetState(false);
            SetDisplay(false);
        }

        private bool ShouldDie()
        {
            return NeighborCountGreaterThanThree() || NeighborCountLessThanTwo();
        }
        
        private bool ShouldPopulate()
        {
            return NeighborCountEqualsThree();        
        }

        private void SetState(bool state)
        {
            Populated = state;
        }
        
        private void SetDisplay(bool state)
        {
            display.color = GetColour(state);
        }

        private Color GetColour(bool state)
        {
            if (randomizeColour)
            {
                aliveColour = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1);
            }
            return state ? aliveColour : deadColour;
        }

        public void ResetItem()
        {
            SetState(true);
        }
        
        private bool NeighborCountGreaterThanThree()
        {
            var count = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.Populated)
                {
                    count++;
                }
            }
            return count > 3;
        }
        
        private bool NeighborCountEqualsThree()
        {
            var count = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.Populated)
                {
                    count++;
                }
            }
            return count == 3;
        }

        private bool NeighborCountLessThanTwo()
        {
            var count = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.Populated)
                {
                    count++;
                }
            }
            return count < 2;
        }
    }
}
