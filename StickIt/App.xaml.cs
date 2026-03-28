using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

using StickIt.Persistence;
using StickIt.Services;
using StickIt.Sticky.Services;

namespace StickIt
{
	public partial class App : System.Windows.Application
	{
		private StickItState _state = new();
		private readonly List<NoteWindow> _windows = new();

		private bool _isShuttingDown;
		private TrayIconService? _tray;
		private DateTime _lastTrayNoticeUtc = DateTime.MinValue;
		private bool _shownStillRunningNoticeThisSession;
		private NoteManagerWindow? _noteManagerWindow;
		private PreferencesWindow? _preferencesWindow;

		private System.Threading.Mutex? _singleInstanceMutex;
		private const int MaxNotesToAutoOpen = 25;

      private double _nextSpawnLeft = 200;
      private double _nextSpawnTop = 200;





		private DateTime? _pendingSaveDueUtc;

      // Debounce saving so we don’t write on every keystroke/mouse move
      private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };

		public AppPreferences Preferences => _state.Preferences;
		public bool IsShuttingDown => _isShuttingDown;

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
			SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

			_state = JsonStore.LoadOrDefault();
			ApplyPreferences(_state.Preferences, persist: false);

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

			var notes = _state.Notes ?? new List<NotePersist>();

			if (notes.Count > MaxNotesToAutoOpen)
			{
				var total = notes.Count;

				notes = notes
					 .OrderByDescending(n => n.ModifiedUtc)
					 .Take(MaxNotesToAutoOpen)
					 .ToList();

				_state.Notes = notes;
				JsonStore.Save(_state);

				_tray?.ShowTrimmedLoadNotice(notes.Count, total);
			}

			foreach (var note in notes)
			{
				SpawnWindow(note);
			}

			UpdateTrayIcon();





