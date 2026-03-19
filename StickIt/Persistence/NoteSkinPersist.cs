namespace StickIt.Persistence
{
	public sealed class NoteSkinPersist
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;

		public string PaperHex { get; set; } = "#F7E03D";
      public string? PaperImagePath { get; set; }
		public string ControlBarHex { get; set; } = "#EAC52C";
		public string ButtonHex { get; set; } = "#C59A00";
		public string TextHex { get; set; } = "#1C1C1C";
		public string? HighlightHex { get; set; }
		public string? OutlineHex { get; set; }
		public string? AccentHex { get; set; }

		public string? TitleFontFamily { get; set; }
		public double? TitleFontSize { get; set; }
		public string? BodyFontFamily { get; set; }
		public double? BodyFontSize { get; set; }
	}
}
