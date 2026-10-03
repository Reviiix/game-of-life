using GameOfLife.Audio;
using GameOfLife.Purchasing;
using GameOfLife.UI.Screens;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GameOfLife.UI.Store
{
    /// <summary>Tells the player how an ad pass purchase or restore went, using the message dialog; a cancelled purchase shows nothing.</summary>
    public sealed class PurchaseFeedback : MonoBehaviour
    {
        private const string PurchasedMessage = "ADS REMOVED.\nTHANK YOU FOR YOUR SUPPORT!";
        private const string DeferredMessage = "YOUR PURCHASE IS WAITING FOR APPROVAL.";
        private const string FailedMessage = "THE PURCHASE DID NOT GO THROUGH.\nPLEASE TRY AGAIN.";
        private const string RestoredMessage = "PURCHASES RESTORED.\nADS ARE REMOVED.";
        private const string NothingToRestoreMessage = "THERE ARE NO PURCHASES TO RESTORE.";
        private const string RestoreFailedMessage = "PURCHASES WERE NOT RESTORED.";

        [SerializeField] private MessagePanel messagePanel;

        private PurchaseManager purchaseManager;
        private AdPassOwnership adPassOwnership;
        private AudioManager audioManager;

        /// <summary>Listens to every purchase and restore outcome.</summary>
        public void Initialise(PurchaseManager purchases, AdPassOwnership ownership, AudioManager audio)
        {
            purchaseManager = purchases;
            adPassOwnership = ownership;
            audioManager = audio;
            purchaseManager.AdPassPurchased += OnPurchased;
            purchaseManager.AdPassPurchaseDeferred += OnDeferred;
            purchaseManager.AdPassPurchaseFailed += OnFailed;
            purchaseManager.RestoreFinished += OnRestoreFinished;
        }

        /// <summary>Stops listening when destroyed.</summary>
        private void OnDestroy()
        {
            if (!purchaseManager)
            {
                return;
            }

            purchaseManager.AdPassPurchased -= OnPurchased;
            purchaseManager.AdPassPurchaseDeferred -= OnDeferred;
            purchaseManager.AdPassPurchaseFailed -= OnFailed;
            purchaseManager.RestoreFinished -= OnRestoreFinished;
        }

        /// <summary>Thanks the player with the win jingle.</summary>
        private void OnPurchased()
        {
            audioManager.Play(SoundEffect.MatchWon);
            messagePanel.ShowMessage(PurchasedMessage);
        }

        /// <summary>Explains that the purchase needs approval, for example Ask to Buy.</summary>
        private void OnDeferred()
        {
            messagePanel.ShowMessage(DeferredMessage);
        }

        /// <summary>Reports a failed purchase, but stays silent when the player cancelled it themselves.</summary>
        private void OnFailed(PurchaseFailureReason reason)
        {
            if (reason == PurchaseFailureReason.UserCancelled)
            {
                return;
            }

            audioManager.Play(SoundEffect.Invalid);
            messagePanel.ShowMessage(FailedMessage);
        }

        /// <summary>Reports whether restoring found the ad pass; a failure is worded neutrally and silently, because cancelling Apple's sign-in looks the same as an error.</summary>
        private void OnRestoreFinished(bool succeeded)
        {
            if (!succeeded)
            {
                messagePanel.ShowMessage(RestoreFailedMessage);
                return;
            }

            messagePanel.ShowMessage(adPassOwnership.IsOwned ? RestoredMessage : NothingToRestoreMessage);
        }
    }
}
