using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.ColorByNumbers
{
	public class PopupController : SingletonComponent<PopupController>
	{
		#region Classes

		[System.Serializable]
		private class PopupInfo
		{
			[Tooltip("The popups id, used to show the popup. Should be unique between all other popups.")]
			public string popupId = "";

			[Tooltip("The Popup component to show.")]
			public Popup popup = null;
		}

		#endregion

		#region Inspector Variables

		[SerializeField] private List<PopupInfo> popupInfos = null;

		#endregion

		#region Public Methods

		public void Show(string id)
		{
			Show(id, null, null);
		}

		public void Show(string id, object[] inData)
		{
			Show(id, inData, null);
		}

		public void Show(string id, object[] inData, Popup.PopupClosed popupClosed)
		{
			Popup popup = GetPopupById(id);

			if (popup != null)
			{
				popup.Show(inData, popupClosed);
			}
			else
			{
				Debug.LogErrorFormat("[PopupController] Popup with id {0} does not exist", id);
			}
		}

		#endregion

		#region Private Methods

		private Popup GetPopupById(string id)
		{
			for (int i = 0; i < popupInfos.Count; i++)
			{
				if (id == popupInfos[i].popupId)
				{
					return popupInfos[i].popup;
				}
			}

			return null;
		}

		#endregion
	}
}