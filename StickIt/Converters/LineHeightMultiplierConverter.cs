using System;
using System.Globalization;
using System.Windows.Data;

namespace StickIt.Converters
{
   public sealed class LineHeightMultiplierConverter : IMultiValueConverter
   {
      public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
      {
         if (values.Length < 2)
            return 0d;

         if (!TryGetDouble(values[0], out var fontSize))
            return 0d;

         if (!TryGetDouble(values[1], out var multiplier))
            return fontSize;

         return fontSize * multiplier;
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
