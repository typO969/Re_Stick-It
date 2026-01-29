using System;

using StickIt.Models;
using StickIt.Services;

namespace StickIt.Persistence
{
	public static class NotePersistMapper
	{
		public static NoteModel ToModel(NotePersist p)
		{
			var m = new NoteModel();

			// Canonical identity
			m.Props.Id = p.Id;

			// Visible metadata
			m.Title = p.Title;

			m.Props.CreatedUtc = p.CreatedUtc;
			m.Props.ModifiedUtc = p.ModifiedUtc;

			if (Enum.TryParse(p.ColorKey, out NoteColors.NoteColor ck))
				m.ColorKey = ck;
			else
				m.ColorKey = NoteColors.NoteColor.ThreeMYellow;

			m.FontFamily = p.FontFamily;
			m.FontSize = p.FontSize;

			return m;
		}

		public static NotePersist FromWindow(
			  NoteWindow w,
				int stuckModeFinal,
				DateTime modifiedUtc)
		{
			return new NotePersist
			{
				Id = w.NoteId,

				Left = w.Left,
				Top = w.Top,
				Width = w.Width,
				Height = w.Height,

				Title = w.GetTitle(),

				Rtf = w.GetRtf(),
				Text = w.GetText(),

				ColorKey = w.GetColorKey().ToString(),

				FontFamily = w.GetFontFamily(),
				FontSize = w.GetFontSize(),

				StickyTargetPersist = w.GetStickyTargetPersist(),

				StuckMode = stuckModeFinal,
				IsMinimized = w.GetIsMinimized(),

				// Canonical timestamps now live on the note itself
				CreatedUtc = w.GetCreatedUtc(),
				ModifiedUtc = modifiedUtc
			};
		}
	}
}
