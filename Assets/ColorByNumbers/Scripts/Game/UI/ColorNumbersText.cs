using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BizzyBeeGames.ColorByNumbers
{
	[ExecuteInEditMode]
	public class ColorNumbersText : Text
	{
		#region Properties

		public PictureInformation	PictureInfo		{ get; set; }
		public int					StartX			{ get; set; }
		public int					StartY			{ get; set; }
		public int					EndX			{ get; set; }
		public int					EndY			{ get; set; }
		public float				CellSize		{ get; set; }
		public float				LetterSpacing	{ get; set; }

		#endregion

		#region Protected Methods

		protected override void OnPopulateMesh(VertexHelper toFill)
		{
			base.OnPopulateMesh(toFill);

			List<UIVertex> stream = new List<UIVertex>();

			toFill.GetUIVertexStream(stream);

			if (stream.Count > 0 && PictureInfo != null)
			{
				List<UIVertex> newStream = new List<UIVertex>();

				for (int y = StartY; y <= EndY; y++)
				{
					for (int x = StartX; x <= EndX; x++)
					{
						// Get the index of the number in the stream (There are 6 verts per character)
						int number = GetNumber(x, y);

						AddNumber(x, StartY + EndY - y, number, stream, newStream);
					}
				}

				toFill.AddUIVertexTriangleStream(newStream);
			}
			else
			{
				toFill.AddUIVertexTriangleStream(stream);
			}
		}

	    #endregion

		#region Private Methods

		private int GetNumber(int x, int y)
		{
			if (PictureInfo.ColorNumbers[y][x] != -1 && (!PictureInfo.HasProgress || PictureInfo.Progress[y][x] != -1))
			{
				return PictureInfo.ColorNumbers[y][x] + 1;
			}

			return 0;
		}

		private void AddNumber(int x, int y, int number, List<UIVertex> characterStream, List<UIVertex> stream)
		{
			// Get all the individual digits in the number
			List<int> digits = GetDigits(number);

			// Get all the verticies for the numbers
			AddVerticies(x, y, digits, characterStream, stream);
		}

		/// <summary>
		/// Gets the digits.
		/// </summary>
		private List<int> GetDigits(int number)
		{
			// Get all the individual digits in the number
			List<int> digits = new List<int>();

			if (number == 0)
			{
				digits.Add(0);
			}
			else
			{
				while (number > 0)
				{
					int digit = number % 10;

					number = (number - digit) / 10;

					digits.Add(digit);
				}
			}

			return digits;
		}

		/// <summary>
		/// Adds the verticies.
		/// </summary>
		private void AddVerticies(int x, int y, List<int> digits, List<UIVertex> characterStream, List<UIVertex> stream)
		{
			// Get the center on the character on the grid
			float xCenter   = (x - StartX) * CellSize;
			float yCenter   = -(y - StartY) * CellSize;

			float	halfWidth	= 0;
			bool	first		= true;

			for (int i = digits.Count - 1; i >= 0; i--)
			{
				int		numberIndex		= digits[i] * 6;
				bool	useBlankVert	= (digits.Count == 1 && digits[0] == 0);

				// Get the original vertices for the number
				UIVertex vert1 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex];
				UIVertex vert2 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 1];
				UIVertex vert3 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 2];
				UIVertex vert4 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 3];
				UIVertex vert5 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 4];
				UIVertex vert6 = useBlankVert ? UIVertex.simpleVert : characterStream[numberIndex + 5];

				// Get the offset to the center of the character
				float xCenterOffset = Mathf.Abs(vert1.position.x - vert2.position.x) / 2f;
				float yCenterOffset = Mathf.Abs(vert1.position.y - vert3.position.y) / 2f;

				if (digits.Count > 1)
				{
					xCenter += xCenterOffset;
				}

				// Position the character on the grid
				vert1.position = new Vector3(xCenter - xCenterOffset, yCenter + yCenterOffset, vert1.position.z);
				vert2.position = new Vector3(xCenter + xCenterOffset, yCenter + yCenterOffset, vert2.position.z);
				vert3.position = new Vector3(xCenter + xCenterOffset, yCenter - yCenterOffset, vert3.position.z);
				vert4.position = new Vector3(xCenter + xCenterOffset, yCenter - yCenterOffset, vert4.position.z);
				vert5.position = new Vector3(xCenter - xCenterOffset, yCenter - yCenterOffset, vert5.position.z);
				vert6.position = new Vector3(xCenter - xCenterOffset, yCenter + yCenterOffset, vert6.position.z);

				// Add to the new stream
				stream.Add(vert1);
				stream.Add(vert2);
				stream.Add(vert3);
				stream.Add(vert4);
				stream.Add(vert5);
				stream.Add(vert6);
				
				xCenter 	+= xCenterOffset;
				halfWidth	+= xCenterOffset + (first ? 0 : LetterSpacing / 2f);
				first		= false;
			}

			if (digits.Count > 1)
			{
				int startStreamIndex = stream.Count - digits.Count * 6;

				for (int i = 0; i < digits.Count; i++)
				{
					float letterSpacing = i * LetterSpacing;

					for (int j = 0; j < 6; j++)
					{
						int			streamIndex	= startStreamIndex + i * 6 + j;
						UIVertex	vert		= stream[streamIndex];

						vert.position = new Vector3(vert.position.x - halfWidth + letterSpacing, vert.position.y);

						stream[streamIndex] = vert;

					}
				}
			}
		}

		#endregion
	}
}
