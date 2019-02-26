using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;

public struct IapProduct
{
    public string Id;
    public ProductType Type;

    public IapProduct(string InId, ProductType InType)
    {
        Id = InId;
        Type = InType;
    }
}
public sealed class IapPurchaser : MonoBehaviour, IStoreListener
{
    private static IapPurchaser instance;

    public static IapPurchaser Instance
    {
        get
        {
            if (null == instance)
            {
                instance = (new GameObject("IAP")).AddComponent<IapPurchaser>();
            }
            return instance;
        }
    }

    private static IStoreController storeController;          // The Unity Purchasing system.
    private static IExtensionProvider storeExtensionProvider; // The store-specific Purchasing subsystems.
    private UnityAction<int> initializeResultAction;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        orderQueue = new Dictionary<string, UnityAction<int, Product>>(5);
    }
    public bool IsInitialized()
    {
        return storeController != null && storeExtensionProvider != null;
    }

    public void Initialize(IapProduct[] InProducts, UnityAction<int> InInitResultAction)
    {
        Debug.Log("[IapPurchaser]    Initialize with Consumable and NonConsumable.");
        if (IsInitialized())
        {
            Debug.Log("[IapPurchaser]    Multiple initialization.");
            if (null != InInitResultAction) InInitResultAction.Invoke(-1);
            return;
        }

        initializeResultAction = InInitResultAction;

        ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        int len = InProducts.Length;
        for (int i = 0; i < len; i++)
        {
            Debug.Log(string.Format("[IapPurchaser]    Project: Id = {0}, Type = {1};", InProducts[i].Id, InProducts[i].Type));
            builder.AddProduct(InProducts[i].Id, InProducts[i].Type);
        }
        UnityPurchasing.Initialize(this, builder);
    }

    public void Initialize(IapProduct[] InProducts, string InSubscriptionId,
        string InAppleSubscription, string InGooglePlaySubscription,
        UnityAction<int> InInitResultAction)
    {
        Debug.Log("[IapPurchaser]    Initialize with Consumable, NonConsumable and Subscription.");

        if (IsInitialized())
        {
            Debug.Log("[IapPurchaser]    Multiple initialization.");
            if (null != InInitResultAction) InInitResultAction.Invoke(-1);
            return;
        }

        initializeResultAction = InInitResultAction;

        ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        int len = InProducts.Length;
        for (int i = 0; i < len; i++)
        {
            Debug.Log(string.Format("[IapPurchaser]    Project: Id = {0}, Type = {1};", InProducts[i].Id, InProducts[i].Type));
            builder.AddProduct(InProducts[i].Id, InProducts[i].Type);
        }

        Debug.Log(string.Format("[IapPurchaser]    Subscription: Apple = {0}, Google = {1};", InAppleSubscription, InGooglePlaySubscription));
        builder.AddProduct(InSubscriptionId, ProductType.Subscription, new IDs(){
            { InAppleSubscription, AppleAppStore.Name },
            { InGooglePlaySubscription, GooglePlay.Name },
        });

        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController InController, IExtensionProvider InExtensions)
    {
        Debug.Log("[IapPurchaser]    OnInitialized: Succeed.");

        if (null != initializeResultAction) initializeResultAction.Invoke(-1);
        storeController = InController;
        storeExtensionProvider = InExtensions;
    }

    public void OnInitializeFailed(InitializationFailureReason InError)
    {
        Debug.Log("[IapPurchaser]    OnInitialized:  failure, Reason:" + InError);

        if (null != initializeResultAction) initializeResultAction.Invoke((int)InError);
    }

    private Dictionary<string, UnityAction<int, Product>> orderQueue;
    public void BuyProductId(string InProductId, UnityAction<int, Product> InBuyResultAction)
    {
        if (IsInitialized())
        {
            Product product = storeController.products.WithID(InProductId);

            if (product != null && product.availableToPurchase)
            {
                Debug.Log(string.Format("[IapPurchaser]    Buy product id: '{0}'", product.definition.id));

                orderQueue.Add(InProductId, InBuyResultAction);

                storeController.InitiatePurchase(product);
            }
            else
            {
                Debug.Log("[IapPurchaser]    UnityIAP OnInitialized: BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        else
        {
            Debug.Log("[IapPurchaser]    UnityIAP BuyProductID FAIL, Not initialized.");
        }
    }

    public void RestorePurchases()
    {
        if (IsInitialized())
        {
            // If we are running on an Apple device ... 
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                // ... begin restoring purchases
                Debug.Log("RestorePurchases started ...");

                // Fetch the Apple store-specific subsystem.
                var apple = storeExtensionProvider.GetExtension<IAppleExtensions>();
                // Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
                // the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
                apple.RestoreTransactions((result) =>
                {
                    // The first phase of restoration. If no more responses are received on ProcessPurchase then 
                    // no purchases are available to be restored.
                    Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
                });
            }
            // Otherwise ...
            else
            {
                // We are not running on an Apple device. No work is necessary to restore purchases.
                Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
            }
        }
        else
        {
            Debug.Log("RestorePurchases FAIL. Not initialized.");
        }
    }


    public void OnPurchaseFailed(Product InProduct, PurchaseFailureReason InReason)
    {
        Debug.Log(string.Format("[IapPurchaser]    OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", InProduct.definition.storeSpecificId, InReason));

        UnityAction<int, Product> action;
        if (orderQueue.TryGetValue(InProduct.transactionID, out action))
        {
            if (null != action) action.Invoke((int)InReason, InProduct);
        }
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs InArgs)
    {
        Debug.LogError(string.Format("[IapPurchaser]    definition = {0},\nmetadata={1}\navailableToPurchase={2},\ntransactionID={3},\nhasReceipt={4}\nreceipt={5}",
            string.Format("ProductDefinition:[id={0},storeSpecificId={1},type={2},enabled={3},]", InArgs.purchasedProduct.definition.id, InArgs.purchasedProduct.definition.storeSpecificId, InArgs.purchasedProduct.definition.type, InArgs.purchasedProduct.definition.enabled),
            string.Format("ProductMetadata:[localizedPriceString={0},localizedTitle={1},localizedDescription={2},isoCurrencyCode={3},localizedPrice={4}]", InArgs.purchasedProduct.metadata.localizedPriceString, InArgs.purchasedProduct.metadata.localizedTitle, InArgs.purchasedProduct.metadata.localizedDescription, InArgs.purchasedProduct.metadata.isoCurrencyCode, InArgs.purchasedProduct.metadata.localizedPrice),
            InArgs.purchasedProduct.availableToPurchase,
            InArgs.purchasedProduct.transactionID,
            InArgs.purchasedProduct.hasReceipt,
            InArgs.purchasedProduct.receipt));

        Debug.Log(string.Format("[IapPurchaser]    ProcessPurchase: Succeed. Product: '{0}'", InArgs.purchasedProduct.definition.id));

        UnityAction<int, Product> action;
        if (orderQueue.TryGetValue(InArgs.purchasedProduct.definition.id, out action))
        {
            orderQueue.Remove(InArgs.purchasedProduct.definition.id);
            if (null != action) action.Invoke(-1, InArgs.purchasedProduct);
        }
        else
        {
            Debug.Log(string.Format("[IapPurchaser]    ProcessPurchase: Restore. Product: '{0}'", InArgs.purchasedProduct.definition.id));
        }

        return PurchaseProcessingResult.Complete;
    }
}
