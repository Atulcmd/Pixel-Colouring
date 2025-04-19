using UnityEngine;
using UnityEngine.UI;
using System.Collections;

#if UNITY_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

#pragma warning disable 0414 // Reason: Some inspector variables are only used in specific platforms and their usages are removed using #if blocks

namespace BizzyBeeGames.ColorByNumbers
{
	[RequireComponent(typeof(Button))]
	public class IAPProductButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string	productId		= "";
		[SerializeField] private Text	titleText		= null;
		[SerializeField] private Text	descriptionText	= null;
		[SerializeField] private Text	priceText		= null;

		#endregion

		#region Private Methods

		private Button button;

		#endregion

		#region Properties

		#endregion

		#region Unity Methods

		private void Start()
		{
			button = gameObject.GetComponent<Button>();

			button.onClick.AddListener(OnClicked);

			UpdateButton();
			
			if (IAPController.IsEnabled)
			{
				IAPController.Instance.OnInitializedSuccessfully	+= UpdateButton;
				IAPController.Instance.OnProductPurchased			+= OnProductPurchased;
			}
		}

		#endregion

		#region Private Methods

		private void OnClicked()
		{
			#if UNITY_IAP
			IAPController.Instance.BuyProduct(productId);
			#endif
		}

		private void OnProductPurchased(string purchasedProductId)
		{
			if (productId == purchasedProductId)
			{
				UpdateButton();
			}
		}

		private void UpdateButton()
		{
			if (IAPController.IsEnabled && IAPController.Instance.IsInitialized)
			{
				#if UNITY_IAP
				Product product = IAPController.Instance.GetProductInformation(productId);

				if (product != null && product.availableToPurchase)
				{
					button.interactable = true;

					SetupButton(product);
				}
				else
				{
					button.interactable = false;
				}
				#endif
			}
			else
			{
				button.interactable = false;
			}
		}

		#if UNITY_IAP
		private void SetupButton(Product product)
		{
			if (IAPController.Instance.IsProductPurchased(productId))
			{
				// If the product has been purchased then hide the button (Only for non-consumable products)
				gameObject.SetActive(false);
			}
			else
			{
				gameObject.SetActive(true);

				if (priceText != null)
				{
					priceText.text = product.metadata.localizedPriceString;
				}

				if (titleText != null)
				{
					#if UNITY_EDITOR
					titleText.text = productId;
					#else
					string title = product.metadata.localizedTitle;
					
					// Strip the "(App Name)" text that is included by google for some reason
					int startIndex	= title.LastIndexOf('(');
					int endIndex	= title.LastIndexOf(')');
					
					if (startIndex > 0 && endIndex > 0 && startIndex < endIndex)
					{
					title = title.Remove(startIndex, endIndex - startIndex + 1);
					title = title.Trim();
					}
					
					titleText.text = title;
					#endif
				}

				if (descriptionText != null)
				{
					descriptionText.text = product.metadata.localizedDescription;
				}
			}
		}
		#endif

		#endregion
	}
}
