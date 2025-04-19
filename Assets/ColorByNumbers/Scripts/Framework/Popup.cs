using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace BizzyBeeGames.ColorByNumbers
{
	public class Popup : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private float			backgroundAlpha		= 0.35f;
		[SerializeField] private float			animationDuration	= 350f;
		[SerializeField] private Image			background			= null;
		[SerializeField] private RectTransform	uiContainer			= null;

		#endregion

		#region Member Variables

		private bool		isShowing;
		private PopupClosed	callback;

		#endregion

		#region Delegates

		public delegate void PopupClosed(bool cancelled, object[] outData);

		#endregion

		#region Public Methods

		public void Show()
		{
			Show(null, null);
		}

		public void Show(object[] inData, PopupClosed callback)
		{
			this.callback = callback;

			if (isShowing)
			{
				return;
			}

			isShowing = true;

			gameObject.SetActive(true);

			Color fromColor	= background.color;
			Color toColor	= background.color;

			fromColor.a	= 0f;
			toColor.a	= backgroundAlpha ;

			background.color		= fromColor;
			uiContainer.localScale	= Vector3.zero;

			Tween.Colour(background, Tween.TweenStyle.EaseOut, fromColor, toColor, animationDuration);

			Tween.ScaleX(uiContainer, Tween.TweenStyle.EaseOut, 0f, 1f, animationDuration);
			Tween.ScaleY(uiContainer, Tween.TweenStyle.EaseOut, 0f, 1f, animationDuration);

			OnShowing(inData);
		}

		public void Hide(bool cancelled)
		{
			Hide(cancelled, null);
		}

		public void Hide(bool cancelled, object[] outData)
		{
			if (!isShowing)
			{
				return;
			}

			isShowing = false;

			Color fromColor	= background.color;
			Color toColor	= background.color;

			fromColor.a	= backgroundAlpha;
			toColor.a	= 0f;

			background.color		= fromColor;
			uiContainer.localScale	= Vector3.one;

			Tween.Colour(background, Tween.TweenStyle.EaseOut, fromColor, toColor, animationDuration);

			Tween.ScaleX(uiContainer, Tween.TweenStyle.EaseOut, 1f, 0f, animationDuration);
			Tween.ScaleY(uiContainer, Tween.TweenStyle.EaseOut, 1f, 0f, animationDuration).SetFinishCallback((GameObject tweenedObject) => 
			{
				gameObject.SetActive(false);
			});

			if (callback != null)
			{
				callback(cancelled, outData);
			}
		}

		public virtual void OnShowing(object[] inData)
		{

		}

		#endregion
	}
}