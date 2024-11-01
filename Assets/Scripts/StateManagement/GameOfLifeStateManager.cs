using System.Collections;
using GridSystem;
using Menu;
using pure_unity_methods.StateManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace StateManagement
{
    public class GameOfLifeStateManager : SequentialStateManager
    {
        public bool Paused { get; private set; } = true;
        [Header("Count Down")]
        [SerializeField] [Range(MinimumSeconds, MaximumSeconds)] private int seconds;
        [FormerlySerializedAs("countdown")] [SerializeField]private CountdownStartText countdownStart;
        private const int MinimumSeconds = 1;
        private const int MaximumSeconds = 5;

        public void StartGame()
        {
            Paused = false;
            GridManager.Instance.EnableInteractions(false);
            StartCoroutine(countdownStart.CountDown(seconds, GridManager.Instance.RandomisedColour, () =>
            {
                if (GridManager.Instance.IsGameValid())
                {
                    GridManager.Instance.OptimiseGrid();
                    ProgressState();
                }
                else
                {
                    MenuManager.Instance.EnableInvalidGame();
                    GridManager.Instance.EnableInteractions();
                }
            }));
        }

        public override void ProgressState()
        {
            if (Paused) return;
            base.ProgressState();
        }

        public void ResetGame()
        {
            Paused = true;
            GridManager.Instance.ReInitialise();
        }
        
        public void PauseGame()
        {
            Paused = true;
            GridManager.Instance.EnableInteractions();
        }
    }
}
