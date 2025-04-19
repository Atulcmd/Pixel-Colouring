using System;
using System.Collections.Generic;

using UnityEngine;

#if ADMOB
using GoogleMobileAds;
using GoogleMobileAds.Api;
#endif

#pragma warning disable 0414 // Reason: Some inspector variables are only used in specific platforms and their usages are removed using #if blocks

namespace BizzyBeeGames.ColorByNumbers
{
	public class AdsController : SingletonComponent<AdsController>
	{
		#region Enums

		public enum BannerPosition
		{
			Top,
			Bottom
		}

		public enum InterstitialType
		{
			UnityAds,
			AdMob
		}

		private enum BannerState
		{
			Idle,
			Loading,
			Loaded,
			Showing
		}

		#if ADMOB
		private enum AdMobEvent
		{
			OnTopBannerLoaded,
			OnTopBannerFailedToLoad,
			OnBottomBannerLoaded,
			OnBottomBannerFailedToLoad,
			OnInterstitalAdLoaded,
			OnInterstitalAdClosed
		}
		#endif

		#endregion

		#region Inspector Variables

		[SerializeField] private bool				enableAdMobBannerAds		= false;
		[SerializeField] private string				androidBannerAdUnitID		= "";
		[SerializeField] private string				iosBannerAdUnitID			= "";

		[SerializeField] private bool				enableInterstitialAds		= false;
		[SerializeField] private InterstitialType 	interstitialType			= InterstitialType.AdMob;
		[SerializeField] private string				androidInterstitialAdUnitID	= "";
		[SerializeField] private string				iosInterstitialAdUnitID		= "";
		[SerializeField] private bool				enableUnityAdsInEditor		= false;
		[SerializeField] private string				placementId					= "";

		[SerializeField] private string				admobAndroidAppId;
		[SerializeField] private string				admobIOSAppId;
		[SerializeField] private string				unityAndroidGameId;
		[SerializeField] private string				unityIOSGameId;

		#endregion

		#region Member Variables

		#if ADMOB
		private BannerView		topBanner;
		private BannerView		bottomBanner;
		private InterstitialAd	interstitial;

		// The events invoked by AdMob Banner and Interstital objects are not invoked on the main thread, this can cause the app to crash if any Unity specific
		// things happen in the callback for the event (Such as creating a Texture2D) So we must add the events to this queue and run then in the Update method
		// so that they are execture on the main Unity thread
		private List<AdMobEvent> adMobEventQueue = new List<AdMobEvent>();

		// Lock used so that the adMobEventQueue list is only used on one thread at a time
		private static object adMobEventQueueLock = new object();
		#endif

		private BannerState		topBannerState;
		private BannerState		bottomBannerState;
		private float			bannerSize;

		private bool			isInterstitialAdLoaded;
		private System.Action	interstitialAdClosedCallback;

		#endregion

		#region Properties

		public bool IsAdsEanbledInPlatform
		{
			get
			{
				#if !UNITY_ANDROID && !UNITY_IPHONE
				return false;
				#else
				// We want to return true when in the editor so we can see the effects of layout changes with respect to banner ads
				return true;
				#endif
			}
		}

		/// <summary>
		/// Gets the height of the banner in pixels
		/// </summary>
		/// <value>The get banner height in pixels.</value>
		public float BannerHeightInPixels
		{
			get
			{
				if (bannerSize == 0)
				{
					#if !ADMOB || UNITY_EDITOR
					bannerSize = 130f;
					#else
					BannerView bannerView = new BannerView(BannderAdUnitId, AdSize.SmartBanner, AdPosition.Bottom);

					bannerSize = bannerView.GetHeightInPixels();

					bannerView.Destroy();
					#endif
				}

				return bannerSize;
			}
		}

		public bool				IsBannerAdsEnabled			{ get { return !RemoveAds && IsAdsEanbledInPlatform && enableAdMobBannerAds; } }
		public bool				IsInterstitialAdsEnabled	{ get { return !RemoveAds && IsAdsEanbledInPlatform && enableInterstitialAds; } }
		public System.Action	OnAdsRemoved				{ get; set; }

