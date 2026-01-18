using System;
using System.Linq;
using System.Windows;
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


		// Debounce saving so we don’t write on every keystroke/mouse move
		private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

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

			Exit += (_, __) =>
			{
				_saveTimer.Stop();
				PersistAllWindowsOnShutdown();
			};

			SessionEnding += (_, __) =>
			{
				_saveTimer.Stop();
				PersistAllWindowsOnShutdown();
			};


		}

		public void QueueSaveFromWindow() => QueueSave();

		public void MinimizeAllNotes()
		{
			foreach (var w in _windows.Where(w => w.IsLoaded))
				w.SetIsMinimized(true);
			QueueSave();
		}

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
			w.SizeChanged += (_, __) => QueueSave();
			w.StateChanged += (_, __) => QueueSave();
			w.NoteTextChanged += (_, __) => QueueSave();
			w.Closed += (_, __) =>
			{
				_windows.Remove(w);
				QueueSave();
			};

			_windows.Add(w);
			w.Show();
		}


		private static void ClampToVirtualScreen(NoteWindow w)
		{
			// Virtual desktop bounds (multi-monitor)
			double left = SystemParameters.VirtualScreenLeft;
			double top = SystemParameters.VirtualScreenTop;
			double width = SystemParameters.VirtualScreenWidth;
			double height = SystemParameters.VirtualScreenHeight;

			double right = left + width;
			double bottom = top + height;

			// If the window is completely off-screen, snap it inward.
			const double margin = 20;

			if (w.Left > right - margin) w.Left = right - w.Width - margin;
			if (w.Top > bottom - margin) w.Top = bottom - w.Height - margin;

			if (w.Left + w.Width < left + margin) w.Left = left + margin;
			if (w.Top + w.Height < top + margin) w.Top = top + margin;
		}


		private void QueueSave()
		{
			_saveTimer.Stop();
			_saveTimer.Start();
		}

		private void PersistAllWindows()
		{
			_state.Notes = _windows
				.Where(w => w.IsLoaded)
				.Select(w =>
					NotePersistMapper.FromWindow(
						w,
				w.GetStuckMode(),
				DateTime.UtcNow
					))
				.ToList();

			JsonStore.Save(_state);

		}


		private void PersistAllWindowsOnShutdown()
		{
			_state.Notes = _windows
			 .Where(w => w.IsLoaded)
			 .Select(w =>
			 {
				 var stuck = w.GetStuckMode();
				 if (stuck == StuckModeStuckToApp)
					 stuck = StuckModeUnstuck; // or AlwaysOnTop

				 return NotePersistMapper.FromWindow(
					  w,
					  stuck,
					  DateTime.UtcNow
				 );
			 })
			 .ToList();

			JsonStore.Save(_state);

		}


		public void CreateNewNoteNear(NoteWindow? anchor = null)
		{
			var p = new NotePersist
			{
				Left = anchor != null ? anchor.Left + 30 : 200,
				Top = anchor != null ? anchor.Top + 30 : 200,
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
