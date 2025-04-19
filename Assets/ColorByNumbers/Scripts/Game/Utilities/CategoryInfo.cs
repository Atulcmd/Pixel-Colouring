using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.ColorByNumbers
{
	[System.Serializable]
	public class CategoryInfo
	{
		#region Inspector Variables

		[Tooltip("Name that is displayed in the CategoryListItem")]
		public string displayName;

		[Tooltip("List of all the picture files that can be played in this category. Picture files are generated using the \"CBN Image Import\" window which can be opened by selecting the menu item \"Color By Numbers -> CBN Image Import\"")]
		public List<TextAsset> pictureFiles;

		#endregion

		#region Properties

		/// <summary>
		/// List of PictureInformations for eac picture file foe this category only
		/// </summary>
		public List<PictureInformation> PictureInfos { get; set; }

		#endregion
	}
}
