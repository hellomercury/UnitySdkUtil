﻿using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    public InputField ProductID;
    public Button BuyBtn;
    public Button ConsumableBtn, NonConsumableBtn;

    void Awake()
    {
        IapPurchaser.Instance.Initialize(new IapProduct[]
        {
            new IapProduct("com.szn.unitysdkutil.10", ProductType.Consumable),
            new IapProduct("com.szn.unitysdkutil.rmads", ProductType.NonConsumable), 
        },
            InResult =>
           {
               Debug.Log("[IapPurchaser]    Initialize result = " + (InResult == -1 ? "Succeed" : "Fail"));
           });
    }
    // Use this for initialization
    void Start()
    {
        BuyBtn.onClick.AddListener(() => IapPurchaser.Instance.BuyProductId(ProductID.text, (InProductId, InProduct) =>
          {
              Debug.LogError("[IapPurchaser]    Buy " + InProductId + "-" + InProduct.transactionID + ",\n" + InProduct.metadata.isoCurrencyCode);
          }));

        ConsumableBtn.onClick.AddListener(() => IapPurchaser.Instance.BuyProductId("com.szn.unitysdkutil.10", (InProductId, InProduct) =>
        {
            Debug.LogError("[IapPurchaser]    Buy Consumable = " + InProductId + "-" + InProduct.transactionID + ",\n" + InProduct.metadata.isoCurrencyCode);
        }));


        NonConsumableBtn.onClick.AddListener(() => IapPurchaser.Instance.BuyProductId("com.szn.unitysdkutil.rmads", (InProductId, InProduct) =>
        {
            Debug.LogError("[IapPurchaser]    Buy NonConsumable = " + InProductId + "-" + InProduct.transactionID + ",\n" + InProduct.metadata.isoCurrencyCode);
        }));
    }
}
