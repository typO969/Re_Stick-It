using System;
using System.Windows;
using System.Windows.Controls;

namespace StickIt
{
	public partial class DesktopAreaPickerWindow : Window
	{
		private System.Windows.Point? _dragStart;
		private System.Windows.Rect _currentRect;

		public DesktopAreaSelection? SelectedArea { get; private set; }

		public DesktopAreaPickerWindow()
		{
			InitializeComponent();

			Left = SystemParameters.VirtualScreenLeft;
			Top = SystemParameters.VirtualScreenTop;
			Width = SystemParameters.VirtualScreenWidth;
			Height = SystemParameters.VirtualScreenHeight;

			KeyDown += DesktopAreaPickerWindow_KeyDown;
		}

		private void DesktopAreaPickerWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Escape)
			{
				DialogResult = false;
				Close();
			}
		}

		private void Canvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			SelectionCanvas.CaptureMouse();
			_dragStart = e.GetPosition(SelectionCanvas);
			UpdateSelection(_dragStart.Value, _dragStart.Value);
			SelectionRect.Visibility = Visibility.Visible;
		}

		private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (!_dragStart.HasValue || !SelectionCanvas.IsMouseCaptured)
				return;

			var current = e.GetPosition(SelectionCanvas);
			UpdateSelection(_dragStart.Value, current);
		}

		private void Canvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (!_dragStart.HasValue)
				return;

			SelectionCanvas.ReleaseMouseCapture();
			var end = e.GetPosition(SelectionCanvas);
			UpdateSelection(_dragStart.Value, end);

			var screenTopLeft = PointToScreen(new System.Windows.Point(_currentRect.Left, _currentRect.Top));
			SelectedArea = new DesktopAreaSelection(screenTopLeft.X, screenTopLeft.Y, _currentRect.Width, _currentRect.Height);

			DialogResult = true;
			Close();
		}

		private void UpdateSelection(System.Windows.Point start, System.Windows.Point end)
		{
			double x = Math.Min(start.X, end.X);
			double y = Math.Min(start.Y, end.Y);
			double width = Math.Abs(end.X - start.X);
			double height = Math.Abs(end.Y - start.Y);

			_currentRect = new System.Windows.Rect(x, y, Math.Max(1, width), Math.Max(1, height));
			System.Windows.Controls.Canvas.SetLeft(SelectionRect, _currentRect.Left);
			System.Windows.Controls.Canvas.SetTop(SelectionRect, _currentRect.Top);
			SelectionRect.Width = _currentRect.Width;
			SelectionRect.Height = _currentRect.Height;
		}
	}
}
