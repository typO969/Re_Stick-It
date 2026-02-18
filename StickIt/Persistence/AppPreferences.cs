namespace StickIt.Persistence
{
	public sealed class AppPreferences
	{
		public bool RunOnStartup { get; set; }
		public bool DarkMode { get; set; }
		public bool ShowTaskbarIcon { get; set; } = true;
		public bool ShowTrayIcon { get; set; } = true;

		public bool AlwaysStickNewNotesToDesktop { get; set; }
		public bool SnapNotesToGrid { get; set; }
		public bool KeepNotesInsideDesktopArea { get; set; }
		public double? DesktopAreaLeft { get; set; }
		public double? DesktopAreaTop { get; set; }
		public double? DesktopAreaWidth { get; set; }
		public double? DesktopAreaHeight { get; set; }
		public bool ConfirmOnDelete { get; set; }
		public bool HideNotesOnShowDesktop { get; set; }
		public bool TreatNotesAsTopLevelWindows { get; set; } = true;
		public bool SeparateNotesPerDesktopArea { get; set; }

		public string TitleFontFamily { get; set; } = "Helvetica";
		public double TitleFontSize { get; set; } = 19.0;
		public string BodyFontFamily { get; set; } = "Segoe UI";
		public double BodyFontSize { get; set; } = 14.0;
		public bool ShowDateAlongTitle { get; set; }
		public bool EnableDropShadow { get; set; } = true;
	}
}
