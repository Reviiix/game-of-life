using System;
using pure_unity_methods.StateManagement;
using StateManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Buttons
{
    [RequireComponent(typeof(Image))]
    public class PlayButton : GameButton
    {
        private Image image;
        private GameOfLifeStateManager state;
        [SerializeField] private Sprite play;
        [SerializeField] private Sprite pause;

        protected override void Start()
        {
            base.Start();
            image = GetComponent<Image>();
            state = (GameOfLifeStateManager)SequentialStateManager.Instance;
        }
    
        protected override void OnClick(Action callBack)
        {
            SwapPauseSprite(state.Paused);
            StartCoroutine(AnimateButton(()=>
            {
                SwapPauseState();
                callBack();
            }));
        }

        private void SwapPauseState()
        {
            if (state.Paused)
            {
                state.StartGame();
            }
            else
            {
                state.PauseGame();
            }
        }
    
        private void SwapPauseSprite(bool p)
        {
            image.sprite = p ? play : pause;
        }
    }
}
