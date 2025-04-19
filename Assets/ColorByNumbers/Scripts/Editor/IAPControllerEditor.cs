using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BizzyBeeGames.ColorByNumbers
{
	[CustomEditor(typeof(IAPController))]
	public class IAPControllerEditor : Editor
	{
		public override void OnInspectorGUI ()
		{
			#if !UNITY_IAP
			SerializedProperty enabledProp = serializedObject.FindProperty("enableIAP");

			if (enabledProp.boolValue)
			{
				EditorGUILayout.Space();

				EditorGUILayout.HelpBox("IAP has not been setup for this project. Please refer to the documentation for how to setup IAP.", MessageType.Warning);

				EditorGUILayout.Space();
			}
			#endif

			base.OnInspectorGUI();
		}
	}
}
