using System;
using System.Collections.Generic;

using StickIt.Services;

namespace StickIt.Persistence
{

	public sealed class NotePersist
	{
		public string Id { get; set; } = Guid.NewGuid().ToString("N");

		// Window position/size
		public double Left { get; set; }
		public double Top { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }

		public string? Rtf { get; set; } = null;

		// Content + appearance
		public string Title { get; set; } = "Untitled";
		public string Text { get; set; } = "";

		public string ColorKey { get; set; } = nameof(NoteColors.NoteColor.ThreeMYellow);

		public string FontFamily { get; set; } = "Segoe UI";
		public double FontSize { get; set; } = 14.0;

		public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
		public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

		public int StickyMode { get; set; } = 0;       // your StuckMode
		public string? StickyTarget { get; set; } = null;

		public bool IsMinimized { get; set; } = false; // already present; keep one canonical field


		// 0 = not stuck, 1 = always-on-top, 2 = stuck-to-program (future)
		public int StuckMode { get; set; } = 0;

		// Optional placeholder for future “stuck to program” targeting (safe to ignore for now)
		public string? StuckTarget { get; set; } = null;
	}

	public sealed class StickItState
	{
		public int Version { get; set; } = StateMigrator.CurrentVersion;

		public List<NotePersist> Notes { get; set; } = new();
	}

}