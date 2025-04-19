using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace BizzyBeeGames.ColorByNumbers
{
	[RequireComponent(typeof(Image))]
	public class ImageSpriteSheet : MonoBehaviour
	{
		[SerializeField] private Sprite	spriteSheet		= null;
		[SerializeField] private int	numberOfFrames	= 0;
		[SerializeField] private int	framesASecond	= 24;

		private Image		image;
		private Sprite[]	sprites;
		private float		time;

		private void Start()
		{
			sprites = new Sprite[numberOfFrames];

			float spriteWidth	= (float)spriteSheet.texture.width / (float)numberOfFrames;
			float spriteHeight	= spriteSheet.texture.height;

			for (int i = 0; i < numberOfFrames; i++)
			{
				sprites[i] = Sprite.Create(spriteSheet.texture, new Rect((float)i * spriteWidth, 0, spriteWidth, spriteHeight), new Vector2(0.5f, 0.5f));
			}

			image			= gameObject.GetComponent<Image>();
			image.sprite	= sprites[0];
		}

		private void Update()
		{
			time += Time.deltaTime;

			int frame = (int)(time / (1f / (float)framesASecond)) % numberOfFrames;
		
			image.sprite = sprites[frame];
		}
	}
}
