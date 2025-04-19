using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	public class ColorListItem : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private Image		colorImage			= null;
		[SerializeField] private Text		colorNumberText		= null;
		[SerializeField] private Image		completedCheckmark	= null;
		[SerializeField] private GameObject	selectedIndicator	= null;
		[SerializeField] private Color		lightTextColor		= Color.white;
		[SerializeField] private Color		darkTextColor		= Color.black;
		[SerializeField] private Color		selectedTextColor	= Color.blue;

		#endregion

		#region Member Variables

		private int		colorIndex;
		private Color	itemColor;

		#endregion

		#region Properties

		public System.Action<int> OnColorClicked { get; set; }

		#endregion

		#region Public Methods

		public void Setup(Color color, int colorIndex)
		{
			this.colorIndex = colorIndex;
			this.itemColor	= color;

			colorImage.color			= color;
			colorNumberText.text		= (colorIndex + 1).ToString();

			SetTextColor();
		}

		public void SetSelected(bool isSelected)
		{
			selectedIndicator.SetActive(isSelected);

			if (isSelected)
			{
				colorNumberText.color = selectedTextColor;
			}
			else
			{
				SetTextColor();
			}
		}

		public void SetCompleted(bool isCompleted)
		{
			completedCheckmark.gameObject.SetActive(isCompleted);
			colorNumberText.gameObject.SetActive(!isCompleted);
		}

		public void OnClick()
		{
			if (OnColorClicked != null)
			{
				OnColorClicked(colorIndex);
			}
		}

		#endregion

		#region Private Methods

		private void SetTextColor()
		{
			bool useDarkColor = (TextureUtilities.GetColorDiff(lightTextColor, itemColor) <= 6f);

			colorNumberText.color		= useDarkColor ? darkTextColor : lightTextColor;
			completedCheckmark.color	= useDarkColor ? darkTextColor : lightTextColor;
		}

		#endregion
	}
}
