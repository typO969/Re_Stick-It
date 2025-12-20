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

			foreach (var n in _state.Notes)
			{
				if (string.IsNullOrWhiteSpace(n.ColorKey))
					n.ColorKey = nameof(NoteColors.NoteColor.ThreeMYellow);
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


		private void SpawnWindow(NotePersist note)
		{
			var model = new StickIt.Models.NoteModel();
			var w = new NoteWindow(model);

			w.SetNoteId(note.Id);
			w.SetTitle(note.Title);
			w.SetText(note.Text);
			w.SetColorKey(note.ColorKey);
			w.SetStuckMode(note.StuckMode);
			// Apply persisted content + background
			w.SetNoteId(note.Id); // small helper you’ll add (see 2.3)
			w.SetText(note.Text); // small helper you’ll add
			w.SetColorKey(note.ColorKey);   // small helper you’ll add

			ClampToVirtualScreen(w);

			w.SetFontFamily(note.FontFamily);
			w.SetFontSize(note.FontSize);
			w.SetNoteId(note.Id);
			w.SetTitle(note.Title);
			w.SetColorKey(note.ColorKey);
			w.SetStuckMode(note.StuckMode);

			// geometry
			w.Left = note.Left;
			w.Top = note.Top;
			w.Width = note.Width <= 0 ? 380 : note.Width;
			w.Height = note.Height <= 0 ? 320 : note.Height;

			// Apply persisted geometry
			w.Left = note.Left;
			w.Top = note.Top;
			w.Width = note.Width <= 0 ? 380 : note.Width;
			w.Height = note.Height <= 0 ? 320 : note.Height;

			// Wire autosave triggers
			w.LocationChanged += (_, __) => QueueSave();
			w.SizeChanged += (_, __) => QueueSave();
			w.NoteTextChanged += (_, __) => QueueSave(); // your own event from the NoteWindow
			w.Closed += (_, __) =>
			{
				_windows.Remove(w);
				QueueSave();
			};

			// minimized (apply after Show to be reliable in WPF)
			w.Loaded += (_, __) =>
			{
				if (note.IsMinimized)
					w.SetIsMinimized(true);
			};
			// Prefer RTF if available, else fallback to plain text
			if (!string.IsNullOrWhiteSpace(note.Rtf))
				w.SetRtf(note.Rtf);
			else
				w.SetText(note.Text);


			w.StateChanged += (_, __) => QueueSave();

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
			// Rebuild state from currently open windows (source of truth)
			_state.Notes = _windows
				.Where(w => w.IsLoaded)
				.Select(w => new NotePersist
				{
					 Id = w.NoteId,
					 Left = w.Left,
					 Top = w.Top,
					 Width = w.Width,
					 Height = w.Height,
					 FontFamily = w.GetFontFamily(),
					 FontSize = w.GetFontSize(),

					 Title = w.GetTitle(),
					 Rtf = w.GetRtf(),
					 Text = w.GetText(),

					 ColorKey = w.GetColorKey().ToString(),

					 StuckMode = w.GetStuckMode(),
					 IsMinimized = w.GetIsMinimized(),
				})
				.ToList();

			JsonStore.Save(_state);
		}

		private void PersistAllWindowsOnShutdown()
		{
			// Rebuild state from currently open windows
			_state.Notes = _windows
				 .Where(w => w.IsLoaded)
				 .Select(w =>
				 {
					 var stuck = w.GetStuckMode();

					 // Normalize "stuck to app" on quit
					 if (stuck == StuckModeStuckToApp)
						 stuck = StuckModeUnstuck; // or: StuckModeAlwaysOnTop

					 return new NotePersist
					 {
						 Id = w.NoteId,

						 Left = w.Left,
						 Top = w.Top,
						 Width = w.Width,
						 Height = w.Height,

						 Title = w.GetTitle(),

						 // Prefer RTF for fidelity, keep Text for fallback/search/debug
						 Rtf = w.GetRtf(),
						 Text = w.GetText(),

						 ColorKey = w.GetColorKey().ToString(),

						 StuckMode = stuck,
						 IsMinimized = w.GetIsMinimized(),

						 FontFamily = w.GetFontFamily(),
						 FontSize = w.GetFontSize(),
					 };
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
				IsMinimized = false
			};

			// Add to state so it exists even if we crash before debounce tick
			_state.Notes.Add(p);
			JsonStore.Save(_state);

			SpawnWindow(p);
		}

	}
}
