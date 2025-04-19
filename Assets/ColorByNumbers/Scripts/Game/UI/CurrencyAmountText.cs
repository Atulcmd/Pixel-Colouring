using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	[RequireComponent(typeof(Text))]
	public class CurrencyAmountText : MonoBehaviour
	{
		#region Member Variables

		private Text uiText;

		#endregion

		#region Unity Methods

		private void Start()
		{
			uiText = gameObject.GetComponent<Text>();
		}

		private void Update()
		{
			uiText.text = GameController.Instance.CurrencyAmount.ToString();
		}

		#endregion
	}
}