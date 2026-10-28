using GameOfLife.Ads;
using UnityEngine;

namespace GameOfLife.UI.HowToPlay
{
    /// <summary>Opens the how-to-play guide on the app's very first launch, holding ads back until it closes, and remembers on the device once it has been closed.</summary>
    public sealed class FirstRunGuide
    {
        private const int Seen = 1;

        private readonly string storageKey;
        private HowToPlayPanel guidePanel;
        private AdManager adManager;

        /// <summary>Whether the guide has never been closed on this device.</summary>
        public bool IsFirstRun => PlayerPrefs.GetInt(storageKey, 0) != Seen;

        /// <summary>Remembers the guide's state under the given key.</summary>
        public FirstRunGuide(string storageKey)
        {
            this.storageKey = storageKey;
        }

        /// <summary>Holds every ad back on the first launch, before ads start loading, so none shows over the guide.</summary>
        public void HoldAdsIfFirstRun(AdManager ads)
        {
            if (!IsFirstRun)
            {
                return;
            }

            adManager = ads;
            adManager.HoldBack();
        }

        /// <summary>Opens the guide on the first launch and waits for it to close.</summary>
        public void ShowIfFirstRun(HowToPlayPanel panel)
        {
            if (!IsFirstRun)
            {
                return;
            }

            guidePanel = panel;
            guidePanel.Closed += FinishFirstRun;
            guidePanel.Open();
        }

        /// <summary>Saves that the guide has been seen, without waiting for it to close.</summary>
        public void MarkSeen()
        {
            PlayerPrefs.SetInt(storageKey, Seen);
            PlayerPrefs.Save();
        }

        /// <summary>Saves that the guide has been seen and lets ads show again, once the first-run guide closes.</summary>
        private void FinishFirstRun()
        {
            guidePanel.Closed -= FinishFirstRun;
            guidePanel = null;
            MarkSeen();
            adManager?.Release();
            adManager = null;
        }
    }
}
