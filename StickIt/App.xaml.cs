using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using StickIt.Persistence;
using StickIt.Services;

namespace StickIt
{
	public partial class App : System.Windows.Application
	{
		private StickItState _state = new();
		private readonly List<NoteWindow> _windows = new();

		private const int StuckModeUnstuck = 0;
		private const int StuckModeAlwaysOnTop = 1;
		private const int StuckModeStuckToApp = 2;

		private bool _isShuttingDown;
		private TrayIconService? _tray;
		private DateTime _lastTrayNoticeUtc = DateTime.MinValue;
		private bool _shownStillRunningNoticeThisSession;

		private System.Threading.Mutex? _singleInstanceMutex;



		// Debounce saving so we don’t write on every keystroke/mouse move
		private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			ShutdownMode = ShutdownMode.OnExplicitShutdown;

			bool createdNew;
			_singleInstanceMutex = new System.Threading.Mutex(true, @"Global\969Studios.StickIt.v4", out createdNew);

			if (!createdNew)
			{
				Shutdown();
				return;
			}


			_saveTimer.Tick += (_, __) =>
			{
				_saveTimer.Stop();
				PersistAllWindows();
			};

			_state = JsonStore.LoadOrDefault();

			if (_state.Notes.Count == 0)
			{
				_state.Notes.Add(new NotePersist
				{
					Left = 200,
					Top = 200,
					Width = 400,
					Height = 400,
					Text = "",
					ColorKey = nameof(NoteColors.NoteColor.ThreeMYellow)
				});
				JsonStore.Save(_state);
			}

			foreach (var note in _state.Notes)
				SpawnWindow(note);

			// TODO: swap icon source later; for now use the executable icon.
			var iconPath = System.IO.Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory,
				"Properties",
				"Icons",
				"stickIt_Main.ico");

			var icon = new System.Drawing.Icon(iconPath);

			_tray = new StickIt.Services.TrayIconService(
				 icon,
				 CreateNewNoteFromTray,
				 MinimizeAllNotes,
				 RestoreAllNotes,
				 SaveAllNotesNow,
				 ShowAllNotes,
				 ShutdownRequested);




			Exit += (_, __) =>
			{
				_saveTimer.Stop();
				_tray?.Dispose();
				_tray = null;
				FlushPendingUiEdits();
				PersistAllWindowsOnShutdown();
			};

