using GameOfLife.Purchasing;
using GameOfLife.UI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Store
{
    /// <summary>Main menu button that buys the ad pass: shows the store's localised price, is disabled until the store is ready, and disappears once the pass is owned.</summary>
    public sealed class AdPassButton : AnimatedButton
    {
        [SerializeField] private TMP_Text priceLabel;

        private Button button;
        private PurchaseManager purchaseManager;
        private AdPassOwnership adPassOwnership;

        /// <summary>Connects to the store and ownership, then shows the right state straight away.</summary>
        public void Initialise(PurchaseManager purchases, AdPassOwnership ownership)
        {
            button = GetComponent<Button>();
            purchaseManager = purchases;
            adPassOwnership = ownership;
            purchaseManager.StoreStateChanged += Refresh;
            adPassOwnership.OwnershipChanged += OnOwnershipChanged;
            Refresh();
        }

        /// <summary>Starts buying the ad pass.</summary>
        protected override void OnPressed()
        {
            purchaseManager.BuyAdPass();
        }

        /// <summary>Stops listening when destroyed.</summary>
        private void OnDestroy()
        {
            if (purchaseManager)
            {
                purchaseManager.StoreStateChanged -= Refresh;
            }

            if (adPassOwnership != null)
            {
                adPassOwnership.OwnershipChanged -= OnOwnershipChanged;
            }
        }

        /// <summary>Refreshes the button when the pass is bought or refunded.</summary>
        private void OnOwnershipChanged(bool owned)
        {
            Refresh();
        }

        /// <summary>Hides the button if the pass is owned; otherwise shows the price and allows a tap only when the store can sell it and nothing is in progress.</summary>
        private void Refresh()
        {
            gameObject.SetActive(!adPassOwnership.IsOwned);
            button.interactable = purchaseManager.IsAdPassAvailable && !purchaseManager.IsPurchaseInProgress;
            priceLabel.SetText(purchaseManager.AdPassPrice);
        }
    }
}
