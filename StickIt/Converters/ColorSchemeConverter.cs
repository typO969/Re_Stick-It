using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media;

using StickIt.Models;
using StickIt.Services;
using StickIt;

namespace StickIt.Converters
{
	public enum ColorComponent
	{
		Background,
		ControlBar,
		Buttons,
		Highlights,
		Outlines,
		Text
	}

	public readonly record struct RgbVector(byte R, byte G, byte B)
	{
		public System.Windows.Media.Color toColor() => System.Windows.Media.Color.FromRgb(R, G, B);
	}

	internal sealed class ColorSchemeResult
	{
		public string SanitizedName { get; }
		public IReadOnlyDictionary<ColorComponent, RgbVector> Components { get; }
		public IReadOnlyDictionary<string, RgbVector> NamedVectors { get; }

		public ColorSchemeResult(
			 string sanitizedName,
			 IReadOnlyDictionary<ColorComponent, RgbVector> components,
			 IReadOnlyDictionary<string, RgbVector> namedVectors)
		{
			SanitizedName = sanitizedName;
			Components = components;
			NamedVectors = namedVectors;
		}

		public RgbVector GetComponent(ColorComponent component) => Components[component];
	}

	public static class ColorSchemeConverter
	{
		private const double TextReductionFactor = 0.66666d; // Aligns with spec example (2/3 approximation)
		private static readonly ConcurrentDictionary<string, ColorSchemeResult> Cache = new(StringComparer.OrdinalIgnoreCase);

		public static System.Windows.Media.Color GetColor(string colorName, string baseColorHex, ColorComponent component)
		{
			var scheme = Cache.GetOrAdd(BuildCacheKey(colorName, baseColorHex), _ => GenerateScheme(colorName, baseColorHex));
			return scheme.GetComponent(component).toColor();
		}

		public static IReadOnlyDictionary<string, RgbVector> GetRgbVectors(string colorName, string baseColorHex)
		{
			var scheme = Cache.GetOrAdd(BuildCacheKey(colorName, baseColorHex), _ => GenerateScheme(colorName, baseColorHex));
			return scheme.NamedVectors;
		}

		private static ColorSchemeResult GenerateScheme(string colorName, string baseColorHex)
		{
			var sanitizedName = SanitizeColorName(colorName);
			var background = ParseHex(baseColorHex);
			var controlBar = Subtract(background, (25, 25, 25));
			var buttons = Subtract(controlBar, (20, 20, 20));
			var highlights = Add(background, (30, 100, 25));
			var outlines = Halve(buttons);
			var text = ReduceByApproximateTwoThirds(outlines);

			var components = new Dictionary<ColorComponent, RgbVector>
			{
				[ColorComponent.Background] = background,
				[ColorComponent.ControlBar] = controlBar,
				[ColorComponent.Buttons] = buttons,
				[ColorComponent.Highlights] = highlights,
				[ColorComponent.Outlines] = outlines,
				[ColorComponent.Text] = text
			};

			var namedVectors = new Dictionary<string, RgbVector>(StringComparer.OrdinalIgnoreCase)
			{
				[$"bg{sanitizedName}Rgb"] = background,
				[$"cb{sanitizedName}Rgb"] = controlBar,
				[$"but{sanitizedName}Rgb"] = buttons,
				[$"hl{sanitizedName}Rgb"] = highlights,
				[$"ol{sanitizedName}Rgb"] = outlines,
				[$"txt{sanitizedName}Rgb"] = text
			};

			return new ColorSchemeResult(sanitizedName, components, namedVectors);
		}

		private static RgbVector ReduceByApproximateTwoThirds(RgbVector source)
		{
			byte Apply(byte channel)
			{
				var reduction = (int) Math.Floor(channel * TextReductionFactor);
				return ClampChannel(channel - reduction);
			}

			return new RgbVector(Apply(source.R), Apply(source.G), Apply(source.B));
		}

		private static RgbVector Halve(RgbVector source)
		{
			byte Apply(byte channel)
			{
				var halved = (int) Math.Round(channel / 2d, MidpointRounding.AwayFromZero);
				return ClampChannel(halved);
			}

			return new RgbVector(Apply(source.R), Apply(source.G), Apply(source.B));
		}

		private static RgbVector Add(RgbVector source, (int R, int G, int B) delta)
		{
			return new RgbVector(
				 ClampChannel(source.R + delta.R),
				 ClampChannel(source.G + delta.G),
				 ClampChannel(source.B + delta.B));
		}

		private static RgbVector Subtract(RgbVector source, (int R, int G, int B) delta)
		{
			return new RgbVector(
				 ClampChannel(source.R - delta.R),
				 ClampChannel(source.G - delta.G),
				 ClampChannel(source.B - delta.B));
		}

		private static RgbVector ParseHex(string hex)
		{
			if (string.IsNullOrWhiteSpace(hex))
			{
				throw new ArgumentException("Base color hex value is required.", nameof(hex));
			}

			var value = hex.Trim();
			if (value.StartsWith('#'))
			{
				value = value[1..];
			}

			if (value.Length != 6)
			{
				throw new FormatException($"Hex value '{hex}' must contain exactly 6 characters.");
			}

			byte ParseComponent(int start) => byte.Parse(value.Substring(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

			return new RgbVector(ParseComponent(0), ParseComponent(2), ParseComponent(4));
		}

		private static byte ClampChannel(int value)
		{
			if (value < 0)
			{
				return 0;
			}

			if (value > 255)
			{
				return 255;
			}

			return (byte) value;
		}

		private static string BuildCacheKey(string colorName, string baseColorHex)
		{
			return $"{SanitizeColorName(colorName)}|{baseColorHex.Trim().ToUpperInvariant()}";
		}

		private static string SanitizeColorName(string colorName)
		{
			if (string.IsNullOrWhiteSpace(colorName))
			{
				return "Color";
			}

			var builder = new StringBuilder(colorName.Length);
			var nextUpper = true;

			foreach (var ch in colorName)
			{
				if (char.IsLetter(ch))
				{
					builder.Append(nextUpper ? char.ToUpperInvariant(ch) : char.ToLowerInvariant(ch));
					nextUpper = false;
				}
				else if (char.IsDigit(ch))
				{
					builder.Append(ch);
					nextUpper = true;
				}
				else
				{
					nextUpper = true;
				}
			}

			return builder.Length == 0 ? "Color" : builder.ToString();
		}
	}


	[MarkupExtensionReturnType(typeof(System.Windows.Media.Color))]
	public sealed class ColorSchemeExtension : MarkupExtension
	{
		public string ColorName { get; set; } = string.Empty;
		public string BaseColor { get; set; } = string.Empty;
		public ColorComponent Component { get; set; } = ColorComponent.Background;

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (string.IsNullOrWhiteSpace(BaseColor))
			{
				throw new InvalidOperationException("BaseColor must be provided.");
			}

			var effectiveName = string.IsNullOrWhiteSpace(ColorName) ? BaseColor : ColorName;
			return ColorSchemeConverter.GetColor(effectiveName, BaseColor, Component);
		}
	}
}