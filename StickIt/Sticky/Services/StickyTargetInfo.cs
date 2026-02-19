namespace StickIt.Sticky
{
	public sealed class StickyTargetInfo
	{
		// Required
		public IntPtr Hwnd { get; init; }

		// Stable identifiers (used for re-association)
		public string? ProcessName { get; init; }
		public int ProcessId { get; init; }

		// Informational (UI only)
		public string? WindowTitle { get; init; }
		public string? ClassName { get; init; }

		// Versioning / future-proofing
		public DateTime CapturedUtc { get; init; } = DateTime.UtcNow;
	}
}
