﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace BizzyBeeGames.ColorByNumbers
{
	[RequireComponent(typeof(RectTransform))]
	public class Screen : MonoBehaviour
	{
		#region Inspector Variables

		public string 						id;
		[Space]
		public bool							showBannerAd;
		public AdsController.BannerPosition	bannerPosition;
		public Color						bannerPlacementColor;
		[Space]
		public bool adjustTopSafeAreaOnIphoneX;
		public bool adjustBottomSafeAreaOnIphoneX;

		#endregion

		#region Member Variables

		private GameObject adPlacement;

		#endregion

		#region Properties

		public RectTransform RectT { get { return gameObject.GetComponent<RectTransform>(); } }

		#endregion

		#region Unity Methods

		private void OnDestroy()
		{
			#if ADMOB
			if (AdsController.Exists())
			{
				AdsController.Instance.OnAdsRemoved -= OnAdsRemoved;
			}
			#endif
		}

		#endregion

		#region Public Methods

		public virtual void Initialize() 
		{
			#if UNITY_IOS
			if (UnityEngine.iOS.Device.generation == UnityEngine.iOS.DeviceGeneration.iPhoneX)
			{
				Canvas canvas = Utilities.GetCanvas(transform);

				if (bannerPosition == AdsController.BannerPosition.Top || adjustTopSafeAreaOnIphoneX)
				{
					float topBarHeight = UnityEngine.Screen.height - UnityEngine.Screen.safeArea.yMax;
					float adjustHeight = (1f / canvas.scaleFactor) * topBarHeight;

					RectT.offsetMax = new Vector2(0, -adjustHeight);
				}

				if (bannerPosition == AdsController.BannerPosition.Bottom || adjustBottomSafeAreaOnIphoneX)
				{
					float bottomBarHeight = UnityEngine.Screen.safeArea.yMin;
					float adjustHeight = (1f / canvas.scaleFactor) * bottomBarHeight;

					RectT.offsetMin = new Vector2(0, adjustHeight);
				}
			}
			#endif

			#if ADMOB
			if (AdsController.Exists() && AdsController.Instance.IsBannerAdsEnabled && showBannerAd)
			{
				// Need to setup the UI so the new ad doesnt block anything
				SetupScreenToShowBannerAds();

				// Add a listener so we can remove the ad placement object if ads are removed
				AdsController.Instance.OnAdsRemoved += OnAdsRemoved;
			}
			#endif
		}

		private void Start()
		{
			//Canvas canvas = Utilities.GetCanvas(transform);

			//float yMin = UnityEngine.Screen.safeArea.yMin;
			//float yMax = UnityEngine.Screen.safeArea.yMax;

			//Debug.Log("yMin: " + yMin);
			//Debug.Log("yMax: " + yMax);

			//if (bannerPosition == AdsController.BannerPosition.Top)
			//{
			//	float topBarHeight = UnityEngine.Screen.height - yMax;
			//	float adjustHeight = (1f / canvas.scaleFactor) * topBarHeight;

			//	Debug.Log("topBarHeight: " + topBarHeight);
			//	Debug.Log("adjustHeight: " + adjustHeight);
			//}
			//else
			//{
			//	float bottomBarHeight = yMin;
			//	float adjustHeight = (1f / canvas.scaleFactor) * bottomBarHeight;

			//	Debug.Log("bottomBarHeight: " + bottomBarHeight);
			//	Debug.Log("adjustHeight: " + adjustHeight);
			//}
		}

		public virtual void OnShowing(string data)
		{
			#if ADMOB
			if (AdsController.Exists() && AdsController.Instance.IsBannerAdsEnabled)
			{
				if (showBannerAd)
				{
					AdsController.Instance.ShowBannerAd(bannerPosition);
				}
				else
				{
					AdsController.Instance.HideBannerAd();
				}
			}
			#endif
		}

		#endregion

		#region Private Methods

		private void OnAdsRemoved()
		{
			// Destroy the ad placement object if ads are removed
			if (adPlacement != null)
			{
				Destroy(adPlacement);
			}
		}

		private void SetupScreenToShowBannerAds()
		{
			float screenHeight = RectT.rect.height;
			float bannerHeight = AdsController.Instance.BannerHeightInPixels / Utilities.GetCanvas(transform).scaleFactor;

			GameObject screenContent = new GameObject("screen_content");

			// The banner adds take up 130 pixels on a canvas whos scale is set to 1080x1920, so the remaining height for the screen is 1920 - 130 = 1790
			screenContent.AddComponent<LayoutElement>().preferredHeight = screenHeight - bannerHeight;

			// Add the new screen content object to this screen
			screenContent.transform.SetParent(transform, false);

			// Move all the children of this screen to the new screen content object
			for (int i = transform.childCount - 1; i >= 0; i--)
			{
				Transform childTransform = transform.GetChild(i);

				if (childTransform != screenContent.transform)
				{
					childTransform.SetParent(screenContent.transform, false);
					childTransform.SetAsFirstSibling();
				}
			}

			// Create a spacer for where the add will go
			adPlacement = new GameObject("ad_placement");

			// Ads take up 130 pixels on a canvas whos scale is set to 1080x1920
			adPlacement.AddComponent<LayoutElement>().preferredHeight	= bannerHeight;
			adPlacement.AddComponent<Image>().color						= bannerPlacementColor;

			// Add the ad placement as a child of this screen
			adPlacement.transform.SetParent(transform, false);

			// Set the ads position
			if (bannerPosition == AdsController.BannerPosition.Top)
			{
				adPlacement.transform.SetAsFirstSibling();
			}
			else
			{
				adPlacement.transform.SetAsLastSibling();
			}

			// Add a vertical layout group to auto layout the screen content
			gameObject.AddComponent<VerticalLayoutGroup>();
		}

		#endregion
	}
}
