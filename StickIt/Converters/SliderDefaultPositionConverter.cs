using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace StickIt.Converters
{
   public sealed class SliderDefaultPositionConverter : IMultiValueConverter
   {
      public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
      {
         if (values.Length < 4)
            return new Thickness(0);

         if (!TryGetDouble(values[0], out var width)
            || !TryGetDouble(values[1], out var min)
            || !TryGetDouble(values[2], out var max)
            || !TryGetDouble(values[3], out var @default))
            return new Thickness(0);

         if (max <= min)
            return new Thickness(0);

         var usable = Math.Max(0, width - 10);
         var ratio = Math.Max(0, Math.Min(1, (@default - min) / (max - min)));
         var offset = ratio * usable;
         return new Thickness(offset, 0, 0, 0);
      }

      public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
         => throw new NotSupportedException();

      private static bool TryGetDouble(object value, out double result)
      {
         if (value is double d)
         {
            result = d;
            return true;
         }

         if (value is float f)
         {
            result = f;
            return true;
         }

         if (value is int i)
         {
            result = i;
            return true;
         }

       if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
         {
            result = parsed;
            return true;
         }

       if (value is string sCurrent && double.TryParse(sCurrent, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsedCurrent))
         {
            result = parsedCurrent;
            return true;
         }

         result = 0;
         return false;
      }
   }
}
