using System;
using GameOfLife.Theming;
using UnityEngine;

namespace GameOfLife.UI.Buttons
{
    /// <summary>One tab of a tab row: raises Pressed once its spring settles, and shows whether it is the selected tab through its panel and label colours.</summary>
    public sealed class TabButton : AnimatedButton
    {
        [SerializeField] private ThemedGraphic panelColour;
        [SerializeField] private ThemedGraphic labelColour;
        [Tooltip("Panel and label colours of the selected tab.")]
        [SerializeField] private ThemeColour selectedPanel = ThemeColour.Ink;
        [SerializeField] private ThemeColour selectedLabel = ThemeColour.InkInverse;
        [Tooltip("Panel and label colours of a tab that is not selected.")]
        [SerializeField] private ThemeColour unselectedPanel = ThemeColour.SurfaceStrong;
        [SerializeField] private ThemeColour unselectedLabel = ThemeColour.Ink;

        public event Action Pressed;

        /// <summary>Colours the tab as selected or not.</summary>
        public void SetSelected(bool selected)
        {
            panelColour.SetColour(selected ? selectedPanel : unselectedPanel);
            labelColour.SetColour(selected ? selectedLabel : unselectedLabel);
        }

        /// <summary>Tells the tab row the tab was pressed.</summary>
        protected override void OnPressed()
        {
            Pressed?.Invoke();
        }
    }
}
