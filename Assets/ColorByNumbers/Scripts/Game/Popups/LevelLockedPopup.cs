using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	public class LevelLockedPopup : BizzyBeeGames.ColorByNumbers.Popup
	{
		#region Inspector Variables

		[SerializeField] private RawImage	grayscaleImage		= null;
		[SerializeField] private Text		unlockAmountText	= null;

		#endregion

		#region Public Methods

		public override void OnShowing(object[] inData)
		{
			Texture2D	grayscaleTexture	= (Texture2D)inData[0];
			int			unlockAmount		= (int)inData[1];

			grayscaleImage.texture	= grayscaleTexture;
			unlockAmountText.text	= unlockAmount.ToString();

			TextureUtilities.ScaleForTexture(grayscaleImage.texture as Texture2D, grayscaleImage.transform);
		}

		#endregion
	}
}
