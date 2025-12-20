using WpfApplication = System.Windows.Application;

using System.IO;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using StickIt.Models;
using StickIt.Services;

namespace StickIt
{
	public partial class NoteWindow : Window
	{
		private readonly NoteModel? _note;
		private int _noteStuckMode = 0;

		public string NoteId { get; private set; } = Guid.NewGuid().ToString("N");
		public NoteColors.NoteColor GetColorKey() => _note?.ColorKey ?? NoteColors.NoteColor.ThreeMYellow;
		public string GetTitle() => _note?.Title ?? "Untitled";
		public void SetFontFamily(string family)
		{
			if (_note != null)
				_note.FontFamily = string.IsNullOrWhiteSpace(family) ? "Helvetica" : family;
		}
		public void SetFontSize(double size)
		{
			if (_note != null)
				_note.FontSize = size <= 0 ? 14.0 : size;
		}
		public string GetFontFamily() => _note?.FontFamily ?? "Helvetica";
		public double GetFontSize() => _note?.FontSize ?? 14.0;

		public void SetTitle(string title)
		{
			if (_note != null)
				_note.Title = title ?? "Untitled";
		}

		public int GetStuckMode() => _noteStuckMode;        // see below
		public void SetStuckMode(int mode) => ApplyStuckMode(mode);

		public bool GetIsMinimized() => this.WindowState == WindowState.Minimized;
		public void SetIsMinimized(bool minimized)
		{
			if (minimized) this.WindowState = WindowState.Minimized;
		}

		public event EventHandler? NoteTextChanged;

		public NoteWindow() : this(new NoteModel())
		{
		}

		public NoteWindow(NoteModel note)
		{
			InitializeComponent();

			_note = note;
			DataContext = _note;

			// Keep autosave behavior consistent regardless of constructor use
			txtNoteContent.TextChanged += (_, __) => NoteTextChanged?.Invoke(this, EventArgs.Empty);		

			ControlBar.MouseLeftButtonDown += (_, __) => DragMove();

			KeyDown += (_, e) =>
			{
				if (e.Key == System.Windows.Input.Key.N &&
					 (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
				{
					((App) WpfApplication.Current).CreateNewNoteNear(this);
					e.Handled = true;
					return;
				}

				if (e.Key == System.Windows.Input.Key.F12)
					new DebugColorsWindow(_note) { Owner = this }.Show();
			};
		}

		public void SetNoteId(string id) => NoteId = string.IsNullOrWhiteSpace(id) ? NoteId : id;

		public void SetText(string text)
		{
			txtNoteContent.Document.Blocks.Clear();
			txtNoteContent.Document.Blocks.Add(new Paragraph(new Run(text ?? "")));
		}

		public string GetText()
		{
			var range = new TextRange(
				txtNoteContent.Document.ContentStart,
				txtNoteContent.Document.ContentEnd);

			return range.Text.TrimEnd('\r', '\n');
		}

		public void SetColorKey(string keyName)
		{
			if (_note != null && Enum.TryParse(keyName, out NoteColors.NoteColor key))
				_note.ColorKey = key;
		}

		private void ColorMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (_note == null)
				return;

			if (sender is not MenuItem menuItem)
				return;

			if (menuItem.Tag is string tag && Enum.TryParse(tag, out NoteColors.NoteColor color))
				_note.ColorKey = color;
		}

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void btnMinimize_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void btnPinCycle_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Implement pin cycle logic here
		}

		private void ApplyStuckMode(int mode)
		{
			if (mode < 0 || mode > 2) mode = 0;
			_noteStuckMode = mode;

			// v4 behavior:
			// 0 = normal, 1 = Topmost, 2 = reserved (treat as normal for now)
			Topmost = (mode == 1);
		}
		public string? GetRtf()
		{
			try
			{
				var range = new TextRange(
					 txtNoteContent.Document.ContentStart,
					 txtNoteContent.Document.ContentEnd);

				using var ms = new MemoryStream();
				range.Save(ms, System.Windows.DataFormats.Rtf);

				// Avoid writing meaningless empty RTF blobs
				if (ms.Length == 0)
					return null;

				return Encoding.UTF8.GetString(ms.ToArray());
			}
			catch
			{
				return null;
			}
		}

		public void SetRtf(string? rtf)
		{
			txtNoteContent.Document.Blocks.Clear();

			if (string.IsNullOrWhiteSpace(rtf))
				return;

			var bytes = Encoding.UTF8.GetBytes(rtf);

			using var ms = new MemoryStream(bytes);
			var range = new TextRange(txtNoteContent.Document.ContentStart, txtNoteContent.Document.ContentEnd);

			try
			{
				range.Load(ms, System.Windows.DataFormats.Rtf);
			}
			catch
			{
				// If corrupted/invalid RTF, leave empty (fallback is applied by App via SetText)
				txtNoteContent.Document.Blocks.Clear();
			}
		}


	}
}