         Exit += (_, __) =>
			{
				_saveTimer.Stop();
				_tray?.Dispose();
				_tray = null;
				_preferencesWindow?.Close();
				_preferencesWindow = null;
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

      private System.Windows.Point FindNonOverlappingPosition(double desiredLeft, double desiredTop, double w, double h)
      {
         double left = SystemParameters.VirtualScreenLeft;
         double top = SystemParameters.VirtualScreenTop;
         double width = SystemParameters.VirtualScreenWidth;
         double height = SystemParameters.VirtualScreenHeight;

         const int step = 24;
         const double margin = 20;

         double minX = left + margin;
         double minY = top + margin;
         double maxX = (left + width) - w - margin;
         double maxY = (top + height) - h - margin;

         // Clamp desired start
         double startX = Math.Max(minX, Math.Min(maxX, desiredLeft));
         double startY = Math.Max(minY, Math.Min(maxY, desiredTop));

         // Occupied rects = currently open note windows
         var occupied = _windows
            .Where(win => win != null && win.IsLoaded)
            .Select(win =>
            {
               // Use RestoreBounds if minimized, and fall back to the intended new note size.
               var b = (win.WindowState == WindowState.Minimized) ? win.RestoreBounds : new Rect(win.Left, win.Top, win.Width, win.Height);

               double ow = (b.Width > 1) ? b.Width : w;
               double oh = (b.Height > 1) ? b.Height : h;

               return new Rect(b.Left, b.Top, ow, oh);
            })
            .ToList();


         // Quick accept if free
         var startRect = new Rect(startX, startY, w, h);
         if (!occupied.Any(r => r.IntersectsWith(startRect)))
            return new System.Windows.Point(startX, startY);

         // Scan outward in a simple expanding pattern
         for (int ring = 1; ring < 200; ring++)
         {
            double dx = ring * step;
            double dy = ring * step;

            var candidates = new[]
            {
         new System.Windows.Point(startX + dx, startY),
         new System.Windows.Point(startX - dx, startY),
         new System.Windows.Point(startX, startY + dy),
         new System.Windows.Point(startX, startY - dy),
                         
         new System.Windows.Point(startX + dx, startY + dy),
         new System.Windows.Point(startX + dx, startY - dy),
         new System.Windows.Point(startX - dx, startY + dy),
         new System.Windows.Point(startX - dx, startY - dy),
      };

            foreach (var c in candidates)
            {
               double cx = Math.Max(minX, Math.Min(maxX, c.X));
               double cy = Math.Max(minY, Math.Min(maxY, c.Y));

               var r = new Rect(cx, cy, w, h);
               if (!occupied.Any(o => o.IntersectsWith(r)))
                  return new System.Windows.Point(cx, cy);
            }
         }

         // Last resort: return clamped desired
         return new System.Windows.Point(startX, startY);
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
			try
			{
				_noteManagerWindow?.Close();
			}
			catch
			{
				// best-effort
			}

			_noteManagerWindow = null;
			_preferencesWindow = null;

			_tray?.Dispose();
			_tray = null;

			try { _singleInstanceMutex?.ReleaseMutex(); }
			catch { /* ignore */ }

			_singleInstanceMutex?.Dispose();
			_singleInstanceMutex = null;


			base.OnExit(e);
		}


      public void CreateNewNoteFromTray()
      {
         // NotifyIcon can raise events via WinForms plumbing; force onto WPF UI thread.
         Dispatcher.BeginInvoke(new Action(() => CreateNewNoteNear(null)));
      }


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


		public void QueueSaveFromWindow() => QueueSave(350);

		public void QueueTextSaveFromWindow() => QueueSave(700);
		public void QueueUndoRedoSaveFromWindow() => QueueSave(1300);

		public void RestoreHiddenNotes()
		{
			foreach (var w in _windows.Where(w => w.IsLoaded))
				w.SetIsMinimized(false);
			QueueSave(300);
		}

		public void ShowPreferences()
		{
			if (_preferencesWindow == null || !_preferencesWindow.IsLoaded)
			{
				_preferencesWindow = new PreferencesWindow(Preferences);
				_preferencesWindow.Owner = _windows.FirstOrDefault(w => w.IsLoaded);
				_preferencesWindow.Closed += (_, __) => _preferencesWindow = null;
				_preferencesWindow.Show();
				return;
			}

			_preferencesWindow.Activate();
		}

		public void ApplyPreferences(AppPreferences preferences, bool persist)
		{
			_state.Preferences = preferences ?? new AppPreferences();
			AppThemeService.ApplyTheme(_state.Preferences.DarkMode);
			UpdateTrayIcon();
			UpdateRunOnStartup();
			ApplyPreferencesToWindows();

			if (persist)
				JsonStore.Save(_state);
		}

		private void ApplyPreferencesToWindows()
		{
			foreach (var w in _windows.Where(w => w.IsLoaded))
			{
				w.ShowInTaskbar = Preferences.ShowTaskbarIcon;
				w.ApplyPreferences(Preferences);
				ClampToPreferredArea(w);
			}
		}

		private void UpdateRunOnStartup()
		{
			try
			{
				var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
				if (!string.IsNullOrWhiteSpace(exePath))
					StartupRegistryService.SetRunOnStartup(Preferences.RunOnStartup, exePath);
			}
			catch
			{
				// best-effort
			}
		}

		private void UpdateTrayIcon()
		{
			if (!Preferences.ShowTrayIcon)
			{
				_tray?.Dispose();
				_tray = null;
				return;
			}

			if (_tray != null)
				return;

			var iconPath = System.IO.Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory,
				"Properties",
				"Icons",
				"stickIt_Main.ico");

			if (!System.IO.File.Exists(iconPath))
				return;

			try
			{
				var icon = new System.Drawing.Icon(iconPath);

				_tray = new StickIt.Services.TrayIconService(
					icon,
					() => Dispatcher.BeginInvoke(new Action(() => CreateNewNoteNear(null))),
					() => Dispatcher.BeginInvoke(new Action(MinimizeAllNotes)),
					() => Dispatcher.BeginInvoke(new Action(RestoreAllNotes)),
					() => Dispatcher.BeginInvoke(new Action(SaveAllNotesNow)),
					() => Dispatcher.BeginInvoke(new Action(ShowAllNotes)),
					() => Dispatcher.BeginInvoke(new Action(ShutdownRequested))
				);
			}
			catch
			{
				// Best-effort tray icon initialization.
			}
		}

		public void ShowNoteManager()
		{
			if (_noteManagerWindow == null || !_noteManagerWindow.IsLoaded)
			{
				_noteManagerWindow = new NoteManagerWindow();
				_noteManagerWindow.Owner = _windows.FirstOrDefault(w => w.IsLoaded);
				_noteManagerWindow.Closed += (_, __) => _noteManagerWindow = null;
				_noteManagerWindow.Show();
				return;
			}

			_noteManagerWindow.Activate();
		}

		public IReadOnlyList<NoteWindow> GetOpenWindowsSnapshot()
		{
			return _windows.Where(w => w.IsLoaded).ToList();
		}

		public void UpdateDefaultBodyFont(string fontFamily, double fontSize)
		{
			_state.Preferences.BodyFontFamily = string.IsNullOrWhiteSpace(fontFamily) ? _state.Preferences.BodyFontFamily : fontFamily;
			_state.Preferences.BodyFontSize = fontSize > 0 ? fontSize : _state.Preferences.BodyFontSize;
			JsonStore.Save(_state);
		}

