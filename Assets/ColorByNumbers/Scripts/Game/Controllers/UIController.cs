using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	public class UIController : SingletonComponent<UIController>
	{
		#region Classes

		[System.Serializable]
		private class MenuScreen
		{
			[Tooltip("The screens id, should be unique between all other screens.")]
			public string id = "";

			[Tooltip("The root RectTransform for the screen.")]
			public RectTransform screenRect = null;

			[Tooltip("The MenuNavigationButton component that controls showing this screen when clicked.")]
			public MenuNavigationButton	menuNavButton = null;
		}

		#endregion

		#region Inspector Variables

		[Tooltip("The number of levels that must be started before an interstital ad is shown (If ads are enabled).")]
		[SerializeField] private int numLevelsTillAd = 3;

		[Tooltip("The list of menu screens, these are the screens that are shown when clicking one of the navigation buttons at the bottom of the screen.")]
		[SerializeField] private List<MenuScreen> menuScreens = null;

		[Space]

		[Tooltip("The PictureListItem prefab that will be instantiated and placed in the library and my works lists.")]
		[SerializeField] private PictureListItem pictureListItemPrefab = null;

		[Tooltip("The GridLayoutGroup component that the list items for the library will be placed under.")]
		[SerializeField] private GridLayoutGroup libraryListContainer = null;

		[Tooltip("The GridLayoutGroup component that the list items for the my works will be placed under.")]
		[SerializeField] private GridLayoutGroup myWorksListContainer = null;

		[Tooltip("The placeholder GameObject that is set to active when there are not items in the my works list.")]
		[SerializeField] private GameObject myWorksNoLevelsContainer = null;

		[Space]

		[Tooltip("If true then the game will setup to display and use the individual categories in the game. If false then all levels will be displayed in the libraryListContainer.")]
		[SerializeField] private bool useCategories;

		[Tooltip("The CategoryListItem prefab that will be instantiated and placed in the categoryListItemContainer for each CategoryInfo defined in the GameController.")]
		[SerializeField] private CategoryListItem categoryListItemPrefab;

		[Tooltip("The Transform all the CategoryItemUIs will be placed in.")]
		[SerializeField] private Transform categoryListItemContainer;

		#endregion

		#region Member Variables

		public const string CategoriesMenuScreenId	= "categories";
		public const string LibraryMenuScreenId		= "library";
		public const string MyWorksMenuScreenId		= "my_works";
		public const string CreateMenuScreenId		= "create";

		private bool						isInitialized;
		private MenuScreen					activeMenuScreen;
		private ObjectPool					listItemPlaceholdPool;
		private ObjectPool					pictureListItemPool;
		private ObjectPool					categoryListItemPool;
		private List<PictureInformation>	libraryPictureInfos;
		private List<PictureInformation>	progressedPictureInfos;
		private List<PictureInformation>	completedPictureInfos;
		private List<PictureListItem>		libraryListItems;
		private List<PictureListItem>		myWorksListItems;

		private int				numLevelsPlayed;
		private CategoryInfo	activeCategoryInfo;

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			if (useCategories && (categoryListItemPrefab == null || categoryListItemContainer == null))
			{
				Debug.LogError("[UIController] Categories require a \"Category List Item Prefab\" and \"Category List Item Container\" to be set.");
			}

			// Create a GameObject to hold all the list items that are pooled
			listItemPlaceholdPool	= new ObjectPool(new GameObject("list_item", typeof(RectTransform)), 1, ObjectPool.CreatePoolContainer(transform));
			pictureListItemPool		= new ObjectPool(pictureListItemPrefab.gameObject, 1, ObjectPool.CreatePoolContainer(transform));

			if (useCategories)
			{
				categoryListItemPool = new ObjectPool(categoryListItemPrefab.gameObject, 1, ObjectPool.CreatePoolContainer(transform));
			}

			libraryPictureInfos		= new List<PictureInformation>();
			progressedPictureInfos	= new List<PictureInformation>();
			completedPictureInfos	= new List<PictureInformation>();

			libraryListItems	= new List<PictureListItem>();
			myWorksListItems	= new List<PictureListItem>();

			// Set all the screen off screen to hide them
			for (int i = 0; i < menuScreens.Count; i++)
			{
				ScreenTransitionController.Instance.SetOffScreen(menuScreens[i].screenRect);
			}

			// Set the library screen as the main screen
			ShowMenuScreenById(useCategories ? CategoriesMenuScreenId : LibraryMenuScreenId);
		}

		private void Start()
		{
			UpdateMyWorksPictureInfos();

			if (useCategories)
			{
				SetupCategoryItemList();
			}

			StartCoroutine(Initialize());

			ScreenTransitionController.Instance.OnShowingScreen += OnScreenShowing;
		}

		#endregion

		#region Public Methods

		public void OnLibraryScolled()
		{
			if (isInitialized)
			{
				UpdateLibraryItemList();
			}
		}

		public void OnMyWorksScrolled()
		{
			if (isInitialized)
			{
				UpdateMyWorksItemList();
			}
		}

		/// <summary>
		/// Shows the menu screen with the given screen id
		/// </summary>
		public void ShowMenuScreenById(string screenId)
		{
			ShowMenuScreenById(screenId, true);
		}

		/// <summary>
		/// Shows the menu screen with the given screen id
		/// </summary>
		public void ShowMenuScreenById(string screenId, bool animate)
		{
			for (int i = 0; i < menuScreens.Count; i++)
			{
				if (screenId == menuScreens[i].id)
				{
					ShowMenuScreen(menuScreens[i], animate);

					return;
				}
			}

			Debug.LogError("[UIController] There is no menu screen with the id: " + screenId);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Sets up list of items for the category screen
		/// </summary>
		private void SetupCategoryItemList()
		{
			for (int i = 0; i < GameController.Instance.CategoryInfos.Count; i++)
			{
				CategoryInfo		categoryInfo		= GameController.Instance.CategoryInfos[i];
				CategoryListItem	categoryListItem	= categoryListItemPool.GetObject<CategoryListItem>(categoryListItemContainer);

				categoryListItem.Setup(categoryInfo);

				categoryListItem.OnCategoryListItemClicked = OnCategoryListItemClicked;
			}
		}

		/// <summary>
		/// Sets up list of items for the library
		/// </summary>
		private void SetupLibraryItemList()
		{
			// Check if there is a selected category
			if (useCategories && activeCategoryInfo == null)
			{
				return;
			}

			// Update the list of PictureInformations we will be displaying the the library
			libraryPictureInfos = useCategories ? activeCategoryInfo.PictureInfos : GameController.Instance.PictureInfos;

			// Refresh the number of item in the list
			SetupItemList(libraryListContainer, libraryListItems, libraryPictureInfos.Count);

			// Need to wait for the unity to size the grid layout groups
			StartCoroutine(WaitThemUpdateLibraryItemList());
		}

		/// <summary>
		/// Sets up the list of items that appear under the "my works" heading
		/// </summary>
		private void SetupMyWorksItemList()
		{
			// Refresh the number of item in the list
			SetupItemList(myWorksListContainer, myWorksListItems, progressedPictureInfos.Count + completedPictureInfos.Count);

			// Need to wait for the unity to size the grid layout groups
			StartCoroutine(WaitThemUpdateMyWorksItemList());
		}

		/// <summary>
		/// Waits till the end of the frame before calling UpdateLibraryItemList
		/// </summary>
		private IEnumerator WaitThemUpdateLibraryItemList()
		{
			yield return new WaitForEndOfFrame();

			UpdateLibraryItemList();
		}

		/// <summary>
		/// Updates the list of PictureListItems for the library items
		/// </summary>
		private void UpdateLibraryItemList(bool forceUpdateAll = false)
		{
			// Return items that are no longer displayed to the pool
			ReturnOffScreenItemsToPool(libraryListContainer, libraryListItems);

			int startIndex, endIndex;

			// Get the starting and ending index for the visible list item
			GetStartEndIndices(libraryListContainer, out startIndex, out endIndex);

			// For each index, check if we need to instatiate a new instance it PictureListItem and place it on the placeholder
			for (int i = startIndex; i < endIndex && i < libraryPictureInfos.Count; i++)
			{
				Transform		placeholder		= libraryListContainer.transform.GetChild(i);
				PictureListItem	pictureListItem	= null;

				if (placeholder.childCount == 0)
				{
					// If the placeholder has no children then we need to get a new PictureListItem from the pool
					pictureListItem = pictureListItemPool.GetObject<PictureListItem>(placeholder);
				}
				else if (forceUpdateAll)
				{
					// If there is already a PictureListItem added and forceUpdateAll is true then get that PictureListItem
					pictureListItem = placeholder.GetChild(0).GetComponent<PictureListItem>();
				}

				if (pictureListItem != null)
				{
					// Setup the PictureListItem using the picture info
					PictureInformation pictureInfo = libraryPictureInfos[i];

					pictureListItem.Setup(pictureInfo, pictureInfo.Completed && !pictureInfo.HasProgress);

					pictureListItem.OnItemClicked = OnPictureItemClicked;
					pictureListItem.OnItemDeleted = OnPictureItemDeleted;

					libraryListItems.Add(pictureListItem);
				}
			}
		}

		/// <summary>
		/// Waits till the end of the frame before calling UpdateMyWorksItemList
		/// </summary>
		private IEnumerator WaitThemUpdateMyWorksItemList()
		{
			yield return new WaitForEndOfFrame();

			UpdateMyWorksItemList();
		}

		/// <summary>
		/// Updates the list of PictureListItems for the my works items
		/// </summary>
		private void UpdateMyWorksItemList()
		{
			// Return items that are no longer displayed to the pool
			ReturnOffScreenItemsToPool(myWorksListContainer, myWorksListItems);

			int startIndex, endIndex;

			// Get the starting and ending index for the visible list item
			GetStartEndIndices(myWorksListContainer, out startIndex, out endIndex);

			int itemCount = progressedPictureInfos.Count + completedPictureInfos.Count;

			// For each index, check if we need to instatiate a new instance it PictureListItem and place it on the placeholder
			for (int i = startIndex; i < endIndex && i < itemCount; i++)
			{
				Transform		placeholder		= myWorksListContainer.transform.GetChild(i);
				PictureListItem	pictureListItem	= null;

				// If the placeholder has no children then we need to add a PcitureListItem to it
				if (placeholder.childCount == 0)
				{
					PictureInformation	pictureInfo				= null;
					bool				isCompletedPictureInfo	= false;

					// Check if the i index is less than the amount of completed picture infos, if so we need to display one of those
					if (i < completedPictureInfos.Count)
					{
						pictureInfo				= completedPictureInfos[i];
						isCompletedPictureInfo	= true;
					}
					// Else display a normal pricture info
					else
					{
						pictureInfo = progressedPictureInfos[i - completedPictureInfos.Count];
					}

					// Get a PictureListItem from the pool and set it's parent to the placeholder
					pictureListItem					= pictureListItemPool.GetObject<PictureListItem>(placeholder);
					pictureListItem.OnItemClicked	= OnPictureItemClicked;
					pictureListItem.OnItemDeleted	= OnPictureItemDeleted;

					// Setup the PictureListItem using the picture info
					pictureListItem.Setup(pictureInfo, isCompletedPictureInfo);

					myWorksListItems.Add(pictureListItem);
				}
			}
		}

		/// <summary>
		/// Waits till the end of the frame then gets the cell size of the contains the setting them up.
		/// </summary>
		private IEnumerator Initialize()
		{
			yield return new WaitForEndOfFrame();

			// Sets the cell size of the containers
			SetCellSize(libraryListContainer);
			SetCellSize(myWorksListContainer);

			// Now setup the lists with their items
			SetupLibraryItemList();
			SetupMyWorksItemList();

			isInitialized = true;
		}

		private void SetCellSize(GridLayoutGroup listContainer)
		{
			// Set teh cell size based on the containers width and the number of columns in the container
			RectTransform	containerRectT	= listContainer.transform as RectTransform;
			float			cellSize		= (containerRectT.rect.width - listContainer.padding.left - listContainer.padding.right - listContainer.spacing.x) / listContainer.constraintCount;

			listContainer.cellSize = new Vector2(cellSize, cellSize);
		}

		/// <summary>
		/// Refreshed the children in listContainer to contain the given amount of item placeholders
		/// </summary>
		private void SetupItemList(GridLayoutGroup listContainer, List<PictureListItem> pictureListItems, int itemCount)
		{
			// Return any list items are displayed
			for (int i = 0; i < pictureListItems.Count; i++)
			{
				pictureListItemPool.ReturnObjectToPool(pictureListItems[i].gameObject);
			}

			pictureListItems.Clear();

			// Add placeholders if the list needs more
			while (listContainer.transform.childCount < itemCount)
			{
				listItemPlaceholdPool.GetObject(listContainer.transform);
			}

			// Remove placeholders if the list has to many
			while (listContainer.transform.childCount > itemCount)
			{
				listItemPlaceholdPool.ReturnObjectToPool(listContainer.transform.GetChild(0).gameObject);
			}

			// Position the list back at the top
			(listContainer.transform as RectTransform).anchoredPosition = Vector2.zero;
		}

		/// <summary>
		/// Returns any picture list items that are out of the bounds of the given listContainer back to their object pool
		/// </summary>
		private void ReturnOffScreenItemsToPool(GridLayoutGroup listContainer, List<PictureListItem> pictureListItems)
		{
			RectTransform	listRectT	= listContainer.transform as RectTransform;
			float			listTop		= listRectT.anchoredPosition.y;
			float			listBottom	= listTop + (listRectT.parent as RectTransform).rect.height;

			// Return all items that are no longer showing back to the pool
			for (int i = pictureListItems.Count - 1; i >= 0; i--)
			{
				PictureListItem	listItem	= pictureListItems[i];
				float			itemTop		= -listItem.RectT.anchoredPosition.y;
				float			itemBottom	= itemTop + listContainer.cellSize.y;

				if (itemTop > listBottom || itemBottom < listTop)
				{
					pictureListItemPool.ReturnObjectToPool(listItem.gameObject);
					pictureListItems.RemoveAt(i);
				}
			}
		}

		/// <summary>
		/// Gets the start index and end index for the list items that are visible
		/// </summary>
		private void GetStartEndIndices(GridLayoutGroup listContainer, out int startIndex, out int endIndex)
		{
			RectTransform	listRectT		= listContainer.transform as RectTransform;
			float			listTop			= listRectT.anchoredPosition.y;
			float			listBottom		= listTop + (listRectT.parent as RectTransform).rect.height;
			float 			spacing			= listContainer.spacing.y;
			float 			topPadding		= listContainer.padding.top;
			float			listItemSize	= listContainer.cellSize.y;

			int startRow	= Mathf.Max(0, Mathf.FloorToInt((listTop + spacing - topPadding) / (listItemSize + spacing)));
			int endRow		= Mathf.Max(0, Mathf.FloorToInt((listBottom - topPadding) / (listItemSize + spacing)));

			startIndex	= startRow * listContainer.constraintCount;
			endIndex	= (endRow + 1) * listContainer.constraintCount;
		}

		/// <summary>
		/// Updates the list of PictureInformations that will appear in the "my works" section
		/// </summary>
		private void UpdateMyWorksPictureInfos()
		{
			progressedPictureInfos.Clear();
			completedPictureInfos.Clear();

			// Add all the picture infos that have been played
			for (int i = 0; i < GameController.Instance.PlayedPictureInfos.Count; i++)
			{
				PictureInformation pictureInfo = GameController.Instance.PlayedPictureInfos[i];

				if (pictureInfo.Completed)
				{
					completedPictureInfos.Add(pictureInfo);
				}

				// If the picture has any progress then add it to the list (Even if if has been completed)
				if (pictureInfo.HasProgress)
				{
					progressedPictureInfos.Add(pictureInfo);
				}
			}

			// Show the "There are no levels" text on the My WORKS screen if there are no levels that have progress
			myWorksNoLevelsContainer.SetActive(progressedPictureInfos.Count + completedPictureInfos.Count == 0);
		}

		/// <summary>
		/// Called when a CategoryListItem is clicked
		/// </summary>
		public void OnCategoryListItemClicked(CategoryInfo categoryInfo)
		{
			// Set the category that we want to display in the library
			activeCategoryInfo = categoryInfo;

			// Setup the library with the new items
			SetupLibraryItemList();

			// Show the library screen now
			ShowMenuScreenById(LibraryMenuScreenId);
		}

		/// <summary>
		/// Called when a PictureListItem is clicked
		/// </summary>
		private void OnPictureItemClicked(PictureInformation pictureInfo)
		{
			numLevelsPlayed++;

			bool adShown = false;

			if (AdsController.Exists() && numLevelsPlayed >= numLevelsTillAd)
			{
				numLevelsPlayed = 0;

				adShown = AdsController.Instance.ShowInterstitialAd(() => { StartLevel(pictureInfo); });
			}

			// If no ad was shown then start the level right away
			if (!adShown)
			{
				StartLevel(pictureInfo);
			}
		}

		/// <summary>
		/// Called when a PictureListItem is deleted
		/// </summary>
		private void OnPictureItemDeleted(PictureInformation pictureInfo)
		{
			GameController.Instance.DeleteLevelProgress(pictureInfo);

			UpdateLibraryItemList(true);
			UpdateMyWorksPictureInfos();
			SetupMyWorksItemList();
		}

		/// <summary>
		/// Starts the level
		/// </summary>
		private void StartLevel(PictureInformation pictureInfo)
		{
			GameController.Instance.StartLevel(pictureInfo);

			ScreenTransitionController.Instance.Show(ScreenTransitionController.GameScreenId);
		}


		/// <summary>
		/// Calls refresh on all the given PictureListItems
		/// </summary>
		private void RefreshPictureListItems(List<PictureListItem> pictureListItems)
		{
			for (int i = 0; i < pictureListItems.Count; i++)
			{
				pictureListItems[i].RefreshTexture();
			}
		}

		/// <summary>
		/// Transitions from the active menu screen to the given menu screen
		/// </summary>
		private void ShowMenuScreen(MenuScreen menuScreen, bool animate = true)
		{
			if (activeMenuScreen == menuScreen)
			{
				return;
			}

			// If the create screen is showing check if the device has permission to use the camera
			if (menuScreen.id == CreateMenuScreenId && !NativePlugin.HasCameraPermission())
			{
				#if UNITY_IOS
				// For iOS, the permission dialog is only shown when WebCamTexture.Play is called but we dont want that.
				// Instead there is a method in the iOSPlugin that will request the permission, it is invoked with the following
				// method in NativePlugin.cs. The response will be handled in OnCamerPermissionResponse
				NativePlugin.RequestCameraPermission(gameObject.name, "OnCamerPermissionResponse");
				#else
				// For Android, the permission is requested at the start of the application by Unity so if we don;t have permission
				// now then the user selected deny so show the permission explination popup
				PopupController.Instance.Show("permissions_popup", new object[] { "Camera" });
				#endif

				return;
			}

			SetSelectedMenuButton(menuScreen.menuNavButton);

			OnScreenShowing(menuScreen.id);

			bool back = (activeMenuScreen != null) && menuScreens.IndexOf(activeMenuScreen) > menuScreens.IndexOf(menuScreen);

			ScreenTransitionController.Instance.TransitionScreens(activeMenuScreen != null ? activeMenuScreen.screenRect : null, menuScreen.screenRect, back, animate);

			activeMenuScreen = menuScreen;
		}

		/// <summary>
		/// Sets the selected menu navigation button
		/// </summary>
		private void SetSelectedMenuButton(MenuNavigationButton selectedMenuNavButton)
		{
			for (int i = 0; i < menuScreens.Count; i++)
			{
				MenuScreen menuScreen = menuScreens[i];

				menuScreen.menuNavButton.SetSelected(menuScreen.menuNavButton == selectedMenuNavButton);
			}
		}

		/// <summary>
		/// Invoked when the screen changes
		/// </summary>
		private void OnScreenShowing(string screenId)
		{
			switch (screenId)
			{
			case ScreenTransitionController.MainScreenId:
				UpdateLibraryItemList(true);
				UpdateMyWorksPictureInfos();
				SetupMyWorksItemList();

				if (activeMenuScreen != null && activeMenuScreen.id == CreateMenuScreenId)
				{
					ShowMenuScreenById(MyWorksMenuScreenId, false);
				}
				break;
			case LibraryMenuScreenId:
				RefreshPictureListItems(libraryListItems);
				break;
			case MyWorksMenuScreenId:
				RefreshPictureListItems(myWorksListItems);
				break;
			}
		}

		/// <summary>
		/// Invoked by the native plugin when the user has selected an option on the permission dialog
		/// </summary>
		private void OnCamerPermissionResponse(string message)
		{
			if (message == "true")
			{
				// Permission has been granted by the user, calling ShowMenuScreenById will not allow access to the CREATE screen
				ShowMenuScreenById(CreateMenuScreenId);
			}
			else
			{
				// Else permission was denied, show the permission dialog to notify the user they cannot use the CREATE feature
				PopupController.Instance.Show("permissions_popup", new object[] { "Camera" });
			}
		}

		#endregion
	}
}
