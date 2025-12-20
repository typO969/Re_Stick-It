using System;
using System.Globalization;
using System.Windows.Data;

using StickIt.Services;

namespace StickIt.Converters
{
	// value: NoteColors.NoteColor (ColorKey)
	// parameter: component name (e.g., "Buttons", "Highlights", "Text", ...)
	public sealed class NoteComponentHexConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not NoteColors.NoteColor key)
				return "";

			var baseHex = NoteColors.Hex[key];

			var compName = parameter as string ?? "Background";
			if (!Enum.TryParse(compName, out ColorComponent component))
				component = ColorComponent.Background;

			var c = ColorSchemeConverter.GetColor(key.ToString(), baseHex, component);

			return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotSupportedException();
	}
}
