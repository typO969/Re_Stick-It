using WpfApplication = System.Windows.Application;

using System.IO;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

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
				// Ctrl+N / Ctrl+W / Ctrl+M
				if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
				{
					switch (e.Key)
					{
						case Key.N:
							AppInstance.CreateNewNoteNear(this);
							e.Handled = true;
							return;

						case Key.W:
							Close(); // close = delete
							e.Handled = true;
							return;

						case Key.M:
							WindowState = WindowState.Minimized;
							AppInstance.QueueSaveFromWindow();
							e.Handled = true;
							return;
					}
				}

				
				if (e.Key == Key.F12)
				{
					new DebugColorsWindow(_note!) { Owner = this }.Show();
					e.Handled = true;
				}

				if (e.Key == System.Windows.Input.Key.N &&
						 (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
				{
					((App) WpfApplication.Current).CreateNewNoteNear(this);
					e.Handled = true;
					return;
				}

				if (e.Key == System.Windows.Input.Key.F12)
					new DebugColorsWindow(_note!) { Owner = this }.Show();
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


		private void txtNoteContent_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			if (this.Content is FrameworkElement fe && fe.ContextMenu != null)
				fe.ContextMenu.IsOpen = true;
		}



		public DateTime GetCreatedUtc() => _note?.Props.CreatedUtc ?? default;

		private void TouchModifiedUtc()
		{
			if (_note != null)
				_note.Props.ModifiedUtc = DateTime.UtcNow;
		}

		private App AppInstance => (App) System.Windows.Application.Current;

		private void Menu_NewNote(object sender, RoutedEventArgs e)
		{
			AppInstance.CreateNewNoteNear(this);
		}

		private void Menu_Color(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem mi || mi.Tag is not string keyName) return;

			if (Enum.TryParse(keyName, out NoteColors.NoteColor key))
			{
				if (_note != null)
					_note.ColorKey = key;
				AppInstance.QueueSaveFromWindow(); // we’ll add this tiny helper in App
			}
		}

		private void Menu_Sticky(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem mi) return;

			if (int.TryParse(mi.Tag?.ToString(), out var mode))
			{
				SetStuckMode(mode);
				AppInstance.QueueSaveFromWindow();
			}
		}

		private void Menu_Minimize(object sender, RoutedEventArgs e)
		{
			SetIsMinimized(true);
			AppInstance.QueueSaveFromWindow();
		}

		private void Menu_Restore(object sender, RoutedEventArgs e)
		{
			SetIsMinimized(false);
			AppInstance.QueueSaveFromWindow();
		}

		private void Menu_Debug(object sender, RoutedEventArgs e)
		{
			new DebugColorsWindow(_note!) { Owner = this }.Show();
		}

		private void Menu_Delete(object sender, RoutedEventArgs e)
		{
			Close(); // close = delete per your rule
		}

		private void Menu_Exit(object sender, RoutedEventArgs e)
		{
			AppInstance.ShutdownRequested();
		}


		private void Menu_Cut(object sender, RoutedEventArgs e) => txtNoteContent.Cut();
		private void Menu_Copy(object sender, RoutedEventArgs e) => txtNoteContent.Copy();
		private void Menu_Paste(object sender, RoutedEventArgs e) => txtNoteContent.Paste();

		private void Menu_SaveNow(object sender, RoutedEventArgs e)
		{
			AppInstance.QueueSaveFromWindow(); // triggers debounce
		}

		private void Menu_MinimizeAll(object sender, RoutedEventArgs e)
		{
			AppInstance.MinimizeAllNotes();
		}

		private void Menu_RestoreAll(object sender, RoutedEventArgs e)
		{
			AppInstance.RestoreHiddenNotes();
		}

		// Placeholders (disabled now; handler exists in case you enable later)
		private void Menu_FontSettings(object sender, RoutedEventArgs e) { }
		private void Menu_LoadNotes(object sender, RoutedEventArgs e) { }
		private void Menu_Preferences(object sender, RoutedEventArgs e) { }
		private void Menu_NoteManager(object sender, RoutedEventArgs e) { }


		private void txtNoteTitle_select(object sender, MouseButtonEventArgs e)
		{
			// Ensure we have a note and the TextBox exists
			if (_note == null || txtNoteTitle == null) return;

			// Clear only if the current title is still the default
			// Prefer checking the TextBox to reflect the UI state; fallback to model if needed
			var isDefaultTitle =
				string.Equals(txtNoteTitle.Text, "Untitled", StringComparison.Ordinal) ||
				string.Equals(_note.Title, "Untitled", StringComparison.Ordinal);

			if (!isDefaultTitle) return;

			// Clear the title and focus the TextBox
			txtNoteTitle.Clear();
			txtNoteTitle.Focus();

			// Update the model in case binding isn't immediate
			_note.Title = string.Empty;

			// Mark as handled so the click doesn't re-trigger other handlers
			e.Handled = true;
		}

		private void NoteColors_SubmenuOpened(object sender, RoutedEventArgs e)
		{
			if (_note == null) return;

			var current = _note.ColorKey;

			foreach (var item in miNoteColors.Items.OfType<MenuItem>())
			{
				if (item.Tag is not string tag) continue;
				if (!Enum.TryParse(tag, out NoteColors.NoteColor key)) continue;

				var isCurrent = (key == current);

				item.IsEnabled = !isCurrent;

				// optional, but nice UX:
				item.IsCheckable = true;
				item.IsChecked = isCurrent;
			}
		}





		// !!!!  -------------  NOTE PROPERTIES TEMP START ---------- !!!!!!!!
		private void NoteMenu_Opened(object sender, RoutedEventArgs e)
		{
			// ID
			if (_note == null)
				return;
			miProp_Id.Header = $"Note ID: {_note.Props.Id}";

			// Contains: chars, words, lines
			var text = GetText();
			var chars = text.Length;
			var words = CountWords(text);
			var lines = CountLines(text);
			miProp_Contains.Header = $"Contains: {chars} chars, {words} words, {lines} lines";

			// Timestamps (UTC; you can format later)
			miProp_Created.Header = $"Created: {_note.Props.CreatedUtc:u}";
			miProp_Modified.Header = $"Modified: {_note.Props.ModifiedUtc:u}";

			// Position
			miProp_Position.Header = $"Position: X={Left:0}, Y={Top:0}";

			// Color
			miProp_Color.Header = $"Color: {_note.ColorKey}";

			// Sticky
			miProp_Sticky.Header = $"Sticky: {StickyLabel(GetStuckMode())}";

			// Font
			miProp_Font.Header = $"Font: {_note.FontFamily}, {_note.FontSize:0.#} pt";
		}

		private static int CountWords(string s)
		{
			if (string.IsNullOrWhiteSpace(s)) return 0;
			return s.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
		}

		private static int CountLines(string s)
		{
			if (string.IsNullOrEmpty(s)) return 0;
			return s.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Length;
		}

		private static string StickyLabel(int mode) => mode switch
		{
			0 => "Not stuck",
			1 => "Always on top",
			2 => "Stick to application (future)",
			_ => $"Unknown ({mode})"
		};
		// !!!!!!!!!!! ----------- NOTE PROPERTIES TEMP END ---------- !!!!!!!!!!!

	}
}
