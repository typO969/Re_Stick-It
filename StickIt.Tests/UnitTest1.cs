using System;
using System.Collections.Generic;
using System.Linq;

using StickIt;
using StickIt.Converters;
using StickIt.Services;

namespace StickIt.Tests
{
   public class UnitTest1
   {
      [Fact]
      public void NoteColors_EveryEnumValueHasHexEntry()
      {
         var allColors = Enum.GetValues<NoteColors.NoteColor>();
         foreach (var color in allColors)
            Assert.True(NoteColors.Hex.ContainsKey(color), $"Missing hex for {color}");
      }

      [Fact]
      public void NoteColors_AllHexValues_AreValidRgbHex()
      {
         foreach (var pair in NoteColors.Hex)
         {
            var hex = pair.Value;
            Assert.Matches("^#[0-9a-fA-F]{6}$", hex);
         }
      }

      [Fact]
      public void NoteColors_ContainsDefaultThreeMYellowHex()
      {
         Assert.True(NoteColors.Hex.TryGetValue(NoteColors.NoteColor.ThreeMYellow, out var hex));
         Assert.Equal("#f7e03d", hex);
      }

      [Fact]
      public void ColorSchemeConverter_GetColor_IsDeterministic()
      {
         var c1 = ColorSchemeConverter.GetColor("ThreeMYellow", "#f7e03d", ColorComponent.Text);
         var c2 = ColorSchemeConverter.GetColor("ThreeMYellow", "#f7e03d", ColorComponent.Text);

         Assert.Equal(c1, c2);
      }

      [Fact]
      public void ColorSchemeConverter_TextOnDarkBackground_IsLighterThanBackground()
      {
         var background = ColorSchemeConverter.GetColor("Night", "#101018", ColorComponent.Background);
         var text = ColorSchemeConverter.GetColor("Night", "#101018", ColorComponent.Text);

         Assert.True(GetPerceivedLuminance(text) > GetPerceivedLuminance(background));
      }

      [Fact]
      public void ColorSchemeConverter_ReturnsValidColor_ForAllComponents()
      {
         var components = Enum.GetValues<ColorComponent>();
         foreach (var component in components)
         {
            var c = ColorSchemeConverter.GetColor("ThreeMYellow", "#f7e03d", component);
            Assert.InRange(c.R, (byte)0, (byte)255);
            Assert.InRange(c.G, (byte)0, (byte)255);
            Assert.InRange(c.B, (byte)0, (byte)255);
         }
      }

      [Fact]
      public void ColorSchemeConverter_InvalidHex_ThrowsFormatException()
      {
         Assert.Throws<FormatException>(() =>
            ColorSchemeConverter.GetColor("Bad", "#12345", ColorComponent.Text));
      }

      [Fact]
      public void FontColorDefaults_IncludeOneDefault3MEntryPerNoteColor()
      {
         var defaults = ColorItem.Defaults;
         var dynamicCount = defaults.Count(c => c.Name.StartsWith("Default 3M ", StringComparison.Ordinal));

         Assert.Equal(Enum.GetValues<NoteColors.NoteColor>().Length, dynamicCount);
         Assert.Contains(defaults, c => c.Name == "Default 3M Three M Yellow text");
      }

      [Fact]
      public void FontColorDefaults_HaveUniqueNames()
      {
         var defaults = ColorItem.Defaults;
         var dupes = defaults
            .GroupBy(c => c.Name, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

         Assert.Empty(dupes);
      }

      private static double GetPerceivedLuminance(System.Windows.Media.Color c)
      {
         return (0.2126 * c.R) + (0.7152 * c.G) + (0.0722 * c.B);
      }
   }
}
