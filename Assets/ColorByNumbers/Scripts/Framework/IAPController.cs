using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;

#if UNITY_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension; 
#endif

#pragma warning disable 0414 // Reason: Some inspector variables are only used in specific platforms and their usages are removed using #if blocks

namespace BizzyBeeGames.ColorByNumbers
{
	public class IAPController : SingletonComponent<IAPController>
	#if UNITY_IAP
	, IStoreListener
	#endif
	{
		#region Classes

		[System.Serializable]
		private class OnProductPurchasedEvent : UnityEngine.Events.UnityEvent {}

		[System.Serializable]
		private class ProductInfo
		{
			public string					productId			= "";
			public bool						consumable			= false;
			public OnProductPurchasedEvent	onProductPurchased	= null;
		}

		#endregion

		#region Inspector Variables

		[Space]
		[SerializeField] private bool				enableIAP				= false;
		[Space]
		[SerializeField] private bool				anyPurchaseRemovesAds	= false;
		[SerializeField] private Button				restorePurchaseButton	= null;
		[Space]
		[SerializeField] private string				removeAdsProductId		= "";
		[SerializeField] private List<ProductInfo>	productInfos			= null;

		#endregion

		#region Member Variables

		#if UNITY_IAP
		private IStoreController	storeController;
		private IExtensionProvider 	extensionProvider;
		#endif


		#endregion

		#region Properties

		/// <summary>
		/// Returns true id IAP is enabled, false otherwise
		/// </summary>
		public static bool IsEnabled
		{
			get
			{
				#if UNITY_IAP
				return IAPController.Exists() && IAPController.Instance.enableIAP;
				#else
				return false;
				#endif
			}
		}

		/// <summary>
		/// Gets the remove ads product id
		/// </summary>
		public string RemoveAdsProductId { get { return removeAdsProductId; } }

		/// <summary>
		/// Callback that is invoked when the IAPController has successfully initialized and has retrieved the list of products/prices
		/// </summary>
		public System.Action OnInitializedSuccessfully { get; set; }

		/// <summary>
		/// Callback that is invoked when a product is purchased, passes the product id that was purchased
		/// </summary>
		public System.Action<string> OnProductPurchased	{ get; set; }

		/// <summary>
		/// Returns true if IAP is initialized
		/// </summary>
		public bool IsInitialized
		{
			#if UNITY_IAP
			get { return storeController != null && extensionProvider != null; }
			#else
			get { return false; }
			#endif
		}

		/// <summary>
		/// String of non-consumable product ids that have been purchase seperated by tabs
		/// </summary>
		private string PurchasedProductIdsString
		{
			get { return PlayerPrefs.GetString("crossword_purchased_products", ""); }
			set { PlayerPrefs.SetString("crossword_purchased_products", value); }
		}

		/// <summary>
		/// List of non-consumable product ids that have been purchased
		/// </summary>
		private List<string> PurchasedProductIds
		{
			get { return string.IsNullOrEmpty(PurchasedProductIdsString) ? new List<string>() : new List<string>(PurchasedProductIdsString.Split('\t')); }
		}

		#endregion

		#region Unity Methods

		private void Start()
		{
			#if UNITY_IAP

			// Show the restore purchase button if this platform is iOS
			restorePurchaseButton.gameObject.SetActive(Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer);
			restorePurchaseButton.onClick.AddListener(RestorePurchases);

			// Initialize IAP
			ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

			// Add all the product ids to teh builder
			for (int i = 0; i < productInfos.Count; i++)
			{
				builder.AddProduct(productInfos[i].productId, productInfos[i].consumable ? ProductType.Consumable : ProductType.NonConsumable);
			}

			// If the remove ads product id has been set then add the remove ads product id to the builder
			if (!string.IsNullOrEmpty(removeAdsProductId))
			{
				builder.AddProduct(removeAdsProductId, ProductType.NonConsumable);
			}

			UnityPurchasing.Initialize(this, builder);

			#endif
		}

		#endregion

		#region Public Methods

		#if UNITY_IAP

		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			Debug.Log("[IAPController] Initializion Successful");

			storeController		= controller;
			extensionProvider	= extensions;

			if (OnInitializedSuccessfully != null)
			{
				OnInitializedSuccessfully();
			}
		}

		public void OnInitializeFailed(InitializationFailureReason failureReason)
		{
			Debug.LogError("[IAPController] Initializion Failed: " + failureReason);
		}

		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			Debug.LogError("[IAPController] Purchase Failed: productId: " + product.definition.id + ", reason: " + failureReason);
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
		{
			Debug.Log("[IAPController] Purchase successful for product id " + args.purchasedProduct.definition.id);

			// Get the product id for the product that was just purchased
			ProductPurchased(args.purchasedProduct.definition.id, args.purchasedProduct.definition.type == ProductType.Consumable);

			return PurchaseProcessingResult.Complete;
		}

		/// <summary>
		/// Starts the buying process for the given product id
		/// </summary>
		public void BuyProduct(string productId)
		{
			if (IsInitialized)
			{
				Product product = storeController.products.WithID(productId);

				// If the look up found a product for this device's store and that product is ready to be sold ... 
				if (product != null && product.availableToPurchase)
				{
					storeController.InitiatePurchase(product);
				}
			}
		}

		/// <summary>
		/// Gets the products store information
		/// </summary>
		public Product GetProductInformation(string productId)
		{
			if (IsInitialized)
			{
				return storeController.products.WithID(productId);
			}

			return null;
		}

		#endif

		/// <summary>
		/// Returns true if the given product id has been purchased, only for non-consumable products, consumable products will always return false.
		/// </summary>
		public bool IsProductPurchased(string productId)
		{
			return PurchasedProductIds.Contains(productId);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Restores the purchases if platform is iOS or OSX
		/// </summary>
		private void RestorePurchases()
		{
			if (IsInitialized && (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.OSXPlayer))
			{
				#if UNITY_IAP
				extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions((result) => {});
				#endif
			}
		}

		private void ProductPurchased(string productId, bool consumable)
		{
			if (!consumable)
			{
				// Add the product id to the list of purchased products so it appears as purchased in the store
				if (!string.IsNullOrEmpty(PurchasedProductIdsString))
				{
					PurchasedProductIdsString += "\t";
				}

				PurchasedProductIdsString += productId;
			}

			// Invoke the callback so other controllers can update their state
			if (OnProductPurchased != null)
			{
				OnProductPurchased(productId);
			}

			// Invoke the event on the ProductInfo that was just purchased
			for (int i = 0; i < productInfos.Count; i++)
			{
				if (productId == productInfos[i].productId)
				{
					productInfos[i].onProductPurchased.Invoke();
				}
			}

			// If something other than the remove ads product was purchased and anyPurchaseRemovesAds is true call
			// ProductPurchased on the remove ads product id to set it as purchased aswell
			if (productId != removeAdsProductId && anyPurchaseRemovesAds && !IsProductPurchased(removeAdsProductId))
			{
				ProductPurchased(removeAdsProductId, false);
			}
		}

		#endregion
	}
}