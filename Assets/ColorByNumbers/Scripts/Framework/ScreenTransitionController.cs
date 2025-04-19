using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BizzyBeeGames.ColorByNumbers
{
	public class ScreenTransitionController : SingletonComponent<ScreenTransitionController>
	{
		#region Inspector Variables

		[Tooltip("The duration in milliseconds for the screen transition animation to complete.")]
		[SerializeField] private float animationSpeed = 350;

		[Tooltip("The list of Screen components that are used in the game.")]
		[SerializeField] private List<Screen> screens = null;

		#endregion

		#region Member Variables

		// The Screen Ids currently used in the game
		public const string MainScreenId = "main";
		public const string GameScreenId = "game";

		// The screen that is currently being shown
		private Screen	currentScreen;
		private bool	isAnimating;

		#endregion

		#region Properties

		public float	AnimationSpeed	{ get { return animationSpeed; } }
		public string	CurrentScreenId	{ get { return currentScreen == null ? "" : currentScreen.id; } }

		#endregion

		#region Properties

		/// <summary>
		/// Invoked when the ScreenTransitionController is transitioning from one screen to another. The first argument is the current showing screen id, the
		/// second argument is the screen id of the screen that is about to show (null if its the first screen). The third argument id true if the screen
		/// that is being show is an overlay
		/// </summary>
		public System.Action<string, string> OnSwitchingScreens;

		/// <summary>
		/// Invoked when ShowScreen is called
		/// </summary>
		public System.Action<string> OnShowingScreen;

		#endregion

		#region Unity Methods

		private void Start()
		{
			// Initialize and hide all the screens
			for (int i = 0; i < screens.Count; i++)
			{
				screens[i].Initialize();
				screens[i].gameObject.SetActive(true);

				HideScreen(screens[i].RectT, false, false, null);
			}

			// Show the main screen when the app starts up
			Show(MainScreenId, false, false);
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Shows the screen with the specified id.
		/// </summary>
		/// <param name="id">Id of Screen to be shown.</param>
		/// <param name="back">If set to true back then the screens will animateleft to right on the screen, if false they animate right to left.</param>
		/// <param name="animate">If set to true animate the screens will animate, if false the screens will snap into place.</param>
		/// <param name="overlay">If set to true then the current screen will not hide.</param>
		/// <param name="onTweenFinished">Called when the screens finish animating.</param>
		public void Show(string id, bool back = false, bool animate = true, System.Action onTweenFinished = null, string data = "")
		{
			if (isAnimating || (currentScreen != null && id == currentScreen.id))
			{
				return;
			}

			Screen screen = GetScreenInfo(id);

			if (screen != null)
			{
				ShowScreen(screen, animate, back, onTweenFinished, data);

				// If its not an overlay screen then hide the current screen
				if (animate)
				{
					HideScreen(currentScreen.RectT, animate, back, null);
				}

				if (OnSwitchingScreens != null)
				{
					OnSwitchingScreens(currentScreen == null ? null : currentScreen.id, id);
				}

				currentScreen = screen;
			}
		}

		public void TransitionScreens(RectTransform fromScreen, RectTransform toScreen, bool back, bool animate = true)
		{
			if (fromScreen != null)
			{
				HideScreen(fromScreen, animate, back, null);
			}

			ShowScreen(toScreen, fromScreen != null && animate, back, null, "");
		}

		public void SetOffScreen(RectTransform screen)
		{
			screen.gameObject.SetActive(true);
			HideScreen(screen, false, false, null);
		}

		#endregion

		#region Private Methods

		private void ShowScreen(Screen screen, bool animate, bool back, System.Action onTweenFinished, string data)
		{
			if (screen == null)
			{
				return;
			}

			if (OnShowingScreen != null)
			{
				OnShowingScreen(screen.id);
			}

			screen.OnShowing(data);

			ShowScreen(screen.RectT, animate, back, onTweenFinished, data);
		}

		private void ShowScreen(RectTransform screenRect, bool animate, bool back, System.Action onTweenFinished, string data)
		{
			float direction = (back ? -1f : 1f);
			float fromX		= GetWidth(screenRect) * direction;
			float toX		= 0;

			SetIsAnimating(animate);

			TransitionScreen(screenRect, fromX, toX, animate, () =>
			{
				SetIsAnimating(false);

				if (onTweenFinished != null)
				{
					onTweenFinished();
				}
			});
		}

		private void SetIsAnimating(bool animate)
		{
			isAnimating = animate;
		}

		private void HideScreen(RectTransform screenRect, bool animate, bool back, System.Action onTweenFinished)
		{
			if (screenRect == null)
			{
				return;
			}

			float direction = (back ? 1f : -1f);
			float fromX		= 0;
			float toX		= GetWidth(screenRect) * direction;

			TransitionScreen(screenRect, fromX, toX, animate, onTweenFinished);
		}

		private void TransitionScreen(RectTransform screenRect, float fromX, float toX, bool animate, System.Action onTweenFinished)
		{
			screenRect.anchoredPosition = new Vector2(fromX, screenRect.anchoredPosition.y);

			if (animate)
			{
				Tween tween = Tween.PositionX(screenRect, Tween.TweenStyle.EaseOut, fromX, toX, animationSpeed);

				tween.SetUseRectTransform(true);

				if (onTweenFinished != null)
				{
					tween.SetFinishCallback((tweenedObject) => { onTweenFinished(); });
				}
			}
			else
			{
				screenRect.anchoredPosition = new Vector2(toX, screenRect.anchoredPosition.y);

				if (onTweenFinished != null)
				{
					onTweenFinished();
				}
			}
		}

		private Screen GetScreenInfo(string id)
		{
			for (int i = 0; i < screens.Count; i++)
			{
				if (id == screens[i].id)
				{
					return screens[i];
				}
			}

			Debug.LogError("[ScreenTransitionController] No Screen exists with the id " + id);

			return null;
		}

		private float GetWidth(RectTransform rectTransform)
		{
			Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

			return UnityEngine.Screen.width * (1f / canvas.scaleFactor);
		}

		#endregion
	}
}