		#if UNITY_ANDROID
		private string BannderAdUnitId { get { return androidBannerAdUnitID; } }
		#elif UNITY_IPHONE
		private string BannderAdUnitId { get { return iosBannerAdUnitID; } }
		#else
		private string BannderAdUnitId { get { return "unexpected_platform"; } }
		#endif

		#if UNITY_ANDROID
		private string InterstitialAdUnitId { get { return androidInterstitialAdUnitID; } }
		#elif UNITY_IPHONE
		private string InterstitialAdUnitId { get { return iosInterstitialAdUnitID; } }
		#else
		private string InterstitialAdUnitId { get { return "unexpected_platform"; } }
		#endif

		#if UNITY_ANDROID
		private string AdMobAppId { get { return admobAndroidAppId; } }
		#elif UNITY_IPHONE
		private string AdMobAppId { get { return admobIOSAppId; } }
		#else
		private string AdMobAppId { get { return "unexpected_platform"; } }
		#endif

		#if UNITY_ANDROID
		private string UnityGameId { get { return unityAndroidGameId; } }
		#elif UNITY_IPHONE
		private string UnityGameId { get { return unityIOSGameId; } }
		#else
		private string UnityGameId { get { return "unexpected_platform"; } }
		#endif

		private bool RemoveAds
		{
			get { return IAPController.IsEnabled && IAPController.Instance.IsProductPurchased(IAPController.Instance.RemoveAdsProductId); }
		}

		#endregion

		#region Unity Methods

		private void Start()
		{
			#if ADMOB
			MobileAds.Initialize(AdMobAppId);

			if (IsInterstitialAdsEnabled && interstitialType == InterstitialType.AdMob)
			{
				// Pre-load the Interstitial Ad
				LoadInterstitialAd();
			}
			#endif

			#if UNITYADS
			if (IsInterstitialAdsEnabled && interstitialType == InterstitialType.UnityAds)
			{
				UnityEngine.Advertisements.Advertisement.Initialize(UnityGameId);
			}
			#endif

			if (IAPController.IsEnabled)
			{
				IAPController.Instance.OnProductPurchased += OnIAPProductPurchased;
			}
		}

		private void OnDestroy()
		{
			#if ADMOB
			DestroyAdMobObjects();
			#endif

			if (IAPController.IsEnabled)
			{
				IAPController.Instance.OnProductPurchased -= OnIAPProductPurchased;
			}
		}

		#if ADMOB
		private void Update()
		{
			HandleQueuedAdMobEvents();
		}
		#endif

		#endregion

		#region Public Methods

		#if ADMOB
		/// <summary>
		/// Shows the banner ad if banner ads are enabled
		/// </summary>
		public void ShowBannerAd(BannerPosition bannerPosition)
		{
			if (IsBannerAdsEnabled)
			{
				// Hide the current showing banner ad
				HideBannerAd();

				// Show the banner ad at the correct position
				switch (bannerPosition)
				{
				case BannerPosition.Top:
					if (topBannerState == BannerState.Idle)
					{
						LoadTopBanner();
					}
					else if (topBannerState == BannerState.Loaded)
					{
						topBanner.Show();
						topBannerState = BannerState.Showing;
					}
					break;
				case BannerPosition.Bottom:
					if (bottomBannerState == BannerState.Idle)
					{
						LoadBottomBanner();
					}
					else if (bottomBannerState == BannerState.Loaded)
					{
						bottomBanner.Show();
						bottomBannerState = BannerState.Showing;
					}
					break;
				}
			}
		}

		/// <summary>
		/// Hides the banner ad is banner ads are enabled
		/// </summary>
		public void HideBannerAd()
		{
			if (IsBannerAdsEnabled)
			{
				if (topBanner != null)
				{
					topBanner.Hide();

					topBannerState = BannerState.Loaded;
				}

				if (bottomBanner != null)
				{
					bottomBanner.Hide();

					bottomBannerState = BannerState.Loaded;
				}
			}
		}
		#endif