			SessionEnding += (_, __) =>
			{
				_saveTimer.Stop();
				FlushPendingUiEdits();
				PersistAllWindowsOnShutdown();
			};
		}

		private void FlushPendingUiEdits()
		{
			// 1) Force focused editors to commit (LostFocus/selection changes, etc.)
			try
			{
				Keyboard.ClearFocus();
			}
			catch
			{
				// best-effort
			}

			// 2) Drain dispatcher so pending input/render/binding work is applied before reading UI state
			try
			{
				Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, static () => { });
			}
			catch
			{
				// best-effort
			}
		}

		public void ShutdownRequested()
		{
			if (_isShuttingDown) return;
			_isShuttingDown = true;

			_saveTimer.Stop();

			FlushPendingUiEdits();
			PersistAllWindowsOnShutdown();

			foreach (var w in _windows.ToArray())
			{
				try { w.Close(); }
				catch { /* ignore */ }
			}

			_tray?.Dispose();
			_tray = null;

			Shutdown();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_tray?.Dispose();
			_tray = null;

			try { _singleInstanceMutex?.ReleaseMutex(); }
			catch { /* ignore */ }

			_singleInstanceMutex?.Dispose();
			_singleInstanceMutex = null;


			base.OnExit(e);
		}


		public void CreateNewNoteFromTray() => CreateNewNoteNear(null);

		public void NotifyStillRunningIfLastNoteClosed()
		{
			if (_tray == null) return;
			if (_shownStillRunningNoticeThisSession) return;

			_shownStillRunningNoticeThisSession = true;
			_tray.ShowStillRunningNotice();
		}


		public void MinimizeAllNotes()
		{
			foreach (var w in _windows.ToArray())
			{
				try { w.WindowState = System.Windows.WindowState.Minimized; }
				catch { /* ignore */ }
			}

			_saveTimer.Stop();
			FlushPendingUiEdits();
			PersistAllWindows();
		}
		public void RestoreAllNotes()
		{
			foreach (var w in _windows.ToArray())
			{
				try
				{
					w.Show();
					w.WindowState = System.Windows.WindowState.Normal;
				}
				catch { /* ignore */ }
			}

			_saveTimer.Stop();
			FlushPendingUiEdits();
			PersistAllWindows();
		}
		public void ShowAllNotes()
		{
			foreach (var w in _windows.ToArray())
			{
				try
				{
					w.Show();

					// If minimized, restore to normal so it can actually come to front.
					if (w.WindowState == System.Windows.WindowState.Minimized)
						w.WindowState = System.Windows.WindowState.Normal;

					w.Activate();

					// Helps when Activate() is ignored due to focus rules.
					w.Topmost = true;
					w.Topmost = false;
				}
				catch { /* ignore */ }
			}

			_saveTimer.Stop();
			FlushPendingUiEdits();
			PersistAllWindows();
		}




		public void SaveAllNotesNow()
		{
			_saveTimer.Stop();
			FlushPendingUiEdits();
			PersistAllWindows();
		}


		public void QueueSaveFromWindow() => QueueSave();

		public void RestoreHiddenNotes()
		{
			foreach (var w in _windows.Where(w => w.IsLoaded))
				w.SetIsMinimized(false);
			QueueSave();
		}



		private void SpawnWindow(NotePersist note)
		{
			// Build the model from persisted state (canonical)
			//var model = new StickIt.Models.NoteModel();
			var model = NotePersistMapper.ToModel(note);


			// Identity + metadata
			model.Props.Id = note.Id;

			model.Title = note.Title;

			if (Enum.TryParse(note.ColorKey, out NoteColors.NoteColor ck))
				model.ColorKey = ck;
			else
				model.ColorKey = NoteColors.NoteColor.ThreeMYellow;

			model.FontFamily = note.FontFamily;
			model.FontSize = note.FontSize;

			// Create window
			var w = new NoteWindow(model);

			// Geometry first
			w.Width = note.Width <= 0 ? 380 : note.Width;
			w.Height = note.Height <= 0 ? 320 : note.Height;
			w.Left = note.Left;
			w.Top = note.Top;

			ClampToVirtualScreen(w);

			// Content (prefer RTF)
			if (!string.IsNullOrWhiteSpace(note.Rtf))
				w.SetRtf(note.Rtf);
			else
				w.SetText(note.Text);

			// Sticky + minimized
			w.SetStuckMode(note.StuckMode);

			w.Loaded += (_, __) =>
			{
				if (note.IsMinimized)
					w.SetIsMinimized(true);
			};

			// Autosave triggers
			w.LocationChanged += (_, __) => QueueSave();
			w.StateChanged += (_, __) => QueueSave();
			w.NoteTextChanged += (_, __) => QueueSave();
			w.Closed += (_, __) =>
			{
				if (_isShuttingDown)
					return; // shutdown path: don't touch _windows (and don't trigger saves)

				_windows.Remove(w);
				QueueSave();

				// Leave app running when last note is deleted.
				if (_windows.Count == 0)
				{
					NotifyStillRunningIfLastNoteClosed();

					// Optional: keep user from ever having “zero notes”
					// CreateNewNoteNear(null);
				}

			};

			_windows.Add(w);
			w.Show();
		}


		private static void ClampToVirtualScreen(NoteWindow w)
		{
			double left = SystemParameters.VirtualScreenLeft;
			double top = SystemParameters.VirtualScreenTop;
			double width = SystemParameters.VirtualScreenWidth;
			double height = SystemParameters.VirtualScreenHeight;

			double right = left + width;
			double bottom = top + height;

			const double margin = 20;

			// Clamp right/bottom overflow
			if (w.Left + w.Width > right - margin)
				w.Left = right - w.Width - margin;

			if (w.Top + w.Height > bottom - margin)
				w.Top = bottom - w.Height - margin;

			// Clamp left/top overflow
			if (w.Left < left + margin)
				w.Left = left + margin;

			if (w.Top < top + margin)
				w.Top = top + margin;

			// If window is larger than the virtual screen, at least pin it to the margin.
			if (w.Width > width - (margin * 2))
				w.Left = left + margin;

			if (w.Height > height - (margin * 2))
				w.Top = top + margin;
		}



		private void QueueSave()
		{
			_saveTimer.Stop();
			_saveTimer.Start();
		}

		private void PersistAllWindows()
		{
			var windows = _windows.ToArray();

			_state.Notes = windows
				 .Select(w => NotePersistMapper.FromWindow(
					  w,
					  w.GetStuckMode(),
					  DateTime.UtcNow))
				 .ToList();

			JsonStore.Save(_state);
		}



		private void PersistAllWindowsOnShutdown()
		{
			var windows = _windows.ToArray();

			_state.Notes = windows
				 .Select(w =>
				 {
					 var stuck = w.GetStuckMode();
					 if (stuck == StuckModeStuckToApp)
						 stuck = StuckModeUnstuck;

					 return NotePersistMapper.FromWindow(
					w,
					stuck,
					DateTime.UtcNow);
				 })
				 .ToList();

			JsonStore.Save(_state);
		}



		public void CreateNewNoteNear(NoteWindow? anchor = null)
		{
			var p = new NotePersist
			{
				Left = anchor != null ? anchor.Left + 20 : 200,
				Top = anchor != null ? anchor.Top + 20 : 200,
				Width = 400,
				Height = 400,
				Title = "Untitled",
				Text = "",
				ColorKey = nameof(NoteColors.NoteColor.ThreeMYellow),
				StuckMode = 0,
				IsMinimized = false,
				CreatedUtc = DateTime.UtcNow,
				ModifiedUtc = DateTime.UtcNow,
			};

			// Add to state so it exists even if we crash before debounce tick
			_state.Notes.Add(p);
			JsonStore.Save(_state);

			SpawnWindow(p);
		}

	}
}
