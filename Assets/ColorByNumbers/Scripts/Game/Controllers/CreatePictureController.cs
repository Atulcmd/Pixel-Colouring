using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Threading;

namespace BizzyBeeGames.ColorByNumbers
{
	public class CreatePictureController : SingletonComponent<CreatePictureController>
	{
		#region Enums

		private enum State
		{
			None,
			Capturing,
			Previewing,
			Processing
		}

		#endregion

		#region Inspector Variables

		[Tooltip("The minumum number of x/y pixels that the final picture can contain.")]
		[SerializeField] private int minSize = 50;

		[Tooltip("The maximum number of x/y pixels that the final picture can contain.")]
		[SerializeField] private int maxSize = 100;

		[Tooltip("The maximum number of colors that can be in the final pictures color palette.")]
		[SerializeField] private int maxColorPaletteSize = 50;

		[Tooltip("The minumum difference between two colors in the color palette. If two colors are close together then a single color is created from the average of the two.")]
		[SerializeField] private float minColorPaletteDiff = 0.1f;

		[Tooltip("The parent RectTransform of the cameraImage RawImage. The cameraImages RectTransforms width/height will be set to the minumum of the parents width/height so that the cameraImage stays in it's parents bounds for different screen sizes.")]
		[SerializeField] private RectTransform cameraImageParent = null;

		[Tooltip("The RawImage component that the devices camera input texture will be set to.")]
		[SerializeField] private RawImage cameraImage = null;

		[Tooltip("The slider that controls the number of x/y pixels in the final picture.")]
		[SerializeField] private Slider difficultySlider = null;

		[Tooltip("The GameObject for the switch camera button. Will be set to in-active if the device has only one camera.")]
		[SerializeField] private GameObject switchCameraButton = null;

		[Tooltip("The GameObject that is set to active if the state is Capturing, ie. we are currently displaying the cameras input.")]
		[SerializeField] private GameObject capturingContainer = null;

		[Tooltip("The GameObject that is set to active if the state is Previewing, ie. the user click the take picture button.")]
		[SerializeField] private GameObject previewContainer = null;

		[Tooltip("The GameObject that is set to active if the state is Processing, ie. the user click the accept button and the image is processing before playing the level.")]
		[SerializeField] private GameObject processingContainer	= null;

		#endregion

		#region Member Variables

		private int					deviceIndex;
		private WebCamTexture		webCamTexture;
		private int					textureSize;
		private Color[]				textureColors;
		private Texture2D			displayTexture;
		private int					prevPictureSize;
		private bool				updateDisplayTexture;
		private CreatePictureWorker	worker;
		private State				state;

		#endregion

		#region Properties

		private string	CreatePictureFolder		{ get { return Application.persistentDataPath + "/CustomPictures"; } }
		private string	CreatePictureIdFilePath	{ get { return CreatePictureFolder + "/next_id.txt"; } }
		private int		PictureSize				{ get { return (int)Mathf.Lerp(minSize, maxSize, difficultySlider.value); } }

		private string AndroidDeviceImagePath { get { return Application.persistentDataPath + "/device_image.png"; } }

		#endregion

		#region Unity Methods

		private void Start()
		{
			switchCameraButton.SetActive(WebCamTexture.devices.Length > 1);
		}

