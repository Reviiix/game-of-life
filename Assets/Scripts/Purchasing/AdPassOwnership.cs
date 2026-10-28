using System;
using UnityEngine;

namespace GameOfLife.Purchasing
{
    /// <summary>Whether this device owns the ad pass; saved locally so ads stay off from the first frame, and kept in step with the store by PurchaseManager.</summary>
    public sealed class AdPassOwnership
    {
        private readonly string storageKey;

        public bool IsOwned { get; private set; }

        public event Action<bool> OwnershipChanged;

        /// <summary>Loads the saved ownership flag stored under the given key.</summary>
        public AdPassOwnership(string storageKey)
        {
            this.storageKey = storageKey;
            IsOwned = PlayerPrefs.GetInt(storageKey, 0) == 1;
        }

        /// <summary>Records that the ad pass is owned and saves it immediately, so it is stored before the purchase is confirmed.</summary>
        public void Grant()
        {
            SetOwned(true);
        }

        /// <summary>Removes ownership, for example when Apple reports a refund.</summary>
        public void Revoke()
        {
            SetOwned(false);
        }

        /// <summary>Saves a change of ownership and tells listeners; does nothing if ownership is unchanged.</summary>
        private void SetOwned(bool owned)
        {
            if (IsOwned == owned)
            {
                return;
            }

            IsOwned = owned;
            PlayerPrefs.SetInt(storageKey, owned ? 1 : 0);
            PlayerPrefs.Save();
            OwnershipChanged?.Invoke(owned);
        }
    }
}
