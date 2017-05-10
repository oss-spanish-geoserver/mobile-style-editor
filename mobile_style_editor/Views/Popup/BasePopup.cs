﻿using System;
using Xamarin.Forms;

namespace mobile_style_editor
{
	public class BasePopup : BaseView
	{
		public EventHandler<EventArgs> Click;

		public BasePopupContent Content { get; protected set; }

		public BasePopup()
		{
			BackgroundColor = Colors.TransparentGray;

			TapGestureRecognizer recognizer = new TapGestureRecognizer();
			recognizer.Tapped += delegate
			{
				if (Click != null)
				{
					Click(this, EventArgs.Empty);
				}
			};

			GestureRecognizers.Add(recognizer);

			Click += OnBackgroundClick;

			Hide(false);
		}

		void OnBackgroundClick(object sender, EventArgs e)
		{
			Hide();
		}

		public void Show(bool animated = true)
		{
			IsVisible = true;

			if (!animated)
			{
				Opacity = 1;
			}
			else
			{
				this.FadeTo(1);
			}
		}

		public async void Hide(bool animated = true)
		{
			if (!animated)
			{
				Opacity = 0;
			}
			else
			{
				await this.FadeTo(0);
			}

			IsVisible = false;
		}
	}
}
