using GameOfLife.Purchasing;
using GameOfLife.UI.Buttons;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.UI.Store
{
    /// <summary>Asks the store to restore the ad pass; shown only on iOS, where Apple requires it (Android restores automatically at launch and the Editor's fake store cannot restore), disabled while restoring and hidden once owned.</summary>
    public sealed class RestorePurchasesButton : AnimatedButton
    {
        private Button button;
        private PurchaseManager purchaseManager;
        private AdPassOwnership adPassOwnership;

        /// <summary>Connects to the store and ownership, then shows or hides the button.</summary>
        public void Initialise(PurchaseManager purchases, AdPassOwnership ownership)
        {
            button = GetComponent<Button>();
            purchaseManager = purchases;
            adPassOwnership = ownership;
            purchaseManager.StoreStateChanged += RefreshInteractable;
            adPassOwnership.OwnershipChanged += OnOwnershipChanged;
            OnOwnershipChanged(adPassOwnership.IsOwned);
            RefreshInteractable();
        }

        /// <summary>Starts restoring purchases.</summary>
        protected override void OnPressed()
        {
            purchaseManager.RestorePurchases();
        }

        /// <summary>Stops listening when destroyed.</summary>
        private void OnDestroy()
        {
            if (purchaseManager)
            {
                purchaseManager.StoreStateChanged -= RefreshInteractable;
            }

            if (adPassOwnership != null)
            {
                adPassOwnership.OwnershipChanged -= OnOwnershipChanged;
            }
        }

        /// <summary>Disables the button while a restore is already running, since Apple's sign-in can take several seconds.</summary>
        private void RefreshInteractable()
        {
            button.interactable = !purchaseManager.IsRestoreInProgress;
        }

        /// <summary>Shows the button only where restoring is needed and only while the pass is not owned.</summary>
        private void OnOwnershipChanged(bool owned)
        {
            gameObject.SetActive(Application.platform == RuntimePlatform.IPhonePlayer && !owned);
        }
    }
}
