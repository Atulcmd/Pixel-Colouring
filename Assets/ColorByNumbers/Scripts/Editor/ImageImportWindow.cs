using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ImageImportWindow : EditorWindow
{
	#region Classes

	private class ImportedTexture
	{
		public int						xPixels;
		public int						yPixels;
		public Texture2D				texture;
		public List<PaletteItem>		palette;
		public Dictionary<string, int>	paletteIndexMap;
	}

	#endregion

	#region Enums

	private enum ScaleMode
	{
		BoxSampling,
		CenterPixel
	}

	private enum AwardType
	{
		Manual,
		PerPixel
	}

	#endregion

	#region Member Variables

	private const float Padding					= 5f;
	private const float FieldSpacing			= 3f;
	private const float FieldHeight				= 16f;
	private const float ButtonHeight			= 20f;
	private const float HeaderHeight			= 19f;
	private const float	PictureZoomAmount		= 0.5f;
	private const float	PictureControlsHeight	= 30f;
	private const float	ColorPaletteItemSize	= 30f;

	private Texture2D		importTexture;
	private bool			adjustToFit			= true;
	private int				xPixels				= 25;
	private int				yPixels				= 25;
	private ScaleMode		scaleMode			= ScaleMode.BoxSampling;
	private bool			mergeSimilarColors	= true;
	private float			mergeThreshold		= 2.3f;

	private bool		isLevelLocked				= false;
	private int			unlockAmount				= 100;
	private bool		awardOnCompletion	= false;
	private AwardType	awardType					= AwardType.Manual;
	private float		perPixelAmount				= 0.05f;
	private int			awardAmount					= 100;
	private string		fileName					= "";
	private string		outputFolder				= "";

	private ImportedTexture		importedTexture;
	private int					blankItemIndex = -1;
	private float				pictureScale = 1f;
	private bool				showGridLines;
	private bool				showNumbers;

	private Rect			windowRect;
	private Vector2			windowScrollPosition;
	private Vector2			textureScrollPosition;
	private Texture2D		headerLineTexture;
	private Texture2D		blackTexture;
	private List<Texture2D>	paletteItemTextures;

	// Textures for icons
	private Dictionary<string, Texture2D> iconTextures;

	// List of infomation about the icon textures, first value is key second is icon file name
	private static readonly List<string[]> iconTextureInfo = new List<string[]>()
	{
		new string[] { "grid_on_white", "icon_grid_on_white" },
		new string[] { "grid_on_black", "icon_grid_on_black" },
		new string[] { "zoom_in_black", "icon_zoom_in" },
		new string[] { "zoom_out_black", "icon_zoom_out" },
		new string[] { "numbers_white", "icon_numbers_white" },
		new string[] { "numbers_black", "icon_numbers_black" }
	};

	#endregion

	#region Properties

	private string AssetDirectory		{ get { return Application.dataPath + "/ColorByNumbers"; } }
	private string DefaultOutputFolder	{ get { return AssetDirectory + "/LevelFiles"; } }

	private bool IsTextureNull		{ get { return importTexture == null; } }
	private bool IsTextureReadable	{ get { return TextureUtilities.CheckIsReadWriteEnabled(importTexture); } }
	private bool HasImportedTexture	{ get { return importedTexture != null && importedTexture.texture != null; } }

	/// <summary>
	/// Gets the headerLineTexture, creates one if it is null
	/// </summary>
	private Texture2D LineTexture { get { return (headerLineTexture != null) ? headerLineTexture : (headerLineTexture = TextureUtilities.CreateTexture(1, 1, GUI.skin.label.normal.textColor)); } }

	/// <summary>
	/// Gets the blackTexture, creates one if it is null
	/// </summary>
	private Texture2D BlackTexture { get { return (blackTexture != null) ? blackTexture : (blackTexture = TextureUtilities.CreateTexture(1, 1, Color.black)); } }

	/// <summary>
	/// Gets the icon textures dictionary, creates it and loads all icon textures if its null
	/// </summary>
	private Dictionary<string, Texture2D> IconTextures
	{
		get
		{
			// Check if iconTextures is null, if so we need to load all the icon textures
			if (iconTextures == null)
			{
				iconTextures = new Dictionary<string, Texture2D>();

				for (int i = 0; i < iconTextureInfo.Count; i++)
				{
					string iconKey	= iconTextureInfo[i][0];
					string iconName	= iconTextureInfo[i][1];
					string iconPath	= string.Format("{0}/AssetFiles/Images/Editor/{1}.png", AssetDirectory, iconName);

					Texture2D texture = null;

					if (System.IO.File.Exists(iconPath))
					{
						texture = new Texture2D(1, 1);
						texture.LoadImage(System.IO.File.ReadAllBytes(iconPath));
						texture.Apply();
					}
					else
					{
						Debug.LogFormat("Icon does not exist: " + iconPath);
					}

					iconTextures.Add(iconKey, texture);
				}
			}

			return iconTextures;
		}
	}

	#endregion

	#region Unity Methods

	[MenuItem("Tools/Color By Numbers/CBN Image Import")]
	public static void Init()
	{
		EditorWindow.GetWindow<ImageImportWindow>("CBN Image Import");
	}

	private void OnDisable()
	{
		DestroyTexture(headerLineTexture);
		DestroyTexture(blackTexture);

		if (importedTexture != null)
		{
			DestroyTexture(importedTexture.texture);
		}

		if (iconTextures != null)
		{
			foreach (KeyValuePair<string, Texture2D> pair in iconTextures)
			{
				DestroyTexture(pair.Value);
			}
		}
	}

	#endregion

	#region Draw Methods

	private void OnGUI()
	{
		// Top and bottom padding
		float drawHeight = GetDrawHeight();

		windowScrollPosition = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), windowScrollPosition, new Rect(0, 0, 0, drawHeight), false, true);

		windowRect = new Rect(Padding, Padding, position.width - Padding * 2f - 14, position.height - Padding * 2f);

		DrawWindow();

		GUI.enabled = true;

		GUI.EndScrollView();
	}

	private void DrawWindow()
	{
		float	y = Padding;
		Rect	boxRect;
		Rect	drawRect;

		float importSettingsHeight	= GetImportSettingsDrawHeight();
		float levelSettingsHeight	= GetLevelSettingsDrawHeight();
		float previewHeight			= GetPreviewDrawHeight();

		boxRect		= new Rect(Padding, y, windowRect.width, importSettingsHeight);
		drawRect	= new Rect(Padding, Padding, boxRect.width - Padding * 2, boxRect.height - Padding * 2);

		GUI.BeginGroup(boxRect, GUI.skin.box);
		GUI.BeginGroup(drawRect);
		DrawImportSettings(new Rect(0, 0, drawRect.width, drawRect.height));
		GUI.EndGroup();
		GUI.EndGroup();

		y += importSettingsHeight + Padding;

		boxRect		= new Rect(Padding, y, windowRect.width, levelSettingsHeight);
		drawRect	= new Rect(Padding, Padding, boxRect.width - Padding * 2, boxRect.height - Padding * 2);

		GUI.BeginGroup(boxRect, GUI.skin.box);
		GUI.BeginGroup(drawRect);
		DrawLevelSettings(new Rect(0, 0, drawRect.width, drawRect.height));
		GUI.EndGroup();
		GUI.EndGroup();

		y += levelSettingsHeight + Padding;

		boxRect		= new Rect(Padding, y, windowRect.width, previewHeight);
		drawRect	= new Rect(Padding, Padding, boxRect.width - Padding * 2, boxRect.height - Padding * 2);

		GUI.enabled = HasImportedTexture;

		GUI.BeginGroup(boxRect, GUI.skin.box);
		GUI.BeginGroup(drawRect);
		DrawPreview(new Rect(0, 0, drawRect.width, drawRect.height));
		GUI.EndGroup();
		GUI.EndGroup();

		GUI.enabled = true;

		y += previewHeight + Padding;
	}

	private void DrawImportSettings(Rect drawRect)
	{
		float y = 0;

		y = DrawHeader("Import Settings", y, drawRect.width);
		y += FieldSpacing;

		Texture2D currentTexture = importTexture;

		y = DrawObjectField("Texture", importTexture, out importTexture, y, drawRect.width);
		y += FieldSpacing;

		// Check if the textures have changed
		if (currentTexture != importTexture)
		{
			if (importTexture != null && adjustToFit)
			{
				yPixels = Mathf.RoundToInt(importTexture.height / (importTexture.width / (float)xPixels));
			}
		}

		GUI.enabled = !IsTextureNull;

		y = DrawBoolField("Adjust X/Y To Fit", adjustToFit, out adjustToFit, y, drawRect.width);
		y += FieldSpacing;

		int newXPixels;
		int newYPixels;

		y = DrawIntField("X Pixels", xPixels, out newXPixels, y, drawRect.width);
		y += FieldSpacing;

		y = DrawIntField("Y Pixels", yPixels, out newYPixels, y, drawRect.width);
		y += FieldSpacing;

		if (adjustToFit && xPixels != newXPixels)
		{
			xPixels = newXPixels;
			yPixels = Mathf.RoundToInt(importTexture.height / (importTexture.width / (float)xPixels));
		}
		else if (adjustToFit && yPixels != newYPixels)
		{
			yPixels = newYPixels;
			xPixels = Mathf.RoundToInt(importTexture.width / (importTexture.height / (float)yPixels));
		}
		else
		{
			xPixels = newXPixels;
			yPixels = newYPixels;
		}

		xPixels = Mathf.Max(1, xPixels);
		yPixels = Mathf.Max(1, yPixels);

		System.Enum newScaleMode;

		y = DrawEnumField("Scale Mode", scaleMode, out newScaleMode, y, drawRect.width);
		y += FieldSpacing;

		scaleMode = (ScaleMode)newScaleMode;

		y = DrawBoolField("Merge Similar Colors", mergeSimilarColors, out mergeSimilarColors, y, drawRect.width);
		y += FieldSpacing;

		if (mergeSimilarColors)
		{
			y = DrawFloatField("Threshold", mergeThreshold, out mergeThreshold, y, drawRect.width);
			y += FieldSpacing;

			mergeThreshold = Mathf.Max(0, mergeThreshold);

		}

		bool clicked;

		DrawButton("Import Texture", out clicked, y, drawRect.width);

		if (clicked)
		{
			if (IsTextureReadable)
			{
				ImportTexture();
			}
			else
			{
				EditorUtility.DisplayDialog("Texture Not Readable", "The texture is not readable, please set the textures \"Read/Write Enabled\" property to true on the textures import settings.", "Ok");
			}
		}

		GUI.enabled = true;
	}

	private void DrawLevelSettings(Rect drawRect)
	{
		float y = 0;

		y = DrawHeader("Level Settings", y, drawRect.width);
		y += FieldSpacing;

		y = DrawBoolField("Is Level Locked", isLevelLocked, out isLevelLocked, y, drawRect.width);
		y += FieldSpacing;

		if (isLevelLocked)
		{
			y = DrawIntField("Unlock Amount", unlockAmount, out unlockAmount, y, drawRect.width);
			y += FieldSpacing;
			y += Padding;
		}

		y = DrawBoolField("Award On Completion", awardOnCompletion, out awardOnCompletion, y, drawRect.width);
		y += FieldSpacing;

		if (awardOnCompletion)
		{
			System.Enum newAwardType;

			y = DrawEnumField("Award Type", awardType, out newAwardType, y, drawRect.width);
			y += FieldSpacing;

			awardType = (AwardType)newAwardType;

			switch (awardType)
			{
				case AwardType.Manual:
					y = DrawIntField("Award Amount", awardAmount, out awardAmount, y, drawRect.width);
					y += FieldSpacing;
					break;
				case AwardType.PerPixel:
					y = DrawFloatField("Amount Per Pixel", perPixelAmount, out perPixelAmount, y, drawRect.width);
					y += FieldSpacing;

					awardAmount = HasImportedTexture ? Mathf.RoundToInt(importedTexture.xPixels * importedTexture.yPixels * perPixelAmount) : 0;

					y = DrawLabel("Award Amount: " + awardAmount, y, drawRect.width);
					y += FieldSpacing;
					break;
			}
		}

		y += Padding;

		y = DrawTextField("File Name", fileName, out fileName, y, drawRect.width);
		y += FieldSpacing;

		if (string.IsNullOrEmpty(outputFolder))
		{
			outputFolder = DefaultOutputFolder;
		}

		DrawLabel("Output Folder: " + outputFolder.Remove(0, Application.dataPath.Length - 6), y, drawRect.width);

		bool chooseClicked;

		chooseClicked = GUI.Button(new Rect(drawRect.width - 50, y, 50, FieldHeight), "Choose");

		if (chooseClicked)
		{
			string choosenFolder = EditorUtility.SaveFolderPanel("Level file output folder", "", "");

			if (!string.IsNullOrEmpty(choosenFolder))
			{
				if (!choosenFolder.StartsWith(Application.dataPath))
				{
					EditorUtility.DisplayDialog("Output Folder", "The output folder must be located in the projects Asset folder", "Ok");
				}
				else
				{
					outputFolder = choosenFolder;
				}
			}
		}

		y += FieldHeight + FieldSpacing;

		bool exportClicked;

		GUI.enabled = HasImportedTexture;

		DrawButton("Export Level", out exportClicked, y, drawRect.width);

		if (exportClicked)
		{
			bool export = true;

			if (!string.IsNullOrEmpty(fileName) && System.IO.File.Exists(string.Format("{0}/{1}.csv", outputFolder, fileName)))
			{
				export = EditorUtility.DisplayDialog("File Exists", "The picture file with the name " + fileName + " already exists. Would you like to overwrite it?", "Yes", "No");
			}

			if (export)
			{
				// Export the level to a file
				TextureUtilities.ExportTextureToFile(
					TextureUtilities.Texture2DHash(importedTexture.texture),
					importedTexture.xPixels,
					importedTexture.yPixels,
					importedTexture.palette,
					outputFolder,
					fileName,
					blankItemIndex,
					isLevelLocked,
					unlockAmount,
					awardOnCompletion,
					awardAmount);
				
				// Refresh the asset database so the new file shows up in the Project folder
				AssetDatabase.Refresh();
			}
		}

		GUI.enabled = true;
	}

	private void DrawPreview(Rect drawRect)
	{
		float y = 0;

		// Draw the preview header
		DrawHeader("Preview", y, drawRect.width);
		y += HeaderHeight + Padding;

		// Draw the control buttons
		Rect controlsRect = new Rect(0, y, drawRect.width, PictureControlsHeight);
		DrawTextureControls(controlsRect);
		y += PictureControlsHeight + Padding;

		// Draw the texture
		Rect textureRect = new Rect(0, y, drawRect.width, GetPreviewTextureHeight());
		DrawTexture(textureRect);
		y += GetPreviewTextureHeight() + Padding;

		// Draw the color palette header
		DrawHeader("Color Palette", y, drawRect.width);
		y += HeaderHeight + Padding;

		// Draw the color palette
		Rect paletteItemRect = new Rect(0, y, drawRect.width, GetColorPaletteHeight());
		DrawPaletteItems(paletteItemRect);
		y += GetColorPaletteHeight() + Padding;

		// Draw the palette info text
		DrawLabel("* Click a color palette item to toggle it as the \"blank\" color.", y, drawRect.width);
		DrawLabel("* Blank colors will be white in the game and will not need to be colored in.", y + FieldHeight, drawRect.width);
	}

	private void DrawTextureControls(Rect drawRect)
	{
		float		buttonSize	= drawRect.height;
		Rect		buttonRect	= new Rect(0, drawRect.y, buttonSize, buttonSize);
		GUIStyle	buttonStyle	= new GUIStyle(GUI.skin.button);

		bool guiEnabled = GUI.enabled;

		// If picture scale is 1 then disable the zoom out button
		GUI.enabled = guiEnabled && (pictureScale > 1);

		// Zoom out button
		if (GUI.Button(buttonRect, IconTextures["zoom_out_black"]))
		{
			pictureScale = Mathf.Max(1f, pictureScale - PictureZoomAmount);
		}

		GUI.enabled = guiEnabled;

		buttonRect.x += buttonSize + Padding;

		// Zoom in button
		if (GUI.Button(buttonRect, IconTextures["zoom_in_black"]))
		{
			pictureScale += PictureZoomAmount;
		}

		buttonRect.x += buttonSize + Padding;

		// Show/Hide grid lines toggle
		showGridLines = GUI.Toggle(buttonRect, showGridLines, IconTextures[showGridLines ? "grid_on_white" : "grid_on_black"], buttonStyle);

		buttonRect.x += buttonSize + Padding;

		// Show/Hide numbers toggle
		showNumbers = GUI.Toggle(buttonRect, showNumbers, IconTextures[showNumbers ? "numbers_white" : "numbers_black"], buttonStyle);
	}

	private void DrawTexture(Rect drawRect)
	{
		if (importedTexture == null || importedTexture.texture == null)
		{
			return;
		}

		Rect scrollRect = new Rect(drawRect);

		drawRect.size = GetPreviewTextureSize();

		if (pictureScale > 1)
		{
			textureScrollPosition = GUI.BeginScrollView(scrollRect, textureScrollPosition, drawRect);
		}
		else
		{
			textureScrollPosition = Vector2.zero;
		}

		GUI.DrawTexture(drawRect, importedTexture.texture, UnityEngine.ScaleMode.StretchToFill);

		if (showGridLines)
		{
			DrawPictureGridLines(drawRect);
		}

		if (showNumbers)
		{
			DrawGridNumbers(drawRect, scrollRect);
		}

		if (pictureScale > 1)
		{
			GUI.EndScrollView();
		}
	}

	private void DrawPictureGridLines(Rect pictureRect)
	{
		float cellWidth		= pictureRect.width / importedTexture.xPixels;
		float cellHeight	= pictureRect.height / importedTexture.yPixels;

		for (int x = 1; x < importedTexture.xPixels; x++)
		{
			float	xPos		= pictureRect.x + x * cellWidth;
			Rect	lineRect	= new Rect(xPos, pictureRect.y, 1, pictureRect.height);

			GUI.DrawTexture(lineRect, BlackTexture, UnityEngine.ScaleMode.StretchToFill);
		}

		for (int y = 1; y < importedTexture.yPixels; y++)
		{
			float	yPos		= pictureRect.y + y * cellHeight;
			Rect	lineRect	= new Rect(pictureRect.x, yPos, pictureRect.width, 1);

			GUI.DrawTexture(lineRect, BlackTexture, UnityEngine.ScaleMode.StretchToFill);
		}
	}

	private void DrawGridNumbers(Rect pictureRect, Rect scrollRect)
	{
		if (Event.current.type != EventType.Repaint)
		{
			return;
		}

		float cellSize = pictureRect.width / (float)importedTexture.xPixels;

		if (cellSize > 15)
		{
			int xStartCell	= Mathf.FloorToInt(textureScrollPosition.x / cellSize);
			int yStartCell	= Mathf.FloorToInt(textureScrollPosition.y / cellSize);
			int xEndCell	= Mathf.Min(importedTexture.xPixels - 1, xStartCell + Mathf.FloorToInt(scrollRect.width / cellSize));
			int yEndCell	= Mathf.Min(importedTexture.yPixels - 1, yStartCell + Mathf.FloorToInt(scrollRect.height / cellSize));

			GUIStyle numberStyle = new GUIStyle(GUI.skin.label);

			numberStyle.alignment = TextAnchor.MiddleCenter;

			for (int x = xStartCell; x <= xEndCell; x++)
			{
				for (int y = yStartCell; y <= yEndCell; y++)
				{
					int		xCell				= Mathf.Clamp(x, 0, importedTexture.xPixels - 1);
					int		yCell				= Mathf.Clamp(importedTexture.yPixels - y - 1, 0, importedTexture.yPixels);
					int		paletteItemIndex	= importedTexture.paletteIndexMap[PaletteIndexKey(xCell, yCell)];
					Color	paletteColor		= importedTexture.palette[paletteItemIndex].color;

					Rect rect = new Rect(scrollRect.x + (float)x * cellSize, scrollRect.y + (float)y * cellSize, cellSize, cellSize);

					DrawColorNumber(rect, paletteColor, paletteItemIndex);
				}
			}
		}
	}

	private void DrawPaletteItems(Rect drawRect)
	{
		int cols	= GetColorPaletteCols();
		int rows	= GetColorPaletteRows();
		int index	= 0;

		for (int row = 0; row < rows; row++)
		{
			float y = row * ColorPaletteItemSize + row * FieldSpacing;

			for (int col = 0; col < cols && index < paletteItemTextures.Count; col++)
			{
				float x = col * ColorPaletteItemSize + col * FieldSpacing;

				Rect paletteItemRect = new Rect(drawRect.x + x, drawRect.y + y, ColorPaletteItemSize, ColorPaletteItemSize);

				if (index == blankItemIndex)
				{
					GUI.DrawTexture(paletteItemRect, BlackTexture);

					float border = 3f;

					paletteItemRect.x		+= border;
					paletteItemRect.y		+= border;
					paletteItemRect.width	-= border * 2;
					paletteItemRect.height	-= border * 2;
				}

				GUIStyle style = new GUIStyle();
				style.normal.background = paletteItemTextures[index];

				if (GUI.Button(paletteItemRect, "", style))
				{
					blankItemIndex = (index == blankItemIndex) ? -1 : index;
				}

				DrawColorNumber(paletteItemRect, importedTexture.palette[index].color, index);

				index++;
			}
		}
	}

	private void DrawColorNumber(Rect rect, Color color, int paletteItemIndex)
	{
		if (Event.current.type != EventType.Repaint)
		{
			return;
		}

		GUIStyle numberStyle = new GUIStyle(GUI.skin.label);

		numberStyle.alignment = TextAnchor.MiddleCenter;

		int controlId = GUIUtility.GetControlID("EditorTextField".GetHashCode(), FocusType.Passive, rect);

		if (TextureUtilities.GetColorDiff(color, Color.black) < 2.3f)
		{
			numberStyle.normal.textColor = Color.white;
		}
		else
		{
			numberStyle.normal.textColor = Color.black;
		}

		string text = "";

		if (paletteItemIndex == blankItemIndex)
		{
			text = "B";
		}
		else if (blankItemIndex != -1 && paletteItemIndex > blankItemIndex)
		{
			text = paletteItemIndex.ToString();
		}
		else
		{
			text = (paletteItemIndex + 1).ToString();
		}

		numberStyle.Draw(rect, new GUIContent(text), controlId);
	}

	private float DrawTextField(string label, string value, out string outValue, float y, float width)
	{
		outValue = EditorGUI.TextField(new Rect(0, y, width, FieldHeight), label, value);

		return y + FieldHeight;
	}

	private float DrawButton(string text, out bool clicked, float y, float width)
	{
		clicked = GUI.Button(new Rect(0, y, width, 20), text);

		return y + ButtonHeight;
	}

	private float DrawEnumField(string label, System.Enum value, out System.Enum outValue, float y, float width)
	{
		outValue = EditorGUI.EnumPopup(new Rect(0, y, width, FieldHeight), label, value);

		return y + FieldHeight;
	}

	private float DrawFloatField(string label, float value, out float outValue, float y, float width)
	{
		outValue = EditorGUI.FloatField(new Rect(0, y, width, FieldHeight), label, value);

		return y + FieldHeight;
	}

	private float DrawIntField(string label, int value, out int outValue, float y, float width)
	{
		outValue = EditorGUI.IntField(new Rect(0, y, width, FieldHeight), label, value);

		return y + FieldHeight;
	}

	private float DrawBoolField(string label, bool value, out bool outValue, float y, float width)
	{
		outValue = EditorGUI.Toggle(new Rect(0, y, width, FieldHeight), label, value);

		return y + FieldHeight;
	}

	private float DrawObjectField<T>(string label, T obj, out T outObj, float y, float width) where T : Object
	{
		outObj = EditorGUI.ObjectField(new Rect(0, y, width, FieldHeight), label, obj, typeof(T), false) as T;

		return y + FieldHeight;
	}

	private float DrawHeader(string headerText, float y, float width)
	{
		y = DrawLabel(headerText, y, width);
		y = y + 1;
		y = DrawLine(y, width);

		return y;
	}

	private float DrawLabel(string text, float y, float width)
	{
		GUI.Label(new Rect(0, y, width, FieldHeight), text);

		return y + FieldHeight;
	}

	private float DrawLine(float y, float width)
	{
		GUI.DrawTexture(new Rect(0, y, width, 1), LineTexture);

		return y + 2;
	}

	private float GetDrawHeight()
	{
		float drawHeight = 0f;

		drawHeight += Padding;
		drawHeight += GetImportSettingsDrawHeight();
		drawHeight += Padding;
		drawHeight += GetLevelSettingsDrawHeight();
		drawHeight += Padding;
		drawHeight += GetPreviewDrawHeight();
		drawHeight += Padding;

		return drawHeight;
	}

	private float GetImportSettingsDrawHeight()
	{
		float numFields		= mergeSimilarColors ? 7f : 6f;
		float drawHeight	= 0f;

		drawHeight += Padding;
		drawHeight += HeaderHeight;
		drawHeight += FieldHeight * numFields + FieldSpacing * (numFields + 1);
		drawHeight += ButtonHeight;
		drawHeight += Padding;

		return drawHeight;
	}

	private float GetLevelSettingsDrawHeight()
	{
		float numFields		= 4f;
		float drawHeight	= 0f;

		if (isLevelLocked)
		{
			numFields	+= 1;
			drawHeight	+= Padding;
		}

		if (awardOnCompletion)
		{
			numFields += (awardType == AwardType.PerPixel) ? 3 : 2;
		}

		drawHeight += Padding;
		drawHeight += HeaderHeight;
		drawHeight += FieldHeight * numFields + FieldSpacing * (numFields + 1);
		drawHeight += Padding;
		drawHeight += ButtonHeight;
		drawHeight += Padding;

		return drawHeight;
	}

	private float GetPreviewDrawHeight()
	{
		float drawHeight = 0f;

		drawHeight += Padding;
		drawHeight += HeaderHeight;
		drawHeight += Padding;
		drawHeight += PictureControlsHeight;

		if (HasImportedTexture)
		{
			drawHeight += Padding;
			drawHeight += GetPreviewTextureHeight();
			drawHeight += Padding;
			drawHeight += HeaderHeight;
			drawHeight += Padding;
			drawHeight += GetColorPaletteHeight();
			drawHeight += Padding;
			drawHeight += FieldHeight * 2f;
		}

		drawHeight += Padding;

		return drawHeight;
	}

	private Vector2 GetPreviewTextureSize()
	{
		Vector2 size = Vector2.zero;

		if (HasImportedTexture)
		{
			if (importedTexture.xPixels > importedTexture.yPixels)
			{
				size.x = (windowRect.width - Padding * 2) * pictureScale;
				size.y = size.x * ((float)importedTexture.yPixels / (float)importedTexture.xPixels);
			}
			else
			{
				size.y = (windowRect.width - Padding * 2) * pictureScale;
				size.x = size.y * ((float)importedTexture.xPixels / (float)importedTexture.yPixels);
			}
		}

		return size;
	}

	private float GetPreviewTextureHeight()
	{
		return Mathf.Min(GetPreviewTextureSize().y, windowRect.width - Padding * 2);
	}

	private float GetColorPaletteHeight()
	{
		int rows = GetColorPaletteRows();

		return rows * ColorPaletteItemSize + (rows - 1) * FieldSpacing;
	}

	private int GetColorPaletteCols()
	{
		float drawWidth = (windowRect.width - Padding * 2);

		int cols = 1 + Mathf.FloorToInt((drawWidth - ColorPaletteItemSize) / (ColorPaletteItemSize + FieldSpacing));

		return cols;
	}

	private int GetColorPaletteRows()
	{
		return HasImportedTexture ? Mathf.CeilToInt((float)importedTexture.palette.Count / (float)GetColorPaletteCols()) : 0;
	}

	#endregion

	#region Methods

	private void ImportTexture()
	{
		Texture2D trimmedTexture = TextureUtilities.TrimAlpha(importTexture);

		bool firstItemBlank;

		xPixels = Mathf.Min(xPixels, trimmedTexture.width);
		yPixels = Mathf.Min(yPixels, trimmedTexture.height);

		Texture2D newTexture	= new Texture2D(xPixels, yPixels, TextureFormat.ARGB32, false);
		newTexture.filterMode	= FilterMode.Point;

		TextureUtilities.ScaleType	scaleType	= (scaleMode == ScaleMode.CenterPixel) ? TextureUtilities.ScaleType.CenterPixel : TextureUtilities.ScaleType.BoxSampling;
		List<PaletteItem>			palette		= TextureUtilities.Pixelize(trimmedTexture.GetPixels(), trimmedTexture.width, trimmedTexture.height, newTexture, xPixels, yPixels, scaleType, (mergeSimilarColors ? mergeThreshold : 0), out firstItemBlank);

		blankItemIndex = firstItemBlank ? 0 : -1;

		SortPalette(palette);

		importedTexture					= new ImportedTexture();
		importedTexture.xPixels			= xPixels;
		importedTexture.yPixels			= yPixels;
		importedTexture.texture			= newTexture;
		importedTexture.palette			= palette;
		importedTexture.paletteIndexMap	= new Dictionary<string, int>();

		for (int i = 0; i < importedTexture.palette.Count; i++)
		{
			PaletteItem paletteItem = importedTexture.palette[i];

			for (int j = 0; j < paletteItem.xCoords.Count; j++)
			{
				int x = paletteItem.xCoords[j];
				int y = paletteItem.yCoords[j];

				importedTexture.paletteIndexMap[PaletteIndexKey(x, y)] = i;
			}
		}

		// Create a new list of textures for each palette item
		DestroyTextures(paletteItemTextures);

		paletteItemTextures = new List<Texture2D>();

		for (int i = 0; i < importedTexture.palette.Count; i++)
		{
			paletteItemTextures.Add(TextureUtilities.CreateTexture(1, 1, importedTexture.palette[i].color));
		}

		Repaint();

		Debug.LogFormat("Imported texture has {0} palette colors", palette.Count);
	}

	private void SortPalette(List<PaletteItem> palette)
	{
		if (palette.Count > 2)
		{
			List<PaletteItem> tempPalette = new List<PaletteItem>(palette);

			palette.Clear();

			PaletteItem paletteItem = tempPalette[0];

			while (true)
			{
				palette.Add(paletteItem);

				tempPalette.Remove(paletteItem);

				if (tempPalette.Count == 0)
				{
					break;
				}

				float diff;

				paletteItem = TextureUtilities.GetClosestPaletteItem(paletteItem.color, tempPalette, out diff);
			}
		}
	}

	/// <summary>
	/// Gets the index key for the coords
	/// </summary>
	private string PaletteIndexKey(int x, int y)
	{
		return string.Format("{0}_{1}", x, y);
	}

	/// <summary>
	/// Destroys the given texture if it's not null
	/// </summary>
	private void DestroyTexture(Texture2D texture)
	{
		if (texture != null)
		{
			DestroyImmediate(texture);
		}
	}

	/// <summary>
	/// Destroies the textures.
	/// </summary>
	private void DestroyTextures(List<Texture2D> textures)
	{
		if (textures != null)
		{
			for (int i = 0; i < textures.Count; i++)
			{
				DestroyTexture(textures[i]);
			}
		}
	}

	#endregion
}
