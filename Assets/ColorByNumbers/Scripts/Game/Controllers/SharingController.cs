using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BizzyBeeGames.ColorByNumbers
{
	public class SharingController : SingletonComponent<SharingController>
	{
		#region Inspector Variables

		[Tooltip("The max size of the image to share.")]
		[SerializeField] private int shareImageSize = 1080;

		[Tooltip("Image name to use when saving an image to Androids photo gallery.")]
		[SerializeField] private string androidGallaryImageName;

		[Tooltip("Image description to use when saving an image to Androids photo gallery.")]
		[SerializeField] private string androidGallaryImageDescription;

		#endregion

		#region Member Variables

		// The permission description that will appear on iOS if the user selects the share other button then clicks the Save Image option.
		private const string LibraryUsageDescription = "Save completed images to the device.";

		private Texture2D			saveToPhotosTexture;
		private System.Action<bool>	saveToPhotosCallback;

		#endregion

		#region Public Variables

		public bool ShareToTwitter(Texture2D imageTexture)
		{
			string imagePath = SaveImageForSharing(imageTexture);

			return NativePlugin.TryShareToTwitter(imagePath);
		}

		public bool ShareToInstagram(Texture2D imageTexture)
		{
			string imagePath = SaveImageForSharing(imageTexture);

			return NativePlugin.TryShareToInstagram(imagePath);
		}

		public void ShareToOther(Texture2D imageTexture)
		{
			string imagePath = SaveImageForSharing(imageTexture);

			NativePlugin.ShareToOther(imagePath);
		}

		public void SaveImageToPhotos(Texture2D imageTexture, System.Action<bool> callback)
		{
			if (NativePlugin.HasPhotosPermission())
			{
				string imagePath = SaveImageForSharing(imageTexture);

				NativePlugin.SaveImageToPhotos(imagePath, androidGallaryImageName, androidGallaryImageDescription);

				callback(true);
			}
			#if UNITY_ANDROID
			else
			{
				// Android asks for permissions at the beginning of the application, cant ask for permission again
				callback(false);
			}
			#elif UNITY_IOS
			else
			{
				saveToPhotosTexture		= imageTexture;
				saveToPhotosCallback	= callback;

				NativePlugin.RequestPhotosPermission(gameObject.name, "OnPhotosPermissionGranted");
			}
			#endif
		}

		#endregion

		#region Private Variables

		/// <summary>
		/// Saves the image for sharing
		/// </summary>
		private string SaveImageForSharing(Texture2D imageTexture)
		{
			// Create teh share texture with is just the imageTexture scaled up
			Texture2D shareTexture = CreateTextureToShare(imageTexture);

			string imagesDirectory	= string.Format("{0}/images", Application.persistentDataPath);
			string imagePath		= string.Format("{0}/share_image.png", imagesDirectory);

			if (!System.IO.Directory.Exists(imagesDirectory))
			{
				System.IO.Directory.CreateDirectory(imagesDirectory);
			}

			// Save teh texture to the device so another application can read it
			System.IO.File.WriteAllBytes(imagePath, shareTexture.EncodeToPNG());

			return imagePath;
		}

		/// <summary>
		/// Increases the size of the texture to share so it is not blurry when opened by another application
		/// </summary>
		private Texture2D CreateTextureToShare(Texture2D imageTexture)
		{
			int pixelScale = Mathf.FloorToInt((float)shareImageSize / (float)Mathf.Max(imageTexture.width, imageTexture.height));

			int shareTextureWidth	= pixelScale * imageTexture.width;
			int shareTextureHeight	= pixelScale * imageTexture.height;

			Texture2D shareTexture = new Texture2D(shareTextureWidth, shareTextureHeight, TextureFormat.ARGB32, false);

			for (int y = 0; y < imageTexture.height; y++)
			{
				for (int x = 0; x < imageTexture.width; x++)
				{
					SetShareTexturePixels(shareTexture, imageTexture.GetPixel(x, y), x * pixelScale, y * pixelScale, pixelScale);
				}
			}

			shareTexture.Apply();

			return shareTexture; 
		}

		/// <summary>
		/// Sets a block of pixels for the share texture
		/// </summary>
		private void SetShareTexturePixels(Texture2D shareTexture, Color color, int xStart, int yStart, int blockSize)
		{
			if (color.a == 0f)
			{
				color = Color.white;
			}

			for (int y = 0; y < blockSize; y++)
			{
				for (int x = 0; x < blockSize; x++)
				{
					shareTexture.SetPixel(xStart + x, yStart + y, color);
				}
			}
		}

		/// <summary>
		/// Invoked when an iOS device grants permission to use the photos library
		/// </summary>
		private void OnPhotosPermissionGranted(string message)
		{
			if (message == "true")
			{
				// Call the method again knowning we have now permission
				SaveImageToPhotos(saveToPhotosTexture, saveToPhotosCallback);
			}
			else
			{
				// Notify callback that permission was denied
				saveToPhotosCallback(false);
			}

			saveToPhotosTexture		= null;
			saveToPhotosCallback	= null;
		}

		#if UNITY_EDITOR && UNITY_IOS
		/// <summary>
		/// Adds some fields to the Info.plist file for iOS builds so that sharing works properly.
		/// </summary>
		[UnityEditor.Callbacks.PostProcessBuild]
		public static void ChangeXcodePlist(UnityEditor.BuildTarget buildTarget, string pathToBuiltProject)
		{
			if (buildTarget == UnityEditor.BuildTarget.iOS)
			{
				string								plistPath	= pathToBuiltProject + "/Info.plist";
				UnityEditor.iOS.Xcode.PlistDocument	plist		= new UnityEditor.iOS.Xcode.PlistDocument();

				plist.ReadFromString(System.IO.File.ReadAllText(plistPath));

				UnityEditor.iOS.Xcode.PlistElementDict rootDict = plist.root;

				// Add the library description, this is so the app can use the "Save Image" feature on the share other view
				rootDict.SetString("NSPhotoLibraryUsageDescription", LibraryUsageDescription);
				rootDict.SetString("NSPhotoLibraryAddUsageDescription", LibraryUsageDescription);

				// Add the instagram app to the queries array
				UnityEditor.iOS.Xcode.PlistElementArray queriesArray = rootDict.CreateArray("LSApplicationQueriesSchemes");

				queriesArray.AddString("twitter");
				queriesArray.AddString("instagram");

				// Write to file
				System.IO.File.WriteAllText(plistPath, plist.WriteToString());
			}
		}
		#endif

		#endregion
	}
}
