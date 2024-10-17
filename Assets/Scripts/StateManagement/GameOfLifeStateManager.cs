using System.Collections;
using GridSystem;
using pure_unity_methods.StateManagement;
using UnityEditor;
using UnityEngine;

namespace StateManagement
{
    public class GameOfLifeStateManager : SequentialStateManager
    {
        private static bool Paused { get; set; } = true;

        public void StartGame()
        {
            if (GridManager.Instance.IsGameValid())
            {
                StartCoroutine(StartGameCoRoutine());
            }
            else
            {
                MenuManager.Instance.EnableInvalidGame();
            }
        }

        private IEnumerator StartGameCoRoutine()
        {
            GridManager.Instance.OptimiseGrid();
            MenuManager.Instance.EnableMain(false);
            yield return new WaitForSeconds(3);
            ProgressState();
        }

        public void PauseGame()
        {
            Paused = !Paused;
            GridManager.Instance.EnableInteractions(!Paused);
            MenuManager.Instance.EnableMain(Paused);

        }
    }
}