		private void Update()
		{
			// Check if we are currently capturing and the camera texture updated
			if (state == State.Capturing && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame)
			{
				// Get the new texture from the camera
				textureSize				= Mathf.Min(webCamTexture.width, webCamTexture.height);
				textureColors			= GetCameraColors();
				updateDisplayTexture	= true;
			}

			// Check if the picture size has changes, if so then flag that we need to update the texture
			if (state == State.Previewing && PictureSize != prevPictureSize)
			{
				updateDisplayTexture = true;
			}

			prevPictureSize = PictureSize;

			// Check if either the camera texture updated of the size updated
			if (updateDisplayTexture)
			{
				// Check if we need to create a new display texture
				if (displayTexture == null || displayTexture.width != PictureSize)
				{
					CreateNewDisplayTexture();
				}

				float scale	= (float)textureSize / (float)PictureSize;

				// Scale the camera texture down to the pixel size and only get the grayscale color for the display texture
				for (int x = 0; x < PictureSize; x++)
				{
					for (int y = 0; y < PictureSize; y++)
					{
						int xCenter	= Mathf.FloorToInt((float)x * scale + scale / 2f);
						int yCenter	= Mathf.FloorToInt((float)y * scale + scale / 2f);

						Color pixelColor = textureColors[GetIndex(xCenter, yCenter, textureSize)];

						pixelColor = new Color(pixelColor.grayscale, pixelColor.grayscale, pixelColor.grayscale);

						displayTexture.SetPixel(x, y, pixelColor);
					}
				}

				displayTexture.Apply();
				
				prevPictureSize = PictureSize;
			}

			// Check if we are processing the display texture, this happens when the user clicks the blue checkmark button
			if (state == State.Processing)
			{
				// If the worked has stopped that means it has completed its task
				if (worker.Stopped)
				{
					// Get the list of PaletteItems that was created
					List<PaletteItem> paletteItems = worker.OutPaletteItems;

					// Save and get the contents of the new picture file
					string contents = SaveToFile(paletteItems, worker.InTextureWidth, worker.InTextureHeight);

					// Create a new PictureInformation for the newly created file and add it to the list in the GameController
					PictureInformation pictureInformation = new PictureInformation(contents);

					// Need to call init progress or the picture will not appear in the my works list
					pictureInformation.InitProgress();

					// Start the level
					GameController.Instance.StartLevel(pictureInformation);

					// Show the game screen
					ScreenTransitionController.Instance.Show(ScreenTransitionController.GameScreenId);

					SetState(State.None);

					worker = null;
				}
			}

			// Make sure the cameraImage remains a square that fits inside it's parent
			if (cameraImageParent != null)
			{
				float cameraImageSize = Mathf.Min(cameraImageParent.rect.width, cameraImageParent.rect.height);

				cameraImage.rectTransform.sizeDelta = new Vector2(cameraImageSize, cameraImageSize);
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Starts the web cam capturing
		/// </summary>
		public void StartCapturing()
		{
			if (webCamTexture == null)
			{
				InitWebCamTexture();
			}

			if (webCamTexture != null && !webCamTexture.isPlaying)
			{
				difficultySlider.value = 0.5f;

				CreateNewDisplayTexture();

				webCamTexture.Play();

				SetState(State.Capturing);
			}
		}

		/// <summary>
		/// Stops capturing
		/// </summary>
		public void StopCapturing()
		{
			if (webCamTexture != null && webCamTexture.isPlaying)
			{
				webCamTexture.Stop();

				SetState(State.None);
			}
		}

		/// <summary>
		/// Switches between the front and back facing cameras
		/// </summary>
		public void SwitchCamera()
		{
			if (webCamTexture != null && webCamTexture.isPlaying && WebCamTexture.devices.Length > 1)
			{
				deviceIndex = (deviceIndex + 1) % 2;

				webCamTexture.Stop();

				webCamTexture.deviceName = WebCamTexture.devices[deviceIndex].name;

				webCamTexture.Play();
			}
		}

		/// <summary>
		/// Opens the devices image picker so the user can select an image from their device to use. The OnDeviceImagePicked method
		/// will be called by the native plugin when the user has selected an image or cancelled.
		/// </summary>
		public void GetDeviceImage()
		{
			#if !UNITY_EDITOR
			if (NativePlugin.HasReadExternalStoragePermission())
			{
				NativePlugin.ShowImagePicker(gameObject.name, "OnDeviceImagePicked", AndroidDeviceImagePath);
				StopCapturing();
			}
			else
			{
				PopupController.Instance.Show("permissions_popup", new object[] { "Storage" });
			}
			#endif
		}

		/// <summary>
		/// Sets the state to preview and stops capturing
		/// </summary>
		public void Preview()
		{
			if (webCamTexture != null && webCamTexture.isPlaying)
			{
				webCamTexture.Stop();

				SetState(State.Previewing);
			}
		}

		/// <summary>
		/// Process this instance.
		/// </summary>
		public void Process()
		{
			Color[]	inTexture	= new Color[PictureSize * PictureSize];
			float 	scale		= (float)textureSize / (float)PictureSize;

			for (int x = 0; x < PictureSize; x++)
			{
				for (int y = 0; y < PictureSize; y++)
				{
					int xCenter	= Mathf.FloorToInt((float)x * scale + scale / 2f);
					int yCenter	= Mathf.FloorToInt((float)y * scale + scale / 2f);

					Color pixelColor = textureColors[GetIndex(xCenter, yCenter, textureSize)];

					inTexture[GetIndex(x, y, PictureSize)] = pixelColor;
				}
			}

			worker					= new CreatePictureWorker();
			worker.InTexture		= inTexture;
			worker.InTextureWidth	= PictureSize;
			worker.InTextureHeight	= PictureSize;
			worker.MaxPaletteSize	= maxColorPaletteSize;
			worker.MinPaletteDiff	= minColorPaletteDiff;

			new Thread(new ThreadStart(worker.Run)).Start();

			SetState(State.Processing);
		}

		/// <summary>
		/// Returns a list of paths to any user create picture files
		/// </summary>
		public List<string> GetUserCreatedPictureContents()
		{
			List<string> pictureContents = new List<string>();

			if (System.IO.Directory.Exists(CreatePictureFolder))
			{
				string[] files = System.IO.Directory.GetFiles(CreatePictureFolder, "*.csv");

				for (int i = 0; i < files.Length; i++)
				{
					pictureContents.Add(System.IO.File.ReadAllText(files[i]));
				}
			}

			return pictureContents;
		}

		#endregion

		#region Private Methods

		private void InitWebCamTexture()
		{
			webCamTexture				= new WebCamTexture();
			webCamTexture.requestedFPS	= 24;
		}

		private void CreateNewDisplayTexture()
		{
			if (displayTexture != null)
			{
				Destroy(displayTexture);
			}

			displayTexture				= new Texture2D(PictureSize, PictureSize, TextureFormat.RGB24, false);
			displayTexture.filterMode	= FilterMode.Point;

			cameraImage.texture = displayTexture;
		}

		private Color[] GetCameraColors()
		{
			int rotation	= Mathf.RoundToInt((float)webCamTexture.videoRotationAngle / 90f) * 90;
			int size		= textureSize;

			Color[]	cameraColors	= null;
			int		index			= 0;

			switch (rotation)
			{
				case 90:
					cameraColors = new Color[size * size];

					for (int x = size - 1; x >= 0; x--)
					{
						for (int y = 0; y < size; y++)
						{
							cameraColors[index++] = webCamTexture.GetPixel(x, y);
						}
					}

					return cameraColors;
				case 180:
					cameraColors = new Color[size * size];

					for (int y = size - 1; y >= 0; y--)
					{
						for (int x = size - 1; x >= 0; x--)
						{
							cameraColors[index++] = webCamTexture.GetPixel(x, y);
						}
					}

					return cameraColors;
				case 270:
					cameraColors = new Color[size * size];

					for (int x = 0; x < size; x++)
					{
						for (int y = size - 1; y >= 0; y--)
						{
							cameraColors[index++] = webCamTexture.GetPixel(x, y);
						}
					}

					return cameraColors;
			}

			return webCamTexture.GetPixels(0, 0, size, size);
		}

		private void SetState(State newState)
		{
			state = newState;

			if (state != State.None)
			{
				capturingContainer.SetActive(state == State.Capturing);
				previewContainer.SetActive(state == State.Previewing);
				processingContainer.SetActive(state == State.Processing);
			}
		}

		private int GetIndex(int x, int y, int size)
		{
			return x + y * size;
		}

		private string SaveToFile(List<PaletteItem> paletteItems, int xPixels, int yPixels)
		{
			// Create the custom image directory if is does not exist
			if (!System.IO.Directory.Exists(CreatePictureFolder))
			{
				System.IO.Directory.CreateDirectory(CreatePictureFolder);
			}

			int nextId = 1;

			// Check if there exists an id file to use
			if (System.IO.File.Exists(CreatePictureIdFilePath))
			{
				string idFileContents = System.IO.File.ReadAllText(CreatePictureIdFilePath);

				// Parse the id in the file
				if (!int.TryParse(idFileContents, out nextId))
				{
					// Shouldn't ever get here unless someone tampers with the file
					nextId = 1;
				}
			}

			// Save the file to the device
			string id		= "custom_" + nextId;
			string contents	= TextureUtilities.ExportTextureToFile(id, xPixels, yPixels, paletteItems, CreatePictureFolder, id);

			// Write the next id to use to the file
			System.IO.File.WriteAllText(CreatePictureIdFilePath, (nextId + 1).ToString());

			return contents;
		}

		/// <summary>
		/// Invoked by the native plugins when the user has selected an image from the device to use
		/// </summary>
		private void OnDeviceImagePicked(string message)
		{
			if (message.StartsWith("ERROR"))
			{
				Debug.LogError("[CreatePictureController] Error when trying to get device image: " + message);

				// No image was selected, start capturing again
				StartCapturing();
			}
			else if (!string.IsNullOrEmpty(message))
			{
				// Load the image into a Texture2D
				Texture2D texture = new Texture2D(1, 1, TextureFormat.RGB24, false);

				texture.LoadImage(System.IO.File.ReadAllBytes(message));

				texture.Apply();

				textureSize = Mathf.Min(texture.width, texture.height);

				int blockX = (texture.width - textureSize) / 2;
				int blockY = (texture.height - textureSize) / 2;

				// Get only the middle of the image because the texture must be a square
				textureColors = texture.GetPixels(blockX, blockY, textureSize, textureSize);

				// Set the state to previewing
				SetState(State.Previewing);

				// Need to update the display texture
				updateDisplayTexture = true;
			}
			else
			{
				// No image was selected, start capturing again
				StartCapturing();
			}
		}

		#endregion
	}
}
