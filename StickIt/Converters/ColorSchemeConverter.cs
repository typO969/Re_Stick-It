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

	public readonly record struct RgbVector(double R, double G, double B)
	{
		public System.Windows.Media.Color toColor() => System.Windows.Media.Color.FromRgb(
			 (byte) Math.Clamp((int) Math.Round(R), 0, 255),
				(byte) Math.Clamp((int) Math.Round(G), 0, 255),
				(byte) Math.Clamp((int) Math.Round(B), 0, 255)
			);
	}

	internal sealed class ColorSchemeResult(
			 string sanitizedName,
			 IReadOnlyDictionary<ColorComponent, RgbVector> components,
			 IReadOnlyDictionary<string, RgbVector> namedVectors)
	{
		public string SanitizedName { get; } = sanitizedName;
		public IReadOnlyDictionary<ColorComponent, RgbVector> Components { get; } = components;
		public IReadOnlyDictionary<string, RgbVector> NamedVectors { get; } = namedVectors;

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
			var controlBar = Subtract(background, (13, 13, 13));
			var buttons = Subtract(background, (50, 50, 50));
			var highlights = Add(background, (30, 100, 25));
			var outlines = Halve(background);
			var text = Subtract(Halve(buttons), (5, 5, 3));			
			text = EnsureReadableInk(text, background);

			//var text = ReduceByApproximateTwoThirds(outlines);

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
			double Apply(double channel)
			{
				var reduction = Math.Floor(channel * TextReductionFactor);
				return ClampChannel((int) (channel - reduction));
			}

			return new RgbVector(
				 Apply(source.R),
				 Apply(source.G),
				 Apply(source.B)
			);
		}


		private static RgbVector EnsureReadableInk(RgbVector darkInk, RgbVector bg)
		{
			// Dark background → derive light ink from the paper itself
			if (RelLuma(bg) < 0.38)
			{
				// Lift background toward white (not all the way)
				var lifted = Add(bg, (130, 130, 130));

				// Desaturate slightly so it reads as ink, not glow
				lifted = Desaturate(lifted, 0.25);

				return Clamp(lifted);
			}

			// Light background → dark ink
			return darkInk;
		}
		private static RgbVector Desaturate(RgbVector c, double amount)
		{
			// amount: 0 = no change, 1 = full grayscale
			double l = 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;
			return new RgbVector(
				 c.R + (l - c.R) * amount,
				 c.G + (l - c.G) * amount,
				 c.B + (l - c.B) * amount
			);
		}
		private static RgbVector Clamp(RgbVector v) =>
			 new RgbVector(
				  Math.Clamp(v.R, 0, 255.0),
				  Math.Clamp(v.G, 0, 255.0),
				  Math.Clamp(v.B, 0, 255.0)
			 );



		private static RgbVector EnsureMinLumaDelta(RgbVector text, RgbVector bg, double minDelta)
		{
			double Lb = RelLuma(bg);

			var t = text;
			for (int i = 0; i < 16; i++)
			{
				if (Math.Abs(RelLuma(t) - Lb) >= minDelta)
					return t;

				t = Subtract(t, (12, 12, 10)); // step darker
			}

			return t;
		}

		private static double RelLuma(RgbVector v)
		{
			// v components are expected 0..255
			double Rs = v.R / 255.0, Gs = v.G / 255.0, Bs = v.B / 255.0;

			double R = Rs <= 0.04045 ? Rs / 12.92 : Math.Pow((Rs + 0.055) / 1.055, 2.4);
			double G = Gs <= 0.04045 ? Gs / 12.92 : Math.Pow((Gs + 0.055) / 1.055, 2.4);
			double B = Bs <= 0.04045 ? Bs / 12.92 : Math.Pow((Bs + 0.055) / 1.055, 2.4);

			return 0.2126 * R + 0.7152 * G + 0.0722 * B;
		}


		private static RgbVector Halve(RgbVector source)
		{
			double Apply(double channel)
			{
				var halved = (int) Math.Round(channel / 2d, MidpointRounding.AwayFromZero);
				return ClampChannel(halved);
			}

			return new RgbVector(Apply(source.R), Apply(source.G), Apply(source.B));
		}

		private static RgbVector Add(RgbVector source, (int R, int G, int B) delta)
		{
			return new RgbVector(
				 ClampChannel((int) (source.R + delta.R)),
				 ClampChannel((int) (source.G + delta.G)),
				 ClampChannel((int) (source.B + delta.B)));
		}

		private static RgbVector Subtract(RgbVector source, (int R, int G, int B) delta)
		{
			return new RgbVector(
				 ClampChannel((int) (source.R - delta.R)),
				 ClampChannel((int) (source.G - delta.G)),
				 ClampChannel((int) (source.B - delta.B)));
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

			byte ParseComponent(int start) => byte.Parse(value.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

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