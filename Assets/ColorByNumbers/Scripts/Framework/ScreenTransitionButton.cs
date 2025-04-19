using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BizzyBeeGames.ColorByNumbers
{
	[RequireComponent(typeof(Button))]
	public class ScreenTransitionButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string	screenIdToShow	= "";
		[SerializeField] private bool	isBackButton	= false;

		#endregion

		#region Unity Methods

		private void Start()
		{
			gameObject.GetComponent<Button>().onClick.AddListener(() => { ScreenTransitionController.Instance.Show(screenIdToShow, isBackButton); } );
		}

		#endregion
	}
}
