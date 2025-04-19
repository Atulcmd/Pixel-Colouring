using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BizzyBeeGames.ColorByNumbers
{
	[CustomEditor(typeof(PictureScrollArea))]
	public class PictureScrollAreaEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.Space();

			EditorGUILayout.HelpBox("To zoom in the editor, first click and hold the picture then hold the 'Z' key on your keyboard, move the mouse to then zoom in/out.", MessageType.Info);
		}
	}
}
