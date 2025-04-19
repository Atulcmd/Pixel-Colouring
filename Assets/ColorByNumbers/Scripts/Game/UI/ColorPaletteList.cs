using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	public class ColorPaletteList : MonoBehaviour
	{
		#region Inspector Variables

		[Tooltip("The ColorListItem prefab to instantiate copies of and place in the list.")]
		[SerializeField] private ColorListItem colorListItemPrefab = null;

		[Tooltip("The Transform where the list items are placed under.")]
		[SerializeField] private RectTransform colorListItemContainer = null;

		#endregion

		#region Member Variables

		private PictureInformation	pictureInfo;
		private ObjectPool			colorListItemPool;
		private List<ColorListItem>	colorListItems;

		#endregion

		#region Properties

		public System.Action<int> OnColorSelected { get; set; }

		private RectTransform ListRectT { get { return transform as RectTransform; } }

		#endregion

		#region Unity Methods

		private void Start()
		{
			colorListItemPool	= new ObjectPool(colorListItemPrefab.gameObject, 5, ObjectPool.CreatePoolContainer(transform));
			colorListItems		= new List<ColorListItem>();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Setup the color list with all the colors in the picture
		/// </summary>
		public void SetupPaletteList(PictureInformation pictureInfo)
		{
			this.pictureInfo = pictureInfo;

			Clear();

			// Add all the color items to the container
			for (int i = 0; i < pictureInfo.Colors.Count; i++)
			{
				ColorListItem colorListItem	= colorListItemPool.GetObject<ColorListItem>(colorListItemContainer.transform);

				colorListItem.OnColorClicked = OnItemClicked;

				colorListItem.Setup(pictureInfo.Colors[i], i);

				colorListItems.Add(colorListItem);
			}

			SetColorSelected(0);

			UpdateCompleted();

			colorListItemContainer.anchoredPosition = Vector2.zero;
		}

		/// <summary>
		/// Sets the color at the given index as the selected color
		/// </summary>
		public void SetColorSelected(int index)
		{
			// Loop through color list items, setting the selected state
			for (int i = 0; i < colorListItems.Count; i++)
			{
				colorListItems[i].SetSelected(i == index);
			}
		}

		/// <summary>
		/// Re-sets the completed flag on all items
		/// </summary>
		public void UpdateCompleted()
		{
			// Loop through color list items, setting the selected state
			for (int i = 0; i < colorListItems.Count; i++)
			{
				colorListItems[i].SetCompleted(pictureInfo.IsColorComplete(i));
			}
		}

		/// <summary>
		/// Clears the list of all items
		/// </summary>
		public void Clear()
		{
			colorListItems.Clear();

			colorListItemPool.ReturnAllObjectsToPool();
		}

		#endregion

		#region Private Methods

		private void OnItemClicked(int index)
		{
			SetColorSelected(index);

			if (OnColorSelected != null)
			{
				OnColorSelected(index);
			}
		}

		#endregion
	}
}
