﻿using System;
using Xamarin.Forms;
using System.Collections.Generic;

namespace mobile_style_editor
{
	public class BaseScrollView : ScrollView
	{
		BaseView content;

		public IList<View> Children { get { return content.Children; } }

		Constraint ZeroConstraint
		{
			get { return GetConstraint(0); }
		}

		public BaseScrollView()
		{
			content = new BaseView();
			content.ClearChildrenOnLayout = false;
			Content = content;

			SizeChanged += OnSizeChanged;
		}

		void OnSizeChanged(object sender, EventArgs e)
		{
			LayoutSubviews();
		}

		public void AddSubview(View view)
		{
			content.AddSubview(view);
			//content.Children.Add(view, ZeroConstraint, ZeroConstraint, ZeroConstraint, ZeroConstraint);
		}

		public void AddSubview(View view, double x, double y, double w, double h)
		{
			content.AddSubview(view, x, y, w, h);
			//content.Children.Add(view, GetConstraint(x), GetConstraint(y), GetConstraint(w), GetConstraint(h));
		}

		Constraint GetConstraint(double number)
		{
			return Constraint.Constant(number);
		}

		public void RemoveChild(View view)
		{
			content.Children.Remove(view);
		}

		public virtual void LayoutSubviews()
		{
			if (Loader != null && Loader.IsRunning)
			{
				RemoveChild(Loader);
				Loader = null;
				ShowLoading();
			}
		}

		public ActivityIndicator Loader { get; private set; }

		public void ShowLoading()
		{
			if (Loader == null)
			{
				Loader = new ActivityIndicator();
				double size = 50;
				AddSubview(Loader, Width / 2 - size / 2, Height / 2 - size / 2, size, size);
			}

			Loader.IsRunning = true;
		}

		public void HideLoading()
		{
			if (Loader != null)
			{
				Loader.IsRunning = false;
			}
		}

		Label toast;
		System.Threading.Timer timer;
		public void Toast(string text)
		{
			if (toast == null)
			{
				toast = new Label();
				toast.VerticalTextAlignment = TextAlignment.Center;
				toast.HorizontalTextAlignment = TextAlignment.Center;
				toast.BackgroundColor = Color.FromRgba(0, 0, 0, 190);
				toast.TextColor = Color.White;

				double padding = 30;
				double w = 300;
				double h = 35;
				double x = Width / 2 - w / 2;
				double y = Height - (h + padding);

				AddSubview(toast, x, y, w, h);
			}
			toast.Text = text;
			toast.FadeTo(1.0);

			if (timer != null)
			{
				timer.Dispose();
				timer = null;
			}

			timer = new System.Threading.Timer((object state) =>
			{
				Device.BeginInvokeOnMainThread(delegate
				{
					toast.FadeTo(0.0);
					timer = null;
				});
			}, null, 500, System.Threading.Timeout.Infinite);
		}

		#region Device Info
		public double Ratio { get { return Height > Width ? Height / Width : Width / Height; } }

		public bool IsTablet
		{
			get
			{
#if __IOS__
				return UIKit.UIDevice.CurrentDevice.Model.Contains("iPad");
#else
				/*
				 * TODO Pulled these constants out of my ass. May need to tweak in the fut				 
				 */
				Console.WriteLine("Ratio: " + Ratio);
				if (Height > Width)
				{
					Console.WriteLine("H > W");
					return Ratio > 1.5;
				}

				Console.WriteLine("W > H");
				return Ratio > 1.7;
#endif
			}
		}
	}
		#endregion
}
