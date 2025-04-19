using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	public class ContinueLevelPopup : Popup
	{
		#region Inspector Variables

		[SerializeField] private RawImage grayscaleImage = null;

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			Texture2D grayscaleTexture = (Texture2D)inData[0];

			grayscaleImage.texture = grayscaleTexture;

			TextureUtilities.ScaleForTexture(grayscaleImage.texture as Texture2D, grayscaleImage.transform);
		}

		public void OnContinueClicked()
		{
			Close("continue");
		}

		public void OnRestartClicked()
		{
			Close("restart");
		}

		public void OnDeleteClicked()
		{
			Close("delete");
		}

		#endregion

		#region Private Methods

		private void Close(string action)
		{
			Hide(false, new object[] { action });
		}

		#endregion
	}
}
