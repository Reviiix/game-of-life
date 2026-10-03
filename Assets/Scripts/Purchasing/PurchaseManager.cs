using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GameOfLife.Purchasing
{
    /// <summary>Sells and restores the ad pass through Unity IAP v5: connects (and reconnects) to the store, loads the product and grants it before confirming each purchase.</summary>
    public sealed class PurchaseManager : MonoBehaviour
    {
        private StoreController store;
        private AdPassOwnership adPassOwnership;
        private List<ProductDefinition> productDefinitions;
        private string adPassProductId;
        private Product adPassProduct;
        private bool isAwaitingDeferredApproval;
        private bool isCheckingExistingOwnership;

        public bool IsAdPassAvailable => adPassProduct != null && adPassProduct.availableToPurchase && store.GetConnectionState() == ConnectionState.Connected;
        public bool IsPurchaseInProgress { get; private set; }
        public bool IsRestoreInProgress { get; private set; }
        public string AdPassPrice => adPassProduct != null ? adPassProduct.metadata.localizedPriceString : string.Empty;

        public event Action StoreStateChanged;
        public event Action AdPassPurchased;
        public event Action<PurchaseFailureReason> AdPassPurchaseFailed;
        public event Action AdPassPurchaseDeferred;
        public event Action<bool> RestoreFinished;

        /// <summary>Subscribes to every store event, asks the store to retry lost connections with back-off, then connects; the ad pass is fetched once connected.</summary>
        public void Initialise(string productId, AdPassOwnership ownership)
        {
            adPassProductId = productId;
            adPassOwnership = ownership;
            productDefinitions = new List<ProductDefinition> { new(adPassProductId, ProductType.NonConsumable) };
            store = UnityIAPServices.StoreController();
            SubscribeToStoreEvents();
            store.SetStoreReconnectionRetryPolicyOnDisconnection(new ExponentialBackOffRetryPolicy());
            ConnectToStore();
        }

        /// <summary>Starts buying the ad pass; the result arrives through AdPassPurchased, AdPassPurchaseFailed or AdPassPurchaseDeferred.</summary>
        public void BuyAdPass()
        {
            if (!IsAdPassAvailable || IsPurchaseInProgress || adPassOwnership.IsOwned)
            {
                return;
            }

            SetPurchaseInProgress(true);
            store.PurchaseProduct(adPassProduct);
        }

        /// <summary>Asks the store to re-deliver past purchases, as Apple requires; Unity IAP then fetches purchases, and RestoreFinished reports once they are applied.</summary>
        public void RestorePurchases()
        {
            if (IsRestoreInProgress)
            {
                return;
            }

            SetRestoreInProgress(true);
            store.RestoreTransactions((succeeded, error) =>
            {
                if (!succeeded)
                {
                    Debug.LogWarning($"Restoring purchases did not complete: {error}");
                    FinishRestore(false);
                }
            });
        }

        /// <summary>Reconnects when the player returns to the game after the store connection was lost.</summary>
        private void OnApplicationPause(bool isPaused)
        {
            if (!isPaused && store != null && store.GetConnectionState() == ConnectionState.Disconnected)
            {
                ConnectToStore();
            }
        }

        /// <summary>Stops listening to the store when destroyed.</summary>
        private void OnDestroy()
        {
            if (store != null)
            {
                UnsubscribeFromStoreEvents();
            }
        }

        /// <summary>Connects to the store, logging rather than throwing if it cannot be reached; the next return to the game tries again.</summary>
        private async void ConnectToStore()
        {
            try
            {
                await store.Connect();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not connect to the store; the ad pass cannot be bought until it does. {exception.Message}");
            }
        }

        /// <summary>Subscribes to the success and failure event of every store call, before Connect, because pending purchases can arrive immediately.</summary>
        private void SubscribeToStoreEvents()
        {
            store.OnStoreConnected += OnStoreConnected;
            store.OnStoreDisconnected += OnStoreDisconnected;
            store.OnProductsFetched += OnProductsFetched;
            store.OnProductsFetchFailed += OnProductsFetchFailed;
            store.OnPurchasesFetched += OnPurchasesFetched;
            store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            store.OnPurchasePending += OnPurchasePending;
            store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            store.OnPurchaseFailed += OnPurchaseFailed;
            store.OnPurchaseDeferred += OnPurchaseDeferred;
            store.OnAuthAccountChanged += OnAuthAccountChanged;
        }

        /// <summary>Removes every subscription made in SubscribeToStoreEvents.</summary>
        private void UnsubscribeFromStoreEvents()
        {
            store.OnStoreConnected -= OnStoreConnected;
            store.OnStoreDisconnected -= OnStoreDisconnected;
            store.OnProductsFetched -= OnProductsFetched;
            store.OnProductsFetchFailed -= OnProductsFetchFailed;
            store.OnPurchasesFetched -= OnPurchasesFetched;
            store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            store.OnPurchasePending -= OnPurchasePending;
            store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            store.OnPurchaseFailed -= OnPurchaseFailed;
            store.OnPurchaseDeferred -= OnPurchaseDeferred;
            store.OnAuthAccountChanged -= OnAuthAccountChanged;
            if (store.AppleStoreExtendedPurchaseService != null)
            {
                store.AppleStoreExtendedPurchaseService.OnEntitlementRevoked -= OnAppleEntitlementRevoked;
            }
        }

        /// <summary>Loads the ad pass and the player's existing purchases, and listens for Apple refunds.</summary>
        private void OnStoreConnected()
        {
            if (store.AppleStoreExtendedPurchaseService != null)
            {
                store.AppleStoreExtendedPurchaseService.OnEntitlementRevoked -= OnAppleEntitlementRevoked;
                store.AppleStoreExtendedPurchaseService.OnEntitlementRevoked += OnAppleEntitlementRevoked;
            }

            FetchProductsAndPurchases();
        }

        /// <summary>Disables buying until the store reconnects and ends anything that was waiting on the lost connection.</summary>
        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            Debug.LogWarning($"Store disconnected: {failure.Message}");
            adPassProduct = null;
            if (isCheckingExistingOwnership)
            {
                FinishOwnershipCheck();
            }

            FinishRestore(false);
            SetPurchaseInProgress(false);
        }

        /// <summary>Keeps the fetched ad pass product so its localised price can be shown and it can be bought.</summary>
        private void OnProductsFetched(List<Product> products)
        {
            adPassProduct = store.GetProductById(adPassProductId);
            StoreStateChanged?.Invoke();
        }

        /// <summary>Logs a product that could not be loaded; the ad pass button stays disabled.</summary>
        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"Could not load the ad pass product: {failure.FailureReason}");
            StoreStateChanged?.Invoke();
        }

        /// <summary>Re-applies an ad pass the store already has on record, then completes any restore or already-owned check that was waiting for this list.</summary>
        private void OnPurchasesFetched(Orders orders)
        {
            foreach (var confirmedOrder in orders.ConfirmedOrders)
            {
                if (ContainsAdPass(confirmedOrder))
                {
                    adPassOwnership.Grant();
                }
            }

            if (isCheckingExistingOwnership)
            {
                FinishOwnershipCheck();
            }

            FinishRestore(true);
        }

        /// <summary>Logs a failed purchase lookup and fails anything waiting for it; the saved ownership flag still applies.</summary>
        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogWarning($"Could not fetch existing purchases: {failure.FailureReason} {failure.Message}");
            if (isCheckingExistingOwnership)
            {
                FinishOwnershipCheck();
            }

            FinishRestore(false);
        }

        /// <summary>Grants and saves the ad pass, then confirms the order; thanks the player only for a purchase they started, including an approved Ask to Buy.</summary>
        private void OnPurchasePending(PendingOrder order)
        {
            if (!ContainsAdPass(order))
            {
                Debug.LogWarning("Received a purchase for a product this game does not sell; it was left unconfirmed.");
                return;
            }

            var wasRequestedByPlayer = IsPurchaseInProgress || isAwaitingDeferredApproval;
            isAwaitingDeferredApproval = false;
            adPassOwnership.Grant();
            store.ConfirmPurchase(order);
            if (wasRequestedByPlayer)
            {
                SetPurchaseInProgress(false);
                AdPassPurchased?.Invoke();
            }
        }

        /// <summary>Logs a confirmation the store could not complete; the store re-delivers it on the next launch and the ad pass stays granted.</summary>
        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failedOrder)
            {
                Debug.LogWarning($"The store could not confirm a purchase: {failedOrder.FailureReason} {failedOrder.Details}");
            }
        }

        /// <summary>Reports a failed purchase, except when the store says the pass is already owned: then its purchases are fetched and the result reported once known.</summary>
        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            isAwaitingDeferredApproval = false;
            if (failedOrder.FailureReason == PurchaseFailureReason.DuplicateTransaction)
            {
                isCheckingExistingOwnership = true;
                store.FetchPurchases();
                return;
            }

            if (failedOrder.FailureReason != PurchaseFailureReason.UserCancelled)
            {
                Debug.LogWarning($"Ad pass purchase failed: {failedOrder.FailureReason} {failedOrder.Details}");
            }

            SetPurchaseInProgress(false);
            AdPassPurchaseFailed?.Invoke(failedOrder.FailureReason);
        }

        /// <summary>Reports a purchase waiting for approval, such as Ask to Buy, and remembers it so the approval is thanked when it arrives.</summary>
        private void OnPurchaseDeferred(DeferredOrder deferredOrder)
        {
            isAwaitingDeferredApproval = true;
            SetPurchaseInProgress(false);
            AdPassPurchaseDeferred?.Invoke();
        }

        /// <summary>Reloads everything after the player signs in to a different store account, because the store has just cleared its caches.</summary>
        private void OnAuthAccountChanged()
        {
            adPassProduct = null;
            StoreStateChanged?.Invoke();
            FetchProductsAndPurchases();
        }

        /// <summary>Removes the ad pass when Apple reports it was refunded.</summary>
        private void OnAppleEntitlementRevoked(string productId)
        {
            if (productId == adPassProductId)
            {
                adPassOwnership.Revoke();
            }
        }

        /// <summary>Asks the store for the ad pass product and for any purchases already made.</summary>
        private void FetchProductsAndPurchases()
        {
            store.FetchProducts(productDefinitions);
            store.FetchPurchases();
        }

        /// <summary>Ends an "already owned" check: thanks the player if the pass turned out to be theirs, otherwise reports the purchase as failed.</summary>
        private void FinishOwnershipCheck()
        {
            isCheckingExistingOwnership = false;
            SetPurchaseInProgress(false);
            if (adPassOwnership.IsOwned)
            {
                AdPassPurchased?.Invoke();
            }
            else
            {
                AdPassPurchaseFailed?.Invoke(PurchaseFailureReason.DuplicateTransaction);
            }
        }

        /// <summary>Reports the end of a restore the player asked for; purchase fetches at start-up report nothing.</summary>
        private void FinishRestore(bool succeeded)
        {
            if (!IsRestoreInProgress)
            {
                return;
            }

            SetRestoreInProgress(false);
            RestoreFinished?.Invoke(succeeded);
        }

        /// <summary>Returns whether an order includes the ad pass.</summary>
        private bool ContainsAdPass(Order order)
        {
            foreach (var item in order.CartOrdered.Items())
            {
                if (item.Product != null && item.Product.definition.id == adPassProductId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Records whether a purchase is underway and tells the UI so it can disable or re-enable the button.</summary>
        private void SetPurchaseInProgress(bool inProgress)
        {
            IsPurchaseInProgress = inProgress;
            StoreStateChanged?.Invoke();
        }

        /// <summary>Records whether a restore is underway and tells the UI so the restore button can be disabled meanwhile.</summary>
        private void SetRestoreInProgress(bool inProgress)
        {
            IsRestoreInProgress = inProgress;
            StoreStateChanged?.Invoke();
        }
    }
}
