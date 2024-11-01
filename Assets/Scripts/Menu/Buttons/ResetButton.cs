using System;
using pure_unity_methods.StateManagement;
using StateManagement;

namespace Menu.Buttons
{
    public class ResetButton : GameButton
    {
        protected override void OnClick(Action callBack)
        {
            StartCoroutine(AnimateButton(() =>
            {
                AreYouSure();
                callBack();
            }));
        }

        private void AreYouSure()
        {
            Reset();
        }

        private void Reset()
        { 
            (SequentialStateManager.Instance as GameOfLifeStateManager)?.ResetGame();
        }
    }
}
