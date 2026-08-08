using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_IOS
using System.Linq;
using UnityEngine.Purchasing;
#endif

public sealed class IosIapService : IDisposable
{
    private const string EntitlementKey =
        "zt3d.ios.ad-free-rewards";

    private string productId = string.Empty;
    private bool purchaseInProgress;
    private bool restoreInProgress;
    private string localizedPrice = string.Empty;
    private Action<string> purchaseErrorCallback;
    private Action<bool, string> restoreCompletionCallback;

#if UNITY_IOS
    private StoreController storeController;
    private Product adFreeRewardsProduct;
#endif

    public event Action StateChanged;

    public bool IsSupported => !string.IsNullOrWhiteSpace(productId);

    public bool HasEntitlement =>
        PlayerPrefs.GetInt(EntitlementKey, 0) == 1;

    public bool IsPurchaseInProgress =>
        purchaseInProgress || restoreInProgress;

    public string LocalizedPrice => localizedPrice;

    public void Configure(IosPlatformSettings settings)
    {
        productId = settings != null
            ? settings.AdFreeRewardsProductId
            : string.Empty;

#if UNITY_IOS && !UNITY_EDITOR
        if (IsSupported)
        {
            _ = InitializeAsync();
        }
#endif
    }

    public void Purchase(Action<string> onUnavailable)
    {
        if (HasEntitlement)
        {
            StateChanged?.Invoke();
            return;
        }

        if (!IsSupported)
        {
            onUnavailable?.Invoke("PURCHASE IS NOT CONFIGURED");
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR
        if (IsPurchaseInProgress)
        {
            onUnavailable?.Invoke("STORE OPERATION ALREADY IN PROGRESS");
            return;
        }

        if (storeController == null || adFreeRewardsProduct == null)
        {
            onUnavailable?.Invoke("STORE IS STILL LOADING");
            return;
        }

        if (!adFreeRewardsProduct.availableToPurchase)
        {
            onUnavailable?.Invoke("PRODUCT UNAVAILABLE");
            return;
        }

        purchaseInProgress = true;
        purchaseErrorCallback = onUnavailable;
        StateChanged?.Invoke();

        try
        {
            storeController.PurchaseProduct(adFreeRewardsProduct);
        }
        catch (Exception exception)
        {
            CompletePurchaseWithError("PURCHASE COULD NOT START");
            Debug.LogWarning(
                $"Apple IAP purchase could not start. {exception.Message}");
        }
#else
        onUnavailable?.Invoke("PURCHASE REQUIRES AN iOS DEVICE");
#endif
    }

    public void Restore(Action<bool, string> onComplete)
    {
        if (!IsSupported)
        {
            onComplete?.Invoke(false, "PURCHASE IS NOT CONFIGURED");
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR
        if (storeController == null)
        {
            onComplete?.Invoke(false, "STORE IS STILL LOADING");
            return;
        }

        if (IsPurchaseInProgress)
        {
            onComplete?.Invoke(
                false,
                "STORE OPERATION ALREADY IN PROGRESS");
            return;
        }

        restoreInProgress = true;
        restoreCompletionCallback = onComplete;
        StateChanged?.Invoke();

        try
        {
            storeController.RestoreTransactions((success, error) =>
            {
                if (success)
                {
                    storeController.FetchPurchases();
                    return;
                }

                CompleteRestore(
                    false,
                    string.IsNullOrWhiteSpace(error)
                        ? "RESTORE FAILED"
                        : error.ToUpperInvariant());
            });
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Apple IAP restore could not start. {exception.Message}");
            CompleteRestore(false, "RESTORE COULD NOT START");
        }
#else
        onComplete?.Invoke(false, "RESTORE REQUIRES AN iOS DEVICE");
#endif
    }

    public void Dispose()
    {
#if UNITY_IOS
        if (storeController != null)
        {
            storeController.OnStoreDisconnected -= OnStoreDisconnected;
            storeController.OnProductsFetched -= OnProductsFetched;
            storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
            storeController.OnPurchasesFetched -= OnPurchasesFetched;
            storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            storeController.OnPurchasePending -= OnPurchasePending;
            storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            storeController.OnPurchaseFailed -= OnPurchaseFailed;
            storeController.OnPurchaseDeferred -= OnPurchaseDeferred;
        }
#endif

        StateChanged = null;
        purchaseErrorCallback = null;
        restoreCompletionCallback = null;
        purchaseInProgress = false;
        restoreInProgress = false;
    }

#if UNITY_IOS
    private async Task InitializeAsync()
    {
        try
        {
            storeController = UnityIAPServices.StoreController();
            storeController.OnStoreDisconnected += OnStoreDisconnected;
            storeController.OnProductsFetched += OnProductsFetched;
            storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            storeController.OnPurchasesFetched += OnPurchasesFetched;
            storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            storeController.OnPurchasePending += OnPurchasePending;
            storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            storeController.OnPurchaseFailed += OnPurchaseFailed;
            storeController.OnPurchaseDeferred += OnPurchaseDeferred;
            storeController.ProcessPendingOrdersOnPurchasesFetched(true);

            await storeController.Connect();
            storeController.FetchProducts(new List<ProductDefinition>
            {
                new(productId, ProductType.NonConsumable)
            });
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"Apple IAP initialization failed. {exception.Message}");
            StateChanged?.Invoke();
        }
    }

    private void OnStoreDisconnected(
        StoreConnectionFailureDescription failure)
    {
        Debug.LogWarning(
            $"Apple IAP store disconnected. {failure.Message}");
        CompletePurchaseWithError("APP STORE CONNECTION LOST");
        CompleteRestore(false, "APP STORE CONNECTION LOST");
    }

    private void OnProductsFetched(List<Product> products)
    {
        adFreeRewardsProduct = products.FirstOrDefault(ProductMatches);
        localizedPrice = adFreeRewardsProduct != null
            ? adFreeRewardsProduct.metadata.localizedPriceString
            : string.Empty;

        if (adFreeRewardsProduct == null)
        {
            Debug.LogWarning(
                $"Apple IAP product was not returned: {productId}");
        }

        StateChanged?.Invoke();
        storeController.FetchPurchases();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning(
            $"Apple IAP products could not be loaded. {failure.FailureReason}");
        StateChanged?.Invoke();
    }

    private void OnPurchasesFetched(Orders orders)
    {
        bool entitlementFound = false;
        foreach (ConfirmedOrder order in orders.ConfirmedOrders)
        {
            if (OrderMatches(order))
            {
                entitlementFound = true;
                GrantEntitlement();
            }
        }

        if (!entitlementFound)
        {
            RevokeEntitlement();
        }

        StateChanged?.Invoke();
        CompleteRestore(true, "PURCHASES RESTORED");
    }

    private void OnPurchasesFetchFailed(
        PurchasesFetchFailureDescription failure)
    {
        Debug.LogWarning(
            $"Apple IAP purchases could not be restored. {failure.Message}");
        StateChanged?.Invoke();
        CompleteRestore(
            false,
            string.IsNullOrWhiteSpace(failure.Message)
                ? "RESTORE FAILED"
                : failure.Message.ToUpperInvariant());
    }

    private void OnPurchasePending(PendingOrder order)
    {
        if (!OrderMatches(order))
        {
            return;
        }

        // Persist the entitlement before acknowledging the transaction.
        GrantEntitlement();
        storeController.ConfirmPurchase(order);
        purchaseInProgress = false;
        purchaseErrorCallback = null;
        StateChanged?.Invoke();
    }

    private void OnPurchaseConfirmed(Order order)
    {
        if (OrderMatches(order))
        {
            GrantEntitlement();
        }

        purchaseInProgress = false;
        purchaseErrorCallback = null;
        StateChanged?.Invoke();
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        if (!OrderMatches(order))
        {
            return;
        }

        string message = order.FailureReason ==
                         PurchaseFailureReason.UserCancelled
            ? "PURCHASE CANCELLED"
            : "PURCHASE FAILED";
        CompletePurchaseWithError(message);
    }

    private void OnPurchaseDeferred(DeferredOrder order)
    {
        if (!OrderMatches(order))
        {
            return;
        }

        CompletePurchaseWithError("PURCHASE AWAITING APPROVAL");
    }

    private bool ProductMatches(Product product)
    {
        return product != null
               && (string.Equals(
                       product.definition.id,
                       productId,
                       StringComparison.Ordinal)
                   || string.Equals(
                       product.uSku,
                       productId,
                       StringComparison.Ordinal));
    }

    private bool OrderMatches(Order order)
    {
        return order?.CartOrdered?.Items()
            .Any(item => ProductMatches(item.Product)) == true;
    }
#endif

    private void GrantEntitlement()
    {
        if (HasEntitlement)
        {
            return;
        }

        PlayerPrefs.SetInt(EntitlementKey, 1);
        PlayerPrefs.Save();
        StateChanged?.Invoke();
    }

    private void RevokeEntitlement()
    {
        if (!HasEntitlement)
        {
            return;
        }

        PlayerPrefs.DeleteKey(EntitlementKey);
        PlayerPrefs.Save();
        StateChanged?.Invoke();
    }

    private void CompletePurchaseWithError(string message)
    {
        if (!purchaseInProgress && purchaseErrorCallback == null)
        {
            return;
        }

        Action<string> callback = purchaseErrorCallback;
        purchaseInProgress = false;
        purchaseErrorCallback = null;
        StateChanged?.Invoke();
        callback?.Invoke(message);
    }

    private void CompleteRestore(bool success, string message)
    {
        if (!restoreInProgress && restoreCompletionCallback == null)
        {
            return;
        }

        Action<bool, string> callback = restoreCompletionCallback;
        restoreInProgress = false;
        restoreCompletionCallback = null;
        StateChanged?.Invoke();
        callback?.Invoke(success, message);
    }
}
