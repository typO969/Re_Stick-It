using System;
using System.Collections.Generic;
using System.Linq;

using StickIt.Converters;
using StickIt.Models;
using StickIt.Persistence;

namespace StickIt.Services
{
	public sealed class SkinService
	{
		private readonly Dictionary<string, NoteSkin> _builtIns = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, NoteSkin> _userSkins = new(StringComparer.OrdinalIgnoreCase);

		public SkinService()
		{
			LoadBuiltIns();
		}

		public void SetUserSkins(IEnumerable<NoteSkinPersist>? skins)
		{
			_userSkins.Clear();

			if (skins == null)
				return;

			foreach (var skin in skins)
			{
				if (skin == null || string.IsNullOrWhiteSpace(skin.Id))
					continue;

				_userSkins[skin.Id] = FromPersist(skin);
			}
		}

		public IReadOnlyList<NoteSkin> GetAllSkins()
		{
			return _builtIns.Values
				.Concat(_userSkins.Values)
				.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
				.ToList();
		}

		public IReadOnlyList<NoteSkin> GetBuiltInSkins()
		{
			return _builtIns.Values
				.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
				.ToList();
		}

		public IReadOnlyList<NoteSkin> GetUserSkins()
		{
			return _userSkins.Values
				.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
				.ToList();
		}

		public NoteSkin? ResolveOrNull(string? skinId)
		{
			if (string.IsNullOrWhiteSpace(skinId))
				return null;

			if (_userSkins.TryGetValue(skinId, out var userSkin))
				return userSkin;

			if (_builtIns.TryGetValue(skinId, out var builtInSkin))
				return builtInSkin;

			return null;
		}

		public NoteSkin ResolveOrFallback(string? skinId, NoteColors.NoteColor fallbackColor)
		{
			return ResolveOrNull(skinId) ?? BuildFromColorKey(fallbackColor);
		}

		public static string GetComponentHex(NoteSkin skin, ColorComponent component)
		{
			return component switch
			{
				ColorComponent.Background => skin.PaperHex,
				ColorComponent.ControlBar => skin.ControlBarHex,
				ColorComponent.Buttons => skin.ButtonHex,
				ColorComponent.Text => skin.TextHex,
				ColorComponent.Highlights => skin.HighlightHex ?? skin.PaperHex,
				ColorComponent.Outlines => skin.OutlineHex ?? skin.ButtonHex,
				_ => skin.PaperHex
			};
		}

		private void LoadBuiltIns()
		{
			_builtIns.Clear();

			foreach (var kvp in NoteColors.Hex)
			{
				var colorKey = kvp.Key;
				var skin = BuildFromColorKey(colorKey);
				if (string.IsNullOrWhiteSpace(skin.Id))
					continue;

				_builtIns[skin.Id] = skin;
			}
		}

		private static NoteSkin BuildFromColorKey(NoteColors.NoteColor colorKey)
		{
			var paperHex = NoteColors.Hex[colorKey];
			var name = colorKey.ToString();

			string toHex(System.Windows.Media.Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

			var controlBar = toHex(ColorSchemeConverter.GetColor(name, paperHex, ColorComponent.ControlBar));
			var buttons = toHex(ColorSchemeConverter.GetColor(name, paperHex, ColorComponent.Buttons));
			var text = toHex(ColorSchemeConverter.GetColor(name, paperHex, ColorComponent.Text));
			var highlights = toHex(ColorSchemeConverter.GetColor(name, paperHex, ColorComponent.Highlights));
			var outlines = toHex(ColorSchemeConverter.GetColor(name, paperHex, ColorComponent.Outlines));

			return new NoteSkin
			{
				Id = name,
				Name = name,
				PaperHex = paperHex,
				ControlBarHex = controlBar,
				ButtonHex = buttons,
				TextHex = text,
				HighlightHex = highlights,
				OutlineHex = outlines
			};
		}

		private static NoteSkin FromPersist(NoteSkinPersist p)
		{
			return new NoteSkin
			{
				Id = p.Id,
				Name = string.IsNullOrWhiteSpace(p.Name) ? p.Id : p.Name,
				PaperHex = p.PaperHex,
          PaperImagePath = p.PaperImagePath,
				ControlBarHex = p.ControlBarHex,
				ButtonHex = p.ButtonHex,
				TextHex = p.TextHex,
				HighlightHex = p.HighlightHex,
				OutlineHex = p.OutlineHex,
				AccentHex = p.AccentHex,
				TitleFontFamily = p.TitleFontFamily,
				TitleFontSize = p.TitleFontSize,
				BodyFontFamily = p.BodyFontFamily,
				BodyFontSize = p.BodyFontSize
			};
		}

		public static NoteSkinPersist ToPersist(NoteSkin skin)
		{
			return new NoteSkinPersist
			{
				Id = skin.Id,
				Name = string.IsNullOrWhiteSpace(skin.Name) ? skin.Id : skin.Name,
				PaperHex = skin.PaperHex,
          PaperImagePath = skin.PaperImagePath,
				ControlBarHex = skin.ControlBarHex,
				ButtonHex = skin.ButtonHex,
				TextHex = skin.TextHex,
				HighlightHex = skin.HighlightHex,
				OutlineHex = skin.OutlineHex,
				AccentHex = skin.AccentHex,
				TitleFontFamily = skin.TitleFontFamily,
				TitleFontSize = skin.TitleFontSize,
				BodyFontFamily = skin.BodyFontFamily,
				BodyFontSize = skin.BodyFontSize
			};
		}
	}
}
