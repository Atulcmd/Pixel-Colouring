using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	[RequireComponent(typeof(Button))]
	public class MenuNavigationButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string	menuScreenId	= "";
		[SerializeField] private Image	buttonIcon		= null;
		[SerializeField] private Text	buttonText		= null;
		[SerializeField] private Color	normalColor		= Color.white;
		[SerializeField] private Color	selectedColor	= Color.white;

		#endregion

		#region Unity Methods

		private void Start()
		{
			gameObject.GetComponent<Button>().onClick.AddListener(() => { UIController.Instance.ShowMenuScreenById(menuScreenId); } );
		}

		public void SetSelected(bool isSelected)
		{
			buttonIcon.color = isSelected ? selectedColor : normalColor;
			buttonText.color = isSelected ? selectedColor : normalColor;
		}

		#endregion
	}
}
