using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	public class PowerUpButton : MonoBehaviour
	{
		#region Inspector Variables

		[Tooltip("The Image component which will have it's color changed when selected.")]
		[SerializeField] private Image selectedImage = null;

		[Tooltip("The Text component where the cost of the power up will be set.")]
		[SerializeField] private Text costText = null;

		[Tooltip("The color of the selectedImage when the power up is not selected.")]
		[SerializeField] private Color normalColor = Color.white;

		[Tooltip("The color of the selectedImage when the power up is selected.")]
		[SerializeField] private Color selectedColor = Color.white;


		#endregion

		#region Public Methods

		public void SetCost(int cost)
		{
			costText.text = cost.ToString();
		}

		public void SetSelected(bool isSelected)
		{
			selectedImage.color = isSelected ? selectedColor : normalColor;
		}

		#endregion
	}
}
