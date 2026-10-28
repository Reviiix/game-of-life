using System;
using System.Collections.Generic;
using GameOfLife.HowToPlay;
using GameOfLife.Theming;
using GameOfLife.UI.Buttons;
using GameOfLife.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.HowToPlay
{
    /// <summary>The how-to-play dialog: builds a card per guide step under the Classic and Versus tabs once, shows one tab at a time and runs only that tab's demo boards while the dialog is open.</summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class HowToPlayPanel : MonoBehaviour
    {
        [SerializeField] private GuideStepView stepPrefab;
        [SerializeField] private RectTransform classicSteps;
        [SerializeField] private RectTransform versusSteps;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TabButton classicTab;
        [SerializeField] private TabButton versusTab;

        private ScreenPanel screenPanel;
        private GuideStepView[] classicViews;
        private GuideStepView[] versusViews;
        private bool isVersusShown;
        private bool isOpen;

        public event Action Closed;

        /// <summary>Builds a card for every step of both tabs, listens to the tabs and the dialog, and starts on the Classic tab.</summary>
        public void Initialise(HowToPlayGuide guide, ThemeService theme)
        {
            screenPanel = GetComponent<ScreenPanel>();
            classicViews = BuildSteps(guide.ClassicSteps, classicSteps, theme);
            versusViews = BuildSteps(guide.VersusSteps, versusSteps, theme);
            classicTab.Pressed += ShowClassic;
            versusTab.Pressed += ShowVersus;
            screenPanel.Hidden += ReportClosed;
            SelectTab(false);
        }

        /// <summary>Opens the guide on the Classic tab, scrolled to the top, with its demos running.</summary>
        public void Open()
        {
            isOpen = true;
            SelectTab(false);
            screenPanel.Show();
        }

        /// <summary>Stops listening to the tabs and the dialog when destroyed.</summary>
        private void OnDestroy()
        {
            if (classicTab)
            {
                classicTab.Pressed -= ShowClassic;
            }

            if (versusTab)
            {
                versusTab.Pressed -= ShowVersus;
            }

            if (screenPanel)
            {
                screenPanel.Hidden -= ReportClosed;
            }
        }

        /// <summary>Switches to the Classic tab.</summary>
        private void ShowClassic()
        {
            SelectTab(false);
        }

        /// <summary>Switches to the Versus tab.</summary>
        private void ShowVersus()
        {
            SelectTab(true);
        }

        /// <summary>Shows one tab's cards, scrolled to the top, hides the other's (which stops its demos) and starts the shown demos while the dialog is open.</summary>
        private void SelectTab(bool versus)
        {
            isVersusShown = versus;
            classicSteps.gameObject.SetActive(!versus);
            versusSteps.gameObject.SetActive(versus);
            classicTab.SetSelected(!versus);
            versusTab.SetSelected(versus);
            scroll.content = versus ? versusSteps : classicSteps;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            if (isOpen)
            {
                PlayDemos(versus ? versusViews : classicViews);
            }
        }

        /// <summary>Stops the shown demos where they are once the dialog starts hiding, so they fade out still, and tells listeners it closed.</summary>
        private void ReportClosed()
        {
            isOpen = false;
            StopDemos(isVersusShown ? versusViews : classicViews);
            Closed?.Invoke();
        }

        /// <summary>Instantiates one card per step under the tab's list.</summary>
        private GuideStepView[] BuildSteps(IReadOnlyList<GuideStep> steps, RectTransform list, ThemeService theme)
        {
            var views = new GuideStepView[steps.Count];
            for (var index = 0; index < steps.Count; index++)
            {
                views[index] = Instantiate(stepPrefab, list);
                views[index].Initialise(steps[index], theme);
            }

            return views;
        }

        /// <summary>Starts every demo in the list from its first frame.</summary>
        private static void PlayDemos(GuideStepView[] views)
        {
            foreach (var view in views)
            {
                view.PlayDemo();
            }
        }

        /// <summary>Stops every demo in the list where it is; playing it again starts from its first frame.</summary>
        private static void StopDemos(GuideStepView[] views)
        {
            foreach (var view in views)
            {
                view.StopDemo();
            }
        }
    }
}
