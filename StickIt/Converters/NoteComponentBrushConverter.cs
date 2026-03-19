using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using StickIt.Models;
using StickIt.Services;

namespace StickIt.Converters
{
	public sealed class NoteComponentBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (parameter is not string p || !Enum.TryParse(p, out ColorComponent component))
				component = ColorComponent.Background;

			if (value is NoteModel model)
			{
				var skin = (System.Windows.Application.Current as StickIt.App)?.Skins.ResolveOrFallback(model.SkinId, model.ColorKey);
				if (skin != null)
				{
					try
					{
						var hex = SkinService.GetComponentHex(skin, component);
						return (SolidColorBrush) (new BrushConverter().ConvertFromString(hex)!);
					}
					catch
					{
                  return System.Windows.Media.Brushes.Transparent;
					}
				}
			}

			if (value is NoteColors.NoteColor key)
			{
				var baseHex = NoteColors.Hex[key];
				var c = ColorSchemeConverter.GetColor(key.ToString(), baseHex, component);
				return new SolidColorBrush(c);
			}

         return System.Windows.Media.Brushes.Transparent;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
			throw new NotSupportedException();
	}
}
