﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace mobile_style_editor
{
	public class FileListPopup : BasePopup
	{
		public FileListPopupContent FileContent { get { return Content as FileListPopupContent; } }

		public FileListHeader Header { get; private set; }

		public FileListPopup()
		{
			Content = new FileListPopupContent();

			Header = new FileListHeader();

			Hide(false);
		}

		public override void LayoutSubviews()
		{
			double verticalPadding, horizontalPadding;

			if (Width > Height)
			{
				horizontalPadding = Width / 6;
			}
			else
			{
				horizontalPadding = Width / 15;
			}

			verticalPadding = 70;

			double x = horizontalPadding;
			double y = verticalPadding;

			double contentHeight = Height - 2 * verticalPadding;
			double contentWidth = Width - 2 * horizontalPadding;

			double w = contentWidth;
			double h = contentHeight;

			AddSubview(Content, x, y, w, h);

			double padding = 10;

			w = contentWidth;
			h = verticalPadding - 3 * padding;
			x = horizontalPadding;
			y = verticalPadding - h;

			AddSubview(Header, x, y, w, h);
		}

		public void Show(List<DriveFile> files)
		{
			Show();
			FileContent.Populate(files.ToObjects());
			Header.IsVisible = false;
		}

		public void Show(List<StoredStyle> styles)
		{
			Show();
			FileContent.Populate(styles.ToObjects());
		}

		public List<GithubFile> GithubFiles { get; private set; }

		public void Show(List<GithubFile> files)
		{
			Header.IsVisible = true;

			Show();
			GithubFiles = files;
			FileContent.Populate(files.ToObjects());

			if (files.Any(file => file.IsProjectFile))
			{
				Header.Select.Enable();
			}
			else
			{
				Header.Select.Disable();
			}
		}

	}
}
