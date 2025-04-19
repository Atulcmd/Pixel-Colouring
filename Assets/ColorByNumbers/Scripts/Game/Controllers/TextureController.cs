using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.ColorByNumbers
{
	public class TextureController : SingletonComponent<TextureController>
	{
		#region Inspector Variables

		[Tooltip("The maximum number of textures that can be cached in memory. If this limit has been reached then the textures that have least recently been used will be removed.")]
		[SerializeField] private int maxCachedTextures = 50;

		#endregion

		#region Member Variables

		private Dictionary<string, Texture2D>	textureCache;
		private List<string>					cachedIds;

		#endregion

		#region Unity Methods

		protected override void Awake()
		{
			base.Awake();

			textureCache	= new Dictionary<string, Texture2D>();
			cachedIds		= new List<string>();
		}

		#endregion

		#region Public Methods

		public Texture2D LoadGrayscale(PictureInformation pictureInfo, float alpha = 1f)
		{
			string id = pictureInfo.Id;

			// Check if there is a cached texture
			if (textureCache.ContainsKey(id) && !pictureInfo.ReloadGrayscale)
			{
				// Move the id to the front of the list
				cachedIds.Remove(id);
				cachedIds.Add(id);

				// Return the cached texture
				return textureCache[id];
			}

			Texture2D grayscaleTexture = GenerateGrayscaleTexture(pictureInfo, alpha, true);

			CacheTexture(id, grayscaleTexture);

			pictureInfo.ReloadGrayscale = false;

			return grayscaleTexture;
		}

		public Texture2D LoadCompletedTexture(PictureInformation pictureInfo)
		{
			string id = pictureInfo.Id + "_completed";

			// Check if there is a cached texture
			if (textureCache.ContainsKey(id))
			{
				// Move the id to the front of the list
				cachedIds.Remove(id);
				cachedIds.Add(id);

				// Return the cached texture
				return textureCache[id];
			}

			Texture2D completeTexture = GenerateCompleteTexture(pictureInfo);

			CacheTexture(id, completeTexture);

			return completeTexture;
		}

		public List<Texture2D> LoadGameTextures(PictureInformation pictureInfo, float grayscaleAlpha, float selectionAlpha, float incorrectColorAlpha)
		{
			List<Texture2D> textures = new List<Texture2D>();

			textures.Add(GenerateMask(pictureInfo));
			textures.Add(GenerateGrayscaleTexture(pictureInfo, grayscaleAlpha, false));
			textures.Add(GenerateColoredTexture(pictureInfo, incorrectColorAlpha));
			textures.AddRange(GenerateSelectionOverlayTextures(pictureInfo, selectionAlpha));

			return textures;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Generates the grayscale texture.
		/// </summary>
		private Texture2D GenerateGrayscaleTexture(PictureInformation pictureInfo, float alpha, bool includeProgressColors)
		{
			Texture2D texture = new Texture2D(pictureInfo.XCells, pictureInfo.YCells, TextureFormat.RGBA32, false);

			texture.filterMode = FilterMode.Point;

			for (int y = 0; y < pictureInfo.YCells; y++)
			{
				for (int x = 0; x < pictureInfo.XCells; x++)
				{
					int		colorNumber	= pictureInfo.ColorNumbers[y][x];
					float	grayScale	= colorNumber == -1 ? 1f : pictureInfo.Colors[colorNumber].grayscale;

					grayScale = grayScale + (1f - grayScale) * (1f - alpha);

					Color pixelColor = new Color(grayScale, grayScale, grayScale, colorNumber == -1 ? 0f : 1f);

					// Check if this cell has been colored by the user
					if (includeProgressColors && pictureInfo.HasProgress && colorNumber != -1 && pictureInfo.Progress[y][x] != colorNumber)
					{
						// Check if its bee colored the correct color
						if (pictureInfo.Progress[y][x] == -1)
						{
							pixelColor = pictureInfo.Colors[colorNumber];
						}
						// If it's not the correct color then set the pixels color to the average of the color that has been set and the grayscale
						else
						{
							Color coloredColor = pictureInfo.Colors[pictureInfo.Progress[y][x]];

							pixelColor.r = (pixelColor.r + coloredColor.r) / 2f;
							pixelColor.g = (pixelColor.g + coloredColor.g) / 2f;
							pixelColor.b = (pixelColor.b + coloredColor.b) / 2f;
						}
					}

					texture.SetPixel(x, y, pixelColor);
				}
			}

			texture.Apply();

			return texture;
		}

		/// <summary>
		/// Loads the colored texture for the given PictureInformation
		/// </summary>
		private Texture2D GenerateColoredTexture(PictureInformation pictureInfo, float incorrectColorAlpha)
		{
			Texture2D texture	= new Texture2D(pictureInfo.XCells, pictureInfo.YCells, TextureFormat.ARGB32, false);
			texture.filterMode	= FilterMode.Point;

			// Set all the pixels to be clear at the start
			for (int x = 0; x < pictureInfo.XCells; x++)
			{
				for (int y = 0; y < pictureInfo.YCells; y++)
				{
					int		colorNumber	= pictureInfo.ColorNumbers[y][x];
					Color	pixelColor	= Color.clear;

					if (pictureInfo.HasProgress && colorNumber != -1 && pictureInfo.Progress[y][x] != colorNumber)
					{
						if (pictureInfo.Progress[y][x] == -1)
						{
							pixelColor = pictureInfo.Colors[colorNumber];
						}
						else
						{
							pixelColor		= pictureInfo.Colors[pictureInfo.Progress[y][x]];
							pixelColor.a	= incorrectColorAlpha;
						}
					}

					texture.SetPixel(x, y, pixelColor);
				}
			}

			texture.Apply();

			return texture;
		}

		/// <summary>
		/// Generates the all the selection overlay textures
		/// </summary>
		private List<Texture2D> GenerateSelectionOverlayTextures(PictureInformation pictureInfo, float selectionAlpha)
		{
			List<Texture2D> overlayTextures = new List<Texture2D>();

			// First create a texture for each of the colors
			for (int i = 0; i < pictureInfo.Colors.Count; i++)
			{
				Texture2D texture = new Texture2D(pictureInfo.XCells, pictureInfo.YCells, TextureFormat.ARGB32, false);

				texture.filterMode = FilterMode.Point;

				overlayTextures.Add(texture);
			}

			Color selectionColor = new Color(0f, 0f, 0f, selectionAlpha);

			// For each cell in the picture, get the overlay texture for that cell and set the color of the x/y pixel to the selectionOverlayColor
			for (int y = 0; y < pictureInfo.YCells; y++)
			{
				for (int x = 0; x < pictureInfo.XCells; x++)
				{
					int colorNumber	= pictureInfo.ColorNumbers[y][x];

					for (int i = 0; i < overlayTextures.Count; i++)
					{
						Texture2D	texture	= overlayTextures[i];	
						Color		color	= (i == colorNumber) ? selectionColor : Color.clear;

						texture.SetPixel(x, y, color);
					}
				}
			}

			// Apply the changes to all the overlay textures
			for (int i = 0; i < overlayTextures.Count; i++)
			{
				overlayTextures[i].Apply();
			}

			return overlayTextures;
		}

		/// <summary>
		/// Generates teh texture that will act as a mask for the blank tiles
		/// </summary>
		private Texture2D GenerateMask(PictureInformation pictureInfo)
		{
			if (!pictureInfo.HasBlankCells)
			{
				return null;
			}

			Texture2D texture = new Texture2D(pictureInfo.XCells, pictureInfo.YCells, TextureFormat.ARGB32, false);

			texture.filterMode = FilterMode.Point;

			for (int y = 0; y < pictureInfo.YCells; y++)
			{
				for (int x = 0; x < pictureInfo.XCells; x++)
				{
					int colorNumber	= pictureInfo.ColorNumbers[y][x];

					texture.SetPixel(x, y, colorNumber == -1 ? Color.clear : Color.white);
				}
			}

			texture.Apply();

			return texture;
		}

		/// <summary>
		/// Loads the completed texture
		/// </summary>
		private Texture2D GenerateCompleteTexture(PictureInformation pictureInfo)
		{
			Texture2D texture	= new Texture2D(pictureInfo.XCells, pictureInfo.YCells, TextureFormat.ARGB32, false);
			texture.filterMode	= FilterMode.Point;

			// Set all the pixels to be clear at the start
			for (int x = 0; x < pictureInfo.XCells; x++)
			{
				for (int y = 0; y < pictureInfo.YCells; y++)
				{
					int		colorNumber	= pictureInfo.ColorNumbers[y][x];
					Color	pixelColor	= (colorNumber == -1) ? Color.clear : pictureInfo.Colors[colorNumber];

					texture.SetPixel(x, y, pixelColor);
				}
			}

			texture.Apply();

			return texture;
		}

		/// <summary>
		/// Caches the grayscale texture.
		/// </summary>
		private void CacheTexture(string id, Texture2D texture)
		{
			// If the cached somehow already contains the texture just update it
			if (textureCache.ContainsKey(id))
			{
				textureCache[id] = texture;
			}
			else
			{
				// Cache the new texture
				textureCache.Add(id, texture);
				cachedIds.Add(id);

				// If there are now more than the max allowed cached textures remove the oldest one
				if (cachedIds.Count > maxCachedTextures)
				{
					string idToRemove = cachedIds[0];

					// Destroy the texture
					Destroy(textureCache[idToRemove]);

					cachedIds.RemoveAt(0);
					textureCache.Remove(idToRemove);
				}
			}
		}

		#endregion
	}
}
