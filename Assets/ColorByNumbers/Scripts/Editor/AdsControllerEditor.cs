using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BizzyBeeGames.ColorByNumbers
{
	[CustomEditor(typeof(AdsController))]
	public class AdsControllerEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.Space();

			// Banner Ad properties
			{
				SerializedProperty enableBannerAdsProp = serializedObject.FindProperty("enableAdMobBannerAds");

				EditorGUILayout.PropertyField(enableBannerAdsProp);

				if (enableBannerAdsProp.boolValue)
				{
					#if !ADMOB
					EditorGUILayout.HelpBox("AdMob has not been setup for this project. Please refer to the documentation for how to setup AdMob.", MessageType.Warning);
					#endif
				}

				GUI.enabled = enableBannerAdsProp.boolValue;

				EditorGUILayout.PropertyField(serializedObject.FindProperty("androidBannerAdUnitID"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("iosBannerAdUnitID"));

				GUI.enabled = true;
			}

			EditorGUILayout.Space();

			// Interstital Ad properties
			{
				SerializedProperty enableInterstitialAdsProp = serializedObject.FindProperty("enableInterstitialAds");
				SerializedProperty interstitialtypeProp = serializedObject.FindProperty("interstitialType");

				EditorGUILayout.PropertyField(enableInterstitialAdsProp);

				GUI.enabled = enableInterstitialAdsProp.boolValue;

				EditorGUILayout.PropertyField(interstitialtypeProp);

				if (interstitialtypeProp.enumValueIndex == 0)
				{
					#if !UNITYADS
					if (enableInterstitialAdsProp.boolValue)
					{
						EditorGUILayout.HelpBox("Unity Ads is not enabled in Unity Services", MessageType.Warning);
					}
					#endif

					EditorGUILayout.PropertyField(serializedObject.FindProperty("enableUnityAdsInEditor"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("placementId"));
				}
				else
				{
					#if !ADMOB
					if (enableInterstitialAdsProp.boolValue)
					{
						EditorGUILayout.HelpBox("AdMob has not been setup for this project. Please refer to the documentation for how to setup AdMob.", MessageType.Warning);
					}
					#endif

					EditorGUILayout.PropertyField(serializedObject.FindProperty("androidInterstitialAdUnitID"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("iosInterstitialAdUnitID"));
				}

				GUI.enabled = true;
			}

			// AdMob App Id
			{
				if (serializedObject.FindProperty("enableAdMobBannerAds").boolValue || serializedObject.FindProperty("interstitialType").enumValueIndex == 1)
				{
					EditorGUILayout.Space();

					EditorGUILayout.PropertyField(serializedObject.FindProperty("admobAndroidAppId"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("admobIOSAppId"));
				}
			}

			// Unity Game Id
			{
				if (serializedObject.FindProperty("interstitialType").enumValueIndex == 0)
				{
					EditorGUILayout.Space();

					EditorGUILayout.PropertyField(serializedObject.FindProperty("unityAndroidGameId"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("unityIOSGameId"));
				}
			}

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