		public void ApplyFontSettingsToAllOpenNotes(FontSettingsData settings, NoteWindow? excludeWindow = null)
		{
			foreach (var window in _windows.Where(w => w.IsLoaded && !ReferenceEquals(w, excludeWindow)))
				window.ApplyFontSettingsToEntireNote(settings);

			QueueSave(700);
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
			w.ShowInTaskbar = Preferences.ShowTaskbarIcon;
			w.ApplyPreferences(Preferences);

			w.SetStickyTarget(note.StickyTargetPersist);

			// Geometry first
			w.Width = note.Width <= 0 ? 380 : note.Width;
			w.Height = note.Height <= 0 ? 320 : note.Height;
			w.Left = note.Left;
			w.Top = note.Top;

			var restoredToMonitor = TryRestoreToSavedMonitor(note, w);
			if (!restoredToMonitor)
				ClampToVirtualScreen(w);
			ClampToPreferredArea(w);

			// Content (prefer RTF)
			w.SetRtf(note.Rtf ?? RtfCodec.FromPlainText(note.Text, note.FontSize));
			w.SetInkIsfBase64(note.InkIsfBase64);

			// Sticky + minimized
			w.SetStuckMode(note.StuckMode);

			w.Loaded += (_, __) =>
			{
				if (note.IsMinimized)
					w.SetIsMinimized(true);
			};

			// Autosave triggers
			w.LocationChanged += (_, __) => QueueSave(300);
			w.StateChanged += (_, __) => QueueSave(300);
			w.NoteTextChanged += (_, __) => QueueSave(700);
			w.NoteUndoRedoRequested += (_, __) => QueueSave(1300);
			w.Closed += (_, __) =>
			{
				if (_isShuttingDown)
					return; // shutdown path: don't touch _windows (and don't trigger saves)

				_windows.Remove(w);
				QueueSave(300);

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

		private static void ClampToWorkingArea(NoteWindow w, System.Windows.Forms.Screen screen)
		{
			var wa = screen.WorkingArea;
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(w);
			double waLeft = wa.Left / Math.Max(0.01, dpi.DpiScaleX);
			double waTop = wa.Top / Math.Max(0.01, dpi.DpiScaleY);
			double waWidth = wa.Width / Math.Max(0.01, dpi.DpiScaleX);
			double waHeight = wa.Height / Math.Max(0.01, dpi.DpiScaleY);
			const double margin = 20;

        double right = waLeft + waWidth;
			double bottom = waTop + waHeight;

			if (w.Left + w.Width > right - margin)
				w.Left = right - w.Width - margin;
			if (w.Top + w.Height > bottom - margin)
				w.Top = bottom - w.Height - margin;

         if (w.Left < waLeft + margin)
				w.Left = waLeft + margin;
			if (w.Top < waTop + margin)
				w.Top = waTop + margin;

       if (w.Width > waWidth - (margin * 2))
				w.Left = waLeft + margin;
			if (w.Height > waHeight - (margin * 2))
				w.Top = waTop + margin;
		}

		private bool ClampToPreferredArea(NoteWindow w)
		{
			if (!Preferences.KeepNotesInsideDesktopArea)
				return false;

			if (Preferences.DesktopAreaLeft is null || Preferences.DesktopAreaTop is null ||
				Preferences.DesktopAreaWidth is null || Preferences.DesktopAreaHeight is null)
				return false;

			double left = Preferences.DesktopAreaLeft.Value;
			double top = Preferences.DesktopAreaTop.Value;
			double width = Preferences.DesktopAreaWidth.Value;
			double height = Preferences.DesktopAreaHeight.Value;

			if (width <= 0 || height <= 0)
				return false;

			double right = left + width;
			double bottom = top + height;

			const double margin = 10;

			if (w.Left + w.Width > right - margin)
				w.Left = right - w.Width - margin;
			if (w.Top + w.Height > bottom - margin)
				w.Top = bottom - w.Height - margin;

			if (w.Left < left + margin)
				w.Left = left + margin;
			if (w.Top < top + margin)
				w.Top = top + margin;

			if (w.Width > width - (margin * 2))
				w.Left = left + margin;
			if (w.Height > height - (margin * 2))
				w.Top = top + margin;

			return true;
		}



		private void QueueSave(int delayMs)
		{
			delayMs = Math.Max(50, delayMs);
			var due = DateTime.UtcNow.AddMilliseconds(delayMs);
			if (!_pendingSaveDueUtc.HasValue || due > _pendingSaveDueUtc.Value)
				_pendingSaveDueUtc = due;

			var remaining = (_pendingSaveDueUtc.Value - DateTime.UtcNow);
			if (remaining < TimeSpan.FromMilliseconds(10))
				remaining = TimeSpan.FromMilliseconds(10);

			_saveTimer.Interval = remaining;
			_saveTimer.Stop();
			_saveTimer.Start();
		}

		private static bool TryRestoreToSavedMonitor(NotePersist note, NoteWindow w)
		{
			var screen = MonitorAffinityService.TryResolveForPersist(note);
			if (screen == null)
				return false;

			var sameMonitor = !string.IsNullOrWhiteSpace(note.MonitorDeviceName)
				&& string.Equals(note.MonitorDeviceName, screen.DeviceName, StringComparison.OrdinalIgnoreCase);

			if (sameMonitor)
			{
				w.Left = note.Left;
				w.Top = note.Top;
				ClampToWorkingArea(w, screen);
				return true;
			}

			if (note.MonitorWorkAreaWidth is null || note.MonitorWorkAreaHeight is null ||
				note.MonitorWorkAreaLeft is null || note.MonitorWorkAreaTop is null)
				return false;

         var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(w);
			double scaleX = Math.Max(0.01, dpi.DpiScaleX);
			double scaleY = Math.Max(0.01, dpi.DpiScaleY);

			double oldLeft = note.MonitorWorkAreaLeft.Value / scaleX;
			double oldTop = note.MonitorWorkAreaTop.Value / scaleY;
			double oldW = Math.Max(1.0, note.MonitorWorkAreaWidth.Value / scaleX);
			double oldH = Math.Max(1.0, note.MonitorWorkAreaHeight.Value / scaleY);
			double rx = (note.Left - oldLeft) / oldW;
			double ry = (note.Top - oldTop) / oldH;

			rx = Math.Max(0.0, Math.Min(1.0, rx));
			ry = Math.Max(0.0, Math.Min(1.0, ry));

			var wa = screen.WorkingArea;
        double waLeft = wa.Left / scaleX;
			double waTop = wa.Top / scaleY;
			double waWidth = wa.Width / scaleX;
			double waHeight = wa.Height / scaleY;

			w.Left = waLeft + (rx * Math.Max(1, waWidth - w.Width));
			w.Top = waTop + (ry * Math.Max(1, waHeight - w.Height));

			ClampToWorkingArea(w, screen);
			return true;
		}

		private void OnDisplaySettingsChanged(object? sender, EventArgs e)
		{
			Dispatcher.BeginInvoke(() =>
			{
				foreach (var w in _windows.Where(x => x.IsLoaded && x.WindowState != WindowState.Minimized))
				{
					if (!ClampToPreferredArea(w))
					{
						var screen = MonitorAffinityService.GetScreenForWindow(w);
						ClampToWorkingArea(w, screen);
					}
				}

				QueueSave(400);
			});
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
				.Select(w => NotePersistMapper.FromWindow(
					w,
					w.GetStuckMode(),
					DateTime.UtcNow))
				.ToList();

			JsonStore.Save(_state);
		}





		public void CreateNewNoteNear(NoteWindow? anchor = null)
		{
         const double newW = 400;
         const double newH = 400;

         double desiredLeft, desiredTop;

         if (anchor != null)
         {
            desiredLeft = anchor.Left + 20;
            desiredTop = anchor.Top + 20;
         } else
         {
            desiredLeft = _nextSpawnLeft;
            desiredTop = _nextSpawnTop;

            _nextSpawnLeft += 24;
            _nextSpawnTop += 24;
         }

         var pos = FindNonOverlappingPosition(desiredLeft, desiredTop, newW, newH);


         var p = new NotePersist
         {
            Left = pos.X,
            Top = pos.Y,
            Width = newW,
            Height = newH,
            Title = "Untitled",
            Text = "",
            ColorKey = nameof(NoteColors.NoteColor.ThreeMYellow),
				StuckMode = 0,
            IsMinimized = false,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow,
				FontFamily = Preferences.BodyFontFamily,
				FontSize = Preferences.BodyFontSize,
         };

			if (Preferences.AlwaysStickNewNotesToDesktop)
			{
				var desktop = DesktopTargetService.TryGetDesktopTarget();
				if (desktop != null)
				{
					p.StuckMode = 2;
					p.StickyTargetPersist = new StickyTargetPersist
					{
						ProcessId = desktop.ProcessId,
						ProcessName = desktop.ProcessName,
						WindowTitle = desktop.WindowTitle,
						ClassName = desktop.ClassName,
						CapturedUtc = desktop.CapturedUtc
					};
				}
			}


         // Add to state so it exists even if we crash before debounce tick
         _state.Notes.Add(p);
			JsonStore.Save(_state);

			SpawnWindow(p);
		}

	}
}
