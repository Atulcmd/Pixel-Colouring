using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	public class PictureListItem : MonoBehaviour
	{
		#region Inspector Variables

		[Tooltip("The alpha to apply to the grayscale, makes the grayscale lighter.")]
		[SerializeField] private float textureAlpha = 0.5f;

		[Tooltip("The RawImage component that the grayscale texture will be set to.")]
		[SerializeField] private RawImage textureImage = null;

		[Tooltip("The GameObject that will be set to active if the level is locked.")]
		[SerializeField] private GameObject lockedIndicator = null;

		[Tooltip("The amount of coins required to unlock the level is set on this Text component.")]
		[SerializeField] private Text unlockAmountText = null;

		[Tooltip("This GameObject will be set to active if the level has progress and can be continued from a save.")]
		[SerializeField] private GameObject continueIcon = null;

		[Tooltip("This GameObject will be set to active if the level has been completed and is on the my works screen.")]
		[SerializeField] private GameObject completedIcon = null;

		#endregion

		#region Member Variables

		private PictureInformation	pictureInfo;
		private bool				isCompletedItem;

		#endregion

		#region Properties

		public System.Action<PictureInformation> OnItemClicked { get; set; }
		public System.Action<PictureInformation> OnItemDeleted { get; set; }

		public RectTransform RectT { get { return transform as RectTransform; } }

		#endregion

		#region Public Methods

		/// <summary>
		/// Setup this PictureListItem instance using the given PictureInformation
		/// </summary>
		public void Setup(PictureInformation pictureInfo, bool completedItem)
		{
			this.pictureInfo		= pictureInfo;
			this.isCompletedItem	= completedItem;

			SetupUI();

			RefreshTexture();

			completedIcon.SetActive(completedItem);
			continueIcon.SetActive(!completedItem && pictureInfo.HasProgress);
		}

		/// <summary>
		/// Gets the grayscale texture from the TextureController
		/// </summary>
		public void RefreshTexture()
		{
			if (isCompletedItem)
			{
				textureImage.texture = TextureController.Instance.LoadCompletedTexture(pictureInfo);
			}
			else
			{
				textureImage.texture = TextureController.Instance.LoadGrayscale(pictureInfo, textureAlpha);
			}

			TextureUtilities.ScaleForTexture(textureImage.texture as Texture2D, textureImage.transform);
		}

		public void OnClicked()
		{
			if (isCompletedItem)
			{
				// If the item is a completed item then show the completed level popup
				PopupController.Instance.Show("completed_level_popup", new object[] { textureImage.texture }, OnItemPopupClosed);
			}
			else if (pictureInfo.IsLocked)
			{
				// If the level is lock then display the popup to allow the user to unlock it
				PopupController.Instance.Show("level_locked_popup", new object[] { textureImage.texture, pictureInfo.UnlockAmount }, OnLockedItemPopupClosed);
			}
			else if (pictureInfo.HasProgress)
			{
				// If the level is not a completed list item and has progress then show the continue level popup
				PopupController.Instance.Show("continue_level_popup", new object[] { textureImage.texture }, OnItemPopupClosed);
			}
			else
			{
				OnItemClicked(pictureInfo);
			}
		}

		#endregion

		#region Private Methods

		private void SetupUI()
		{
			lockedIndicator.SetActive(pictureInfo.IsLocked);

			unlockAmountText.text = pictureInfo.UnlockAmount.ToString();
		}

		private void OnItemPopupClosed(bool cancelled, object[] outData)
		{
			if (!cancelled)
			{
				string action = outData[0] as string;

				switch (action)
				{
					case "continue":
						OnItemClicked(pictureInfo);
						break;
					case "restart":
						pictureInfo.ClearProgress();
						OnItemClicked(pictureInfo);
						break;
					case "delete":
						OnItemDeleted(pictureInfo);
						break;
				}
			}
		}

		private void OnLockedItemPopupClosed(bool cancelled, object[] outData)
		{
			if (!cancelled)
			{
				if (GameController.Instance.CurrencyAmount >= pictureInfo.UnlockAmount)
				{
					// Unlock the level
					GameController.Instance.UnlockLevel(pictureInfo);

					SetupUI();

					// Call the OnItemClicked callback to start the level
					OnItemClicked(pictureInfo);
				}
				else
				{
					PopupController.Instance.Show("not_enough_coins");
				}
			}
		}

		#endregion
	}
}
