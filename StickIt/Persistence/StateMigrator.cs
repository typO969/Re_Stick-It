using System;

using StickIt.Services;

namespace StickIt.Persistence
{
	public static class StateMigrator
	{
		// Increment this only when you introduce a breaking or meaningfully new schema change.
      public const int CurrentVersion = 7;

		public static StickItState MigrateToCurrent(StickItState state)
		{
			state ??= new StickItState { Version = 0 };

			// Treat missing/invalid version as 0
			if (state.Version < 0) state.Version = 0;

			// Always ensure list exists
			state.Notes ??= new();
			state.Preferences ??= new();

			// v0..v4 -> v5 normalization pass (safe even if already v5)
			NormalizeAllNotes(state);
			NormalizePreferences(state.Preferences);

			// If you want actual stepwise migrations later, keep the switch loop.
			// For now, your schema is stable enough that normalization is sufficient.
			state.Version = CurrentVersion;
			return state;
		}

		private static void NormalizeAllNotes(StickItState s)
		{
			foreach (var n in s.Notes)
			{
				// Id
				if (string.IsNullOrWhiteSpace(n.Id))
					n.Id = Guid.NewGuid().ToString("N");

				// Geometry defaults / sanity
				if (n.Width <= 0) n.Width = 400;
				if (n.Height <= 0) n.Height = 400;

				// Title / text
				if (string.IsNullOrWhiteSpace(n.Title))
					n.Title = "Untitled";
				if (n.Text == null)
					n.Text = "";

				// Canonical content: always persist as RTF.
				if (string.IsNullOrWhiteSpace(n.Rtf))
					n.Rtf = RtfCodec.FromPlainText(n.Text, n.FontSize);

				if (n.RtfSchemaVersion <= 0)
					n.RtfSchemaVersion = 1;

				// ColorKey
				if (string.IsNullOrWhiteSpace(n.ColorKey) ||
					 !Enum.TryParse(n.ColorKey, out NoteColors.NoteColor _))
				{
					n.ColorKey = nameof(NoteColors.NoteColor.ThreeMYellow);
				}

				// Font
				if (string.IsNullOrWhiteSpace(n.FontFamily))
					n.FontFamily = "Segoe UI";
				if (n.FontSize <= 0)
					n.FontSize = 14.0;

				// Stuck mode bounds (0..2)
				if (n.StuckMode < 0 || n.StuckMode > 2)
					n.StuckMode = 0;

				// Minimized: default already false; nothing needed.

				// Stuck target: optional; ok as null/empty.
				// Rtf: optional; ok as null.
			}
		}

		private static void NormalizePreferences(AppPreferences prefs)
		{
			if (string.IsNullOrWhiteSpace(prefs.TitleFontFamily))
				prefs.TitleFontFamily = "Helvetica";
			if (prefs.TitleFontSize <= 0)
				prefs.TitleFontSize = 19.0;

			if (string.IsNullOrWhiteSpace(prefs.BodyFontFamily))
				prefs.BodyFontFamily = "Segoe UI";
			if (prefs.BodyFontSize <= 0)
				prefs.BodyFontSize = 14.0;

			if (prefs.DesktopAreaWidth is not null && prefs.DesktopAreaWidth <= 0)
				prefs.DesktopAreaWidth = null;
			if (prefs.DesktopAreaHeight is not null && prefs.DesktopAreaHeight <= 0)
				prefs.DesktopAreaHeight = null;

			if (!Enum.IsDefined(typeof(Mode2HostMissingAction), prefs.Mode2HostMissingAction))
				prefs.Mode2HostMissingAction = Mode2HostMissingAction.SwitchToMode1;

			prefs.SyncFilePath ??= string.Empty;
		}
	}
}
