using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.ColorByNumbers
{
	public class CreatePictureWorker : Worker
	{
		#region Classes

		private class PaletteInfo
		{
			public Dictionary<Color, PaletteItemInfo>	paletteMap;
			public PaletteItem							closestPaletteItem1;
			public PaletteItem							closestPaletteItem2;
			public float								closestPaletteItemDiff;
		}

		private class PaletteItemInfo
		{
			public PaletteItem						paletteItem;
			public SortedList<float, PaletteItem>	diffs;

			public PaletteItemInfo(PaletteItem paletteItem)
			{
				this.paletteItem	= paletteItem;
				this.diffs			= new SortedList<float, PaletteItem>(new ColorDiffComparer());
			}
		}

		private class ColorDiffComparer : IComparer<float>
		{
			public int Compare(float x, float y)
			{
				int result = x.CompareTo(y);

				if (result == 0)
				{
					return 1;
				}

				return result;
			}
		}

		#endregion

		#region Properties

		public Color[]	InTexture		{ get; set; }
		public int		InTextureWidth	{ get; set; }
		public int		InTextureHeight	{ get; set; }
		public int 		MaxPaletteSize	{ get; set; }
		public float	MinPaletteDiff	{ get; set; }

		public List<PaletteItem> OutPaletteItems { get; set; }

		#endregion

		#region Protected Methods

		protected override void Begin()
		{
		}

		protected override void DoWork()
		{
			PaletteInfo paletteInfo = Process();

			OutPaletteItems = new List<PaletteItem>();

			foreach (KeyValuePair<Color, PaletteItemInfo> pair in paletteInfo.paletteMap)
			{
				OutPaletteItems.Add(pair.Value.paletteItem);
			}

			Stop();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Processes the current displayTexture, reducing the number of unique colors by merging colors that are closest together
		/// </summary>
		private PaletteInfo Process()
		{
			PaletteInfo paletteInfo = new PaletteInfo();
			paletteInfo.paletteMap	= new Dictionary<Color, PaletteItemInfo>();

			// Generate the initial palette
			for (int x = 0; x < InTextureWidth; x++)
			{
				for (int y = 0; y < InTextureHeight; y++)
				{
					int		index		= GetIndex(x, y, InTextureHeight);
					Color	pixelColor	= InTexture[index];

					// Bitshift the color so its a 12 bit color instead of a 24 bit color this makes it so there is only 4095 possible unique colors
					// instead of 16777215 colors which will make finding the closest colors alot faster while still retaining a good color range
					int r = ((int)(pixelColor.r * 255f) >> 4) << 4;
					int g = ((int)(pixelColor.g * 255f) >> 4) << 4;
					int b = ((int)(pixelColor.b * 255f) >> 4) << 4;

					pixelColor = new Color((float)r / 255f, (float)g / 255f, (float)b / 255f);

					if (paletteInfo.paletteMap.ContainsKey(pixelColor))
					{
						// If the color already exists in the palette then add the pixel coordintates
						paletteInfo.paletteMap[pixelColor].paletteItem.AddCoords(x, y);
					}
					else
					{
						// This is a new color so add it to the palette
						AddPaletteItem(paletteInfo, new PaletteItem(pixelColor, x, y));
					}
				}
			}

			// Merge any colors that are really close to one another until there are less than the max amount of colors
			while (paletteInfo.paletteMap.Count > MaxPaletteSize || (MinPaletteDiff != 0f && paletteInfo.paletteMap.Count > 1 && paletteInfo.closestPaletteItemDiff < MinPaletteDiff))
			{
				Debug.Log(paletteInfo.closestPaletteItemDiff);

				PaletteItem paletteItem1 = paletteInfo.closestPaletteItem1;
				PaletteItem paletteItem2 = paletteInfo.closestPaletteItem2;

				paletteInfo.closestPaletteItem1 = null;
				paletteInfo.closestPaletteItem2 = null;

				MergePaletteItems(paletteInfo, paletteItem1, paletteItem2);
			}

			return paletteInfo;
		}

		/// <summary>
		/// Adds the PaletteItem to the palette and calculates the difference between every other color already in the palette
		/// </summary>
		private void AddPaletteItem(PaletteInfo paletteInfo, PaletteItem paletteItemToAdd)
		{
			PaletteItemInfo newPaletteItemInfo = new PaletteItemInfo(paletteItemToAdd);

			// Get teh difference between the new color and every other color in the palette
			foreach (KeyValuePair<Color, PaletteItemInfo> pair in paletteInfo.paletteMap)
			{
				PaletteItemInfo paletteItemInfo = pair.Value;

				float diff = TextureUtilities.GetColorDiff2(newPaletteItemInfo.paletteItem.color, paletteItemInfo.paletteItem.color);

				newPaletteItemInfo.diffs.Add(diff, paletteItemInfo.paletteItem);
				paletteItemInfo.diffs.Add(diff, newPaletteItemInfo.paletteItem);
			}

			// Add the new palette item info
			paletteInfo.paletteMap.Add(newPaletteItemInfo.paletteItem.color, newPaletteItemInfo);

			// Only do the next part if there is more than one color in the palette now
			if (paletteInfo.paletteMap.Count > 1)
			{
				// Get the two palette items that are the closest together then any other palette item
				foreach (KeyValuePair<Color, PaletteItemInfo> pair in paletteInfo.paletteMap)
				{
					PaletteItemInfo paletteItemInfo = pair.Value;

					float diff = paletteItemInfo.diffs.Keys[0];

					if (paletteInfo.closestPaletteItem1 == null || diff < paletteInfo.closestPaletteItemDiff)
					{
						paletteInfo.closestPaletteItem1		= paletteItemInfo.paletteItem;
						paletteInfo.closestPaletteItem2		= paletteItemInfo.diffs.Values[0];
						paletteInfo.closestPaletteItemDiff	= diff;
					}
				}
			}
		}

		/// <summary>
		/// Merges the two palette items together into one palette item and adds it to the palette
		/// </summary>
		private void MergePaletteItems(PaletteInfo paletteInfo, PaletteItem paletteItem1, PaletteItem paletteItem2)
		{
			// Remove the two palette items because we are going to create a new one
			paletteInfo.paletteMap.Remove(paletteItem1.color);
			paletteInfo.paletteMap.Remove(paletteItem2.color);

			// For every other palette item, remove the two palette items that where removed from the diffs list
			foreach (KeyValuePair<Color, PaletteItemInfo> pair in paletteInfo.paletteMap)
			{
				pair.Value.diffs.RemoveAt(pair.Value.diffs.IndexOfValue(paletteItem1));
				pair.Value.diffs.RemoveAt(pair.Value.diffs.IndexOfValue(paletteItem2));
			}

			// Merge and add the new palette item
			AddPaletteItem(paletteInfo, PaletteItem.Merge(paletteItem1, paletteItem2));
		}

		/// <summary>
		/// Gets the index for InTexture
		/// </summary>
		private int GetIndex(int x, int y, int size)
		{
			return x + y * size;
		}

		#endregion
	}
}
