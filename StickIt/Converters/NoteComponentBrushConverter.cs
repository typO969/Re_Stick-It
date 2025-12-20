using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using StickIt.Services;

namespace StickIt.Converters
{
	public sealed class NoteComponentBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not NoteColors.NoteColor key)
				return System.Windows.Media.Brushes.Transparent;

			var baseHex = NoteColors.Hex[key];

			if (parameter is not string p || !Enum.TryParse(p, out ColorComponent component))
				component = ColorComponent.Background;

			var c = ColorSchemeConverter.GetColor(key.ToString(), baseHex, component);
			return new SolidColorBrush(c);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotSupportedException();
	}
}
