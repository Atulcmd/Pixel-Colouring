using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BizzyBeeGames.ColorByNumbers
{
	public class ColorByNumbersMenu
	{
		[MenuItem("Tools/Color By Numbers/Clear Save Data")]
		private static void DeleteSaveData()
		{
			// Delete the save file if it exists
			System.IO.File.Delete(Application.persistentDataPath + "/save.json");
		}

		[MenuItem("Tools/Color By Numbers/Clear Save Data", true)] 
		private static bool DeleteSaveDataValidation()
		{
			// Don't allow deleting save data while the application is running
			return !Application.isPlaying;
		}

		[MenuItem("Tools/Color By Numbers/Complete Active Level")]
		private static void CompleteActiveLevel()
		{
			GameController.Instance.CompleteActiveLevel();
		}

		[MenuItem("Tools/Color By Numbers/Complete Active Level", true)] 
		private static bool CompleteActiveLevelValidation()
		{
			// Can only complete the active level if the application is running and we are on the game screen
			return Application.isPlaying && ScreenTransitionController.Instance.CurrentScreenId == ScreenTransitionController.GameScreenId && GameController.Instance.ActivePictureInfo != null;
		}
	}
}