		/// <summary>
		/// Shows the interstital ad but only it it's been loaded. Returns true if the ad is shown, false otherwise.
		/// </summary>
		public bool ShowInterstitialAd(System.Action onAdClosed = null)
		{
			bool adShown = false;

			if (IsInterstitialAdsEnabled)
			{
				switch (interstitialType)
				{
				case InterstitialType.UnityAds:
					#if UNITYADS
					{
						#if UNITY_EDITOR
						{
							if (!enableUnityAdsInEditor)
							{
								break;
							}
						}
						#endif
						
						interstitialAdClosedCallback = onAdClosed;

						UnityEngine.Advertisements.ShowOptions adShowOptions = new UnityEngine.Advertisements.ShowOptions();
						
						adShowOptions.resultCallback = OnUnityAdsInterstitalClosed;
						
						UnityEngine.Advertisements.Advertisement.Show(placementId, adShowOptions);

						adShown = true;
					}
					#else
					Debug.LogError("[AdsController] Unity Ads are not enabled in services");
					#endif

					break;
				case InterstitialType.AdMob:
					#if ADMOB
					if (interstitial != null && isInterstitialAdLoaded)
					{
						interstitialAdClosedCallback = onAdClosed;

						interstitial.Show();

						adShown = true;
					}
					#endif

					break;
				}
			}

			return adShown;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called by the IAPController when the player makes a purchase
		/// </summary>
		private void OnIAPProductPurchased(string productId)
		{
			// Check if the player purchased the remove ads IAP
			if (productId == IAPController.Instance.RemoveAdsProductId)
			{
				#if ADMOB
				// Destroy all ad mob objects
				DestroyAdMobObjects();
				#endif

				if (OnAdsRemoved != null)
				{
					OnAdsRemoved();
				}
			}
		}

		#if UNITYADS
		private void OnUnityAdsInterstitalClosed(UnityEngine.Advertisements.ShowResult adShowResult)
		{
			if (interstitialAdClosedCallback != null)
			{
				interstitialAdClosedCallback();
			}
		}
		#endif

		#if ADMOB
		/// <summary>
		/// Loads the interstitial ad
		/// </summary>
		private void LoadInterstitialAd()
		{
			if (interstitial != null)
			{
				interstitial.Destroy();
			}

			// Create an InterstitialAd
			interstitial = new InterstitialAd(InterstitialAdUnitId);

			interstitial.OnAdLoaded += OnInterstitialAdLoaded;
			interstitial.OnAdClosed += OnInterstitialAdClosed;

			isInterstitialAdLoaded = false;

			interstitial.LoadAd(CreateAdRequest());
		}

		/// <summary>
		/// Loads the top banner if it is not already loaded or loading
		/// </summary>
		private void LoadTopBanner()
		{
			if (topBannerState == BannerState.Idle)
			{
				if (topBanner == null)
				{
					// Create the banner view 
					topBanner = new BannerView(BannderAdUnitId, AdSize.SmartBanner, AdPosition.Top);

					// Set the event callbacks for the top banner
					topBanner.OnAdLoaded		+= OnTopBannerLoaded;
					topBanner.OnAdFailedToLoad	+= OnTopBannerFailedToLoad;
				}

				// Set the state to loading
				topBannerState = BannerState.Loading;

				// Load and show the banner
				topBanner.LoadAd(CreateAdRequest());
			}
		}

		/// <summary>
		/// Loads the bottom banner if it is not already loaded or loading
		/// </summary>
		private void LoadBottomBanner()
		{
			if (bottomBannerState == BannerState.Idle)
			{
				if (bottomBanner == null)
				{
					// Create the banner view 
					bottomBanner = new BannerView(BannderAdUnitId, AdSize.SmartBanner, AdPosition.Bottom);

					// Set the event callbacks for the bottom banner
					bottomBanner.OnAdLoaded			+= OnBottomBannerLoaded;
					bottomBanner.OnAdFailedToLoad	+= OnBottomBannerFailedToLoad;
				}

				// Set the state to loading
				bottomBannerState = BannerState.Loading;

				// Load and show the banner
				bottomBanner.LoadAd(CreateAdRequest());
			}
		}

		/// <summary>
		/// Creates a new Ad request to be used by banners and interstitial ads
		/// </summary>
		/// <returns>The ad request.</returns>
		private AdRequest CreateAdRequest()
		{
			return new AdRequest.Builder()
				.AddTestDevice(AdRequest.TestDeviceSimulator)
				//.AddTestDevice("D23859A2702727667C1848E7B932B4C4")
				.Build();
		}

		/// <summary>
		/// Destroys the ad mob objects
		/// </summary>
		private void DestroyAdMobObjects()
		{
			if (topBanner != null)
			{
				topBanner.Hide();
				topBanner.Destroy();
			}

			if (bottomBanner != null)
			{
				bottomBanner.Hide();
				bottomBanner.Destroy();
			}

			if (interstitial != null)
			{
				interstitial.Destroy();
			}
		}

		/// <summary>
		/// Invoked when the top banner has loaded
		/// </summary>
		private void OnTopBannerLoaded(object sender, EventArgs args)
		{
			QueueAdMobEvent(AdMobEvent.OnTopBannerLoaded);
		}

		/// <summary>
		/// Invoked when the top banner fails to load
		/// </summary>
		private void OnTopBannerFailedToLoad(object sender, AdFailedToLoadEventArgs e)
		{
			QueueAdMobEvent(AdMobEvent.OnTopBannerFailedToLoad);
		}

		/// <summary>
		/// Invoked when the bottom banner has loaded
		/// </summary>
		private void OnBottomBannerLoaded(object sender, EventArgs args)
		{
			QueueAdMobEvent(AdMobEvent.OnBottomBannerLoaded);
		}

		/// <summary>
		/// Invoked when the bottom banner fails to load
		/// </summary>
		private void OnBottomBannerFailedToLoad(object sender, AdFailedToLoadEventArgs e)
		{
			QueueAdMobEvent(AdMobEvent.OnBottomBannerFailedToLoad);
		}

		/// <summary>
		/// Invoked when the interstitial ad has loaded
		/// </summary>
		private void OnInterstitialAdLoaded(object sender, EventArgs args)
		{
			QueueAdMobEvent(AdMobEvent.OnInterstitalAdLoaded);
		}

		/// <summary>
		/// Invoked when the interstitial ad is closed
		/// </summary>
		private void OnInterstitialAdClosed(object sender, EventArgs args)
		{
			QueueAdMobEvent(AdMobEvent.OnInterstitalAdClosed);
		}

		/// <summary>
		/// Adds an AdMobEvent to the queue to be run on the main Unity thread
		/// </summary>
		private void QueueAdMobEvent(AdMobEvent adMobEvent)
		{
			lock (adMobEventQueueLock)
			{
				adMobEventQueue.Add(adMobEvent);
			}
		}

		/// <summary>
		/// Handles the all the AdMobEvents in the queue, called on the main thread
		/// </summary>
		private void HandleQueuedAdMobEvents()
		{
			lock (adMobEventQueueLock)
			{
				while (adMobEventQueue.Count > 0)
				{
					// Get the next event in the queue
					AdMobEvent adMobEvent = adMobEventQueue[0];

					// Handle the event
					HandleAdMobEvent(adMobEvent);

					// Remove the event from the queue
					adMobEventQueue.RemoveAt(0);
				}
			}
		}

		/// <summary>
		/// Handles the AdMobEvent, called on the main thread
		/// </summary>
		private void HandleAdMobEvent(AdMobEvent adMobEvent)
		{
			switch (adMobEvent)
			{
				case AdMobEvent.OnTopBannerLoaded:
					// Failed to load top banner, set the state to Idle so we can try and load it again next time
					topBannerState = BannerState.Idle;
					break;
				case AdMobEvent.OnTopBannerFailedToLoad:
					// Failed to load top banner, set the state to Idle so we can try and load it again next time
					topBannerState = BannerState.Idle;
					break;
				case AdMobEvent.OnBottomBannerLoaded:
					// Bottom banner succeffully loaded
					bottomBannerState = BannerState.Loaded;
					break;
				case AdMobEvent.OnBottomBannerFailedToLoad:
					// Failed to load bottom banner, set the state to Idle so we can try and load it again next time
					bottomBannerState = BannerState.Idle;
					break;
				case AdMobEvent.OnInterstitalAdLoaded:
					// Interstital ad has been loaded and is ready to be shown
					isInterstitialAdLoaded = true;
					break;
				case AdMobEvent.OnInterstitalAdClosed:
					// Load another interstital ad so its ready next time we have to show an ad
					LoadInterstitialAd();

					// Call the callback that was passed in the show method
					if (interstitialAdClosedCallback != null)
					{
						interstitialAdClosedCallback();
					}
					break;
			}
		}

		#endif

		#endregion
	}
}