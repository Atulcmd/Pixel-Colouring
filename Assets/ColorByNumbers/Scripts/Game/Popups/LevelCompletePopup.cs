using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	public class LevelCompletePopup : Popup
	{
		#region Inspector Variables

		[Space]
		[SerializeField] private RawImage		completedImage			= null;
		[SerializeField] private GameObject		awardAmountContainer	= null;
		[SerializeField] private Text			awardAmountText			= null;
		[Space]
		[SerializeField] private CanvasGroup	notificationContainer	= null;
		[SerializeField] private Text			notificationText		= null;

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			Texture2D	completedTexture	= inData[0] as Texture2D;
			int			coinsAwarded		= (int)inData[1];

			awardAmountContainer.SetActive(coinsAwarded > 0);
			awardAmountText.text = string.Format("+{0}", coinsAwarded);

			completedImage.texture = completedTexture;

			TextureUtilities.ScaleForTexture(completedImage.texture as Texture2D, completedImage.transform);
		}

		public void OnContinueClicked()
		{
			// Force the my works screen to appear by showing it on the UIController with animate set to false
			UIController.Instance.ShowMenuScreenById(UIController.MyWorksMenuScreenId, false);

			// Transition back to the main screen
			ScreenTransitionController.Instance.Show(ScreenTransitionController.MainScreenId, true);

			// Hide the popup
			Hide(false);
		}

		public void OnTwitterButtonClicked()
		{
			bool opened = SharingController.Instance.ShareToTwitter(completedImage.texture as Texture2D);

			if (!opened)
			{
				ShowNotification("Twitter is not installed");
			}
		}

		public void OnInstagramButtonClicked()
		{
			bool opened = SharingController.Instance.ShareToInstagram(completedImage.texture as Texture2D);

			if (!opened)
			{
				ShowNotification("Instagram is not installed");
			}
		}

		public void OnShareOtherButtonClicked()
		{
			SharingController.Instance.ShareToOther(completedImage.texture as Texture2D);
		}

		public void OnSaveToDevice()
		{
			SharingController.Instance.SaveImageToPhotos(completedImage.texture as Texture2D, OnSaveToPhotosResponse);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Shows the notification
		/// </summary>
		private void ShowNotification(string message)
		{
			// Set the text for the notification
			notificationText.text = message;

			// Fade in the notification
			Tween.CanvasGroupAlpha(notificationContainer, Tween.TweenStyle.EaseOut, notificationContainer.alpha, 1f, 350f);

			// Wait a couple seconds then hide the notification
			StartCoroutine(WaitThenHideNotification());
		}

		/// <summary>
		/// Hides the notification.
		/// </summary>
		private void HideNotification()
		{
			Tween.CanvasGroupAlpha(notificationContainer, Tween.TweenStyle.EaseOut, notificationContainer.alpha, 0f, 350f);
		}

		/// <summary>
		/// Waits 3 seconds then hides notification.
		/// </summary>
		private IEnumerator WaitThenHideNotification()
		{
			yield return new WaitForSeconds(3);

			HideNotification();
		}

		/// <summary>
		/// Invoked when the ShareController has either save the image to photos or failed to due to permissions not being granted
		/// </summary>
		private void OnSaveToPhotosResponse(bool success)
		{
			if (success)
			{
				ShowNotification("Picture save to device!");
			}
			else
			{
				#if UNITY_IOS
				PopupController.Instance.Show("permissions_popup", new object[] { "Photos" });
				#elif UNITY_ANDROID
				PopupController.Instance.Show("permissions_popup", new object[] { "Storage" });
				#endif
			}
		}

		#endregion
	}
}
