using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using StickIt.Services;

using static StickIt.Services.NoteColors;

namespace StickIt.Models
{
	public sealed class NoteProperties
	{
		// Identity
		public string Id { get; set; } = Guid.NewGuid().ToString("N");

		public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
		public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;


		// Appearance
		public string Title { get; set; } = "Untitled";
		public string FontFamily { get; set; } = "Segoe UI";
		public double FontSize { get; set; } = 14.0;
		public NoteColors.NoteColor ColorKey { get; set; } = NoteColors.NoteColor.ThreeMYellow;
      public string? SkinId { get; set; }
		public FontWeight FontWeight { get; set; }
		public System.Windows.FontStyle FontStyle { get; set; }

		// Window state
		public double X { get; set; }
		public double Y { get; set; }
		public bool IsMinimized { get; set; }

		// Sticky behavior
		public int StickyMode { get; set; }     // 0,1,2
		public string? StickyTarget { get; set; }

		// Content metadata (not content itself)
		public int CharCount { get; set; }
		public int WordCount { get; set; }
	}

}
