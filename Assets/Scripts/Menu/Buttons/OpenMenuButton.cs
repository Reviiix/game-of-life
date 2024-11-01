using System;
using GridSystem;
using UnityEngine;

namespace Menu.Buttons
{
    public class OpenMenuButton : GameButton
    {
        [SerializeField] private bool open;

        protected override void OnClick()
        {
            StartCoroutine(AnimateButton(() =>
            {
                MenuManager.Instance.EnableMain(open);
                GridManager.Instance.EnableInteractions(!open);
            }));
        }

        private void EnableMenu()
        {
            
        }
    }
}
