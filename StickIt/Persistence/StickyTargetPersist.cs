using System;

namespace StickIt.Persistence
{
	public sealed class StickyTargetPersist
	{
		public int ProcessId { get; set; }
		public string? ProcessName { get; set; }

		// Advisory hints (UI / best-effort matching later)
		public string? WindowTitle { get; set; }
		public string? ClassName { get; set; }

		public DateTime CapturedUtc { get; set; } = DateTime.UtcNow;

		public double? OffsetX { get; set; } = null;
		public double? OffsetY { get; set; } = null;

		// Physical screen-pixel point captured while choosing target.
		// Used to avoid rebinding to similarly-named windows on other monitors.
		public double? TargetAnchorX { get; set; } = null;
		public double? TargetAnchorY { get; set; } = null;

	}
}
