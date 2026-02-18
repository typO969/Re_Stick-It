using System;
using System.Globalization;
using System.Windows;

namespace StickIt
{
	public partial class DesktopAreaWindow : Window
	{
		public DesktopAreaSelection? SelectedArea { get; private set; }

		public DesktopAreaWindow(double? left, double? top, double? width, double? height)
		{
			InitializeComponent();

			LeftBox.Text = Format(left);
			TopBox.Text = Format(top);
			WidthBox.Text = Format(width);
			HeightBox.Text = Format(height);
		}

		private static string Format(double? value) => value?.ToString("0", CultureInfo.InvariantCulture) ?? string.Empty;

		private void UseVirtualDesktop_Click(object sender, RoutedEventArgs e)
		{
			LeftBox.Text = SystemParameters.VirtualScreenLeft.ToString("0", CultureInfo.InvariantCulture);
			TopBox.Text = SystemParameters.VirtualScreenTop.ToString("0", CultureInfo.InvariantCulture);
			WidthBox.Text = SystemParameters.VirtualScreenWidth.ToString("0", CultureInfo.InvariantCulture);
			HeightBox.Text = SystemParameters.VirtualScreenHeight.ToString("0", CultureInfo.InvariantCulture);
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			if (!TryParse(LeftBox.Text, out var left) ||
				!TryParse(TopBox.Text, out var top) ||
				!TryParse(WidthBox.Text, out var width) ||
				!TryParse(HeightBox.Text, out var height))
			{
				System.Windows.MessageBox.Show("Enter numeric values for the desktop area.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (width <= 0 || height <= 0)
			{
				System.Windows.MessageBox.Show("Width and height must be greater than zero.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			SelectedArea = new DesktopAreaSelection(left, top, width, height);
			DialogResult = true;
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private static bool TryParse(string text, out double value)
		{
			return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}
	}

	public sealed class DesktopAreaSelection
	{
		public DesktopAreaSelection(double left, double top, double width, double height)
		{
			Left = left;
			Top = top;
			Width = width;
			Height = height;
		}

		public double Left { get; }
		public double Top { get; }
		public double Width { get; }
		public double Height { get; }
	}
}
