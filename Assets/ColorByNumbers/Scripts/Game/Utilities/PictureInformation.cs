using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.ColorByNumbers
{
	public class PictureInformation
	{
		#region Member Variables

		private string			fileContents;
		private bool			isFileLoaded;
		private bool			isIdLoaded;

		private string			id;
		private int				xCells;
		private int				yCells;
		private List<List<int>>	colorNumbers;
		private List<Color>		colors;
		private bool			hasBlankCells;
		private bool			isLocked;
		private int				unlockAmount;
		private bool			awardOnComplete;
		private int				awardAmount;

		// Saved matrix of color numbers, -1 means it's colored in, number >= 0 means it still needs to be colored
		private List<List<int>>	progress;
		private List<int>		colorsLeft;
		private bool			unlocked;
		private bool			completed;

		#endregion

		#region Properties

		/// <summary>
		/// If true then the grayscale for the menu screen needs to be re-loaded and not use the one in the cache
		/// </summary>
		public bool ReloadGrayscale { get; set; }

		/// <summary>
		/// Gets the unique id of the picture
		/// </summary>
		public string Id
		{
			get
			{
				if (!isIdLoaded)
				{
					LoadIdFromPictureFile();
				}

				return id;
			}
		}

		/// <summary>
		/// Gets the number of X cells in the picture
		/// </summary>
		/// <value>The X cells.</value>
		public int XCells
		{
			get
			{
				if (!isFileLoaded)
				{
					LoadPictureFile();
				}

				return xCells;
			}
		}

		/// <summary>
		/// Gets the number of Y cells in the picture
		/// </summary>
		/// <value>The Y cells.</value>
		public int YCells
		{
			get
			{
				if (!isFileLoaded)
				{
					LoadPictureFile();
				}

				return yCells;
			}
		}

		/// <summary>
		/// Gets a matrix of each cell and the color number for the cell
		/// </summary>
		public List<List<int>> ColorNumbers
		{
			get
			{
				if (!isFileLoaded)
				{
					LoadPictureFile();
				}

				return colorNumbers;
			}
		}

		/// <summary>
		/// Gets a list of all the colors in the picture, the index of the color is it's number
		/// </summary>
		public List<Color> Colors
		{
			get
			{
				if (!isFileLoaded)
				{
					LoadPictureFile();
				}

				return colors;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance has blank cells.
		/// </summary>
		public bool HasBlankCells
		{
			get
			{
				if (!isFileLoaded)
				{
					LoadPictureFile();
				}

				return hasBlankCells;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is locked until purchased with in-game currency
		/// </summary>
		public bool IsLocked
		{
			get
			{
				if (!isFileLoaded)
				{
					LoadPictureFile();
				}

				return isLocked && !unlocked;
			}
		}

		/// <summary>
		/// Gets the unlock amount
		/// </summary>
		public int UnlockAmount
		{
			get
			{
				if (!isFileLoaded)
				{
					LoadPictureFile();
				}

				return unlockAmount;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance awards in-game currency when completed
		/// </summary>
		public bool AwardOnComplete
		{
			get
			{
				if (!isFileLoaded)
				{
					LoadPictureFile();
				}

				return awardOnComplete;
			}
		}

		/// <summary>
		/// Gets the award amount
		/// </summary>
		public int AwardAmount
		{
			get
			{
				if (!isFileLoaded)
				{
					LoadPictureFile();
				}

				return awardAmount;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance has saved progress
		/// </summary>
		public bool HasProgress
		{
			get
			{
				return progress != null;
			}
		}

		public List<List<int>> Progress
		{
			get
			{
				if (progress == null)
				{
					InitProgress();
				}

				return progress;
			}
		}

		public List<int> ColorsLeft
		{
			get
			{
				if (colorsLeft == null)
				{
					InitProgress();
				}

				return colorsLeft;
			}
		}

		public bool Completed
		{
			get
			{
				return completed;
			}
		}

		#endregion

		#region Public Methods

		public PictureInformation(string content)
		{
			fileContents = content;
		}

		/// <summary>
		/// Unlocks this PictureInformation instance so the user can play any number of times
		/// </summary>
		public void SetUnlocked()
		{
			unlocked = true;
		}

		/// <summary>
		/// Sets this PictureInformation instance completed
		/// </summary>
		public void SetCompleted(bool isComplete = true)
		{
			completed = isComplete;
		}

		/// <summary>
		/// Clears any progress, makes it so this instance was never started
		/// </summary>
		public void ClearProgress()
		{
			progress		= null;
			colorsLeft		= null;
			ReloadGrayscale	= true;
		}

		/// <summary>
		/// Checks if a given color is complete (Has all it's pixels colored in)
		/// </summary>
		public bool IsColorComplete(int colorIndex)
		{
			return HasProgress && colorsLeft[colorIndex] == 0;
		}

		/// <summary>
		/// Checks if this PictureInformation has all of it's pixels colored in
		/// </summary>
		public bool IsLevelComplete()
		{
			// Check if the level has any progress, it cant be complete if it has even been started yet
			if (HasProgress)
			{
				bool allColorsComplete = true;

				// Check if each of the colors are complete
				for (int i = 0; i < colors.Count; i++)
				{
					if (!IsColorComplete(i))
					{
						allColorsComplete = false;

						break;
					}
				}

				return allColorsComplete;
			}

			return false;
		}

		public Dictionary<string, object> GetSaveData()
		{
			Dictionary<string, object> saveData = new Dictionary<string, object>();

			saveData["has_progress"] = HasProgress;

			if (HasProgress)
			{
				saveData["progress"]	= Progress;
				saveData["colors_left"]	= ColorsLeft;
			}

			saveData["id"]			= Id;
			saveData["completed"]	= completed;
			saveData["unlocked"]	= unlocked;

			return saveData;
		}

		public void LoadSaveData(JSONNode saveData)
		{
			if (saveData["has_progress"].AsBool)
			{
				progress	= new List<List<int>>();
				colorsLeft	= new List<int>();

				foreach (JSONArray list in saveData["progress"].AsArray)
				{
					List<int> temp = new List<int>();

					foreach (JSONNode item in list)
					{
						temp.Add(item.AsInt);
					}

					progress.Add(temp);
				}

				foreach (JSONNode item in saveData["colors_left"].AsArray)
				{
					colorsLeft.Add(item.AsInt);
				}
			}

			completed	= saveData["completed"].AsBool;
			unlocked	= saveData["unlocked"].AsBool;
		}

		public void InitProgress()
		{
			if (!isFileLoaded)
			{
				LoadPictureFile();
			}

			progress	= new List<List<int>>();
			colorsLeft	= new List<int>();

			// Create the dictionary that keeps track of how many color cells are left for each color
			for (int i = 0; i < colors.Count; i++)
			{
				colorsLeft.Add(0);
			}

			// Copy the colorNumbers matrix
			for (int i = 0; i < colorNumbers.Count; i++)
			{
				progress.Add(new List<int>(colorNumbers[i]));

				// Increase the colorsLeft count for each of the colors
				for (int j = 0; j < colorNumbers[i].Count; j++)
				{
					int colorIndex = colorNumbers[i][j];

					if (colorIndex != -1)
					{
						colorsLeft[colorIndex]++;
					}
				}
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Loads just the levels id from the file
		/// </summary>
		private void LoadIdFromPictureFile()
		{
			int secondLineStartIndex	= fileContents.IndexOf('\n') + 1;
			int secondNewlineIndex		= fileContents.IndexOf('\n', secondLineStartIndex);
			int length					= secondNewlineIndex - secondLineStartIndex;

			id			= fileContents.Substring(secondLineStartIndex, length);
			isIdLoaded	= true;
		}

		/// <summary>
		/// Parses the picture file.
		/// </summary>
		private void LoadPictureFile()
		{
			List<string[]> lines = ParseCSVLines(fileContents);

			if (lines.Count == 0)
			{
				Debug.LogError("[PictureInformation] ParsePictureFile: Malformed file contents, there are no lines in the file.");

				return;
			}

			int index = 0;

			//formatVersion = lines[index][0];
			index++;

			id = lines[index][0];
			index++;

			// Get the level lock info
			if (!ParseBool(lines[index], 0, out isLocked) || !ParseInt(lines[index], 1, out unlockAmount))
			{
				Debug.LogError("[PictureInformation] ParsePictureFile: Malformed file contents, could not parse level lock information.");

				return;
			}

			index++;

			// Get the award info
			if (!ParseBool(lines[index], 0, out awardOnComplete) || !ParseInt(lines[index], 1, out awardAmount))
			{
				Debug.LogError("[PictureInformation] ParsePictureFile: Malformed file contents, could not parse level lock information.");

				return;
			}

			index++;

			// Get the number of x and y cells in the picture
			if (!ParseInt(lines[index], 0, out xCells) || !ParseInt(lines[index], 1, out yCells))
			{
				Debug.LogError("[PictureInformation] ParsePictureFile: Malformed file contents, could not parse xCells and/or yCells.");

				return;
			}

			index++;

			// Get a list of integers that represent what colors each pixel are
			colorNumbers = new List<List<int>>();

			for (int i = index; i < yCells + index; i++)
			{
				if (i >= lines.Count)
				{
					Debug.LogError("[PictureInformation] ParsePictureFile: Malformed file contents, no enough lines when parse color numbers.");

					return;
				}

				colorNumbers.Add(new List<int>());

				for (int j = 0; j < xCells; j++)
				{
					int number;

					if (!ParseInt(lines[i], j, out number))
					{
						Debug.LogError("[PictureInformation] ParsePictureFile: Malformed file contents, could not parse color number.");

						return;
					}

					if (number == -1)
					{
						hasBlankCells = true;
					}

					colorNumbers[i - index].Add(number);
				}
			}

			index += yCells;

			// Get the list of colors in the picture
			colors = new List<Color>();

			for (int i = index; i < lines.Count; i++)
			{
				float r, g, b;

				if (!ParseFloat(lines[i], 0, out r) ||
					!ParseFloat(lines[i], 1, out g) ||
					!ParseFloat(lines[i], 2, out b))
				{
					Debug.LogError("[PictureInformation] ParsePictureFile: Malformed file contents, could not parse color information. " + lines[i]);

					return;
				}

				colors.Add(new Color(r, g, b, 1f));
			}

			isIdLoaded		= true;
			isFileLoaded	= true;
		}

		/// <summary>
		/// Parses the CSV file and seperate the lines
		/// </summary>
		private List<string[]> ParseCSVLines(string csv)
		{
			List<string[]>	lines		= new List<string[]>();
			string[]		csvLines	= csv.Split('\n');

			for (int i = 0; i < csvLines.Length; i++)
			{
				string line = csvLines[i].Replace("\r", "").Trim();

				if (!string.IsNullOrEmpty(line))
				{
					lines.Add(line.Split(','));
				}
			}

			return lines;
		}

		/// <summary>
		/// Helper method that converts a string at the given index into an integer, returns false if it fails
		/// </summary>
		private bool ParseInt(string[] line, int index, out int value)
		{
			value = 0;

			if (index >= line.Length)
			{
				return false;
			}

			if (!int.TryParse(line[index], out value))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Helper method that converts a string at the given index into an float, returns false if it fails
		/// </summary>
		private bool ParseFloat(string[] line, int index, out float value)
		{
			value = 0;

			if (index >= line.Length)
			{
				return false;
			}

			if (!float.TryParse(line[index], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Helper method that converts a string at the given index into a boolean, returns false if it fails
		/// </summary>
		private bool ParseBool(string[] line, int index, out bool value)
		{
			value = false;

			if (index >= line.Length)
			{
				return false;
			}

			if (!bool.TryParse(line[index], out value))
			{
				return false;
			}

			return true;
		}

		#endregion
	}
}