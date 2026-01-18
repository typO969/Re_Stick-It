using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using StickIt.Services;

namespace StickIt.Converters
{
	public sealed class ColorKeyToBackgroundBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not string keyName) return System.Windows.Media.Brushes.Transparent;

			if (!Enum.TryParse(keyName, out NoteColors.NoteColor key))
				return System.Windows.Media.Brushes.Transparent;

			var baseHex = NoteColors.Hex[key];
			var c = ColorSchemeConverter.GetColor(key.ToString(), baseHex, ColorComponent.Background);
			return new SolidColorBrush(c);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			 throw new NotSupportedException();
	}
}
