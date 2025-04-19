using UnityEngine;
using System.Collections;

namespace BizzyBeeGames.ColorByNumbers
{
	public class IAPObject : MonoBehaviour
	{
		#region Unity Methods

		private void Start()
		{
			UpdateVisibility();

			if (IAPController.IsEnabled)
			{
				IAPController.Instance.OnInitializedSuccessfully += UpdateVisibility;
			}
		}

		#endregion

		#region Private Methods

		private void UpdateVisibility()
		{
			gameObject.SetActive(IAPController.IsEnabled && IAPController.Instance.IsInitialized);
		}

		#endregion
	}
}
