using System;
using System.Drawing;
using System.Windows.Forms;

namespace StickIt.Services
{
	public sealed class TrayIconService : IDisposable
	{
		private readonly NotifyIcon _ni;

		public TrayIconService(
			Icon icon,
			Action onNewNote,
			Action onMinimizeAll,
			Action onRestoreAll,
			Action onSaveAll,
			Action onShowNotes,
			Action onExit)
		{
			_ni = new NotifyIcon
			{
				Icon = icon,
				Text = "StickIt",
				Visible = true,
				ContextMenuStrip = BuildMenu(onNewNote, onMinimizeAll, onRestoreAll, onSaveAll, onShowNotes, onExit)
			};

			_ni.MouseUp += (_, e) =>
			{
				// Left click: create a new note
				if (e.Button == MouseButtons.Left)
					onNewNote();
			};


			_ni.DoubleClick += (_, __) => onShowNotes();

		}


		private static ContextMenuStrip BuildMenu(
				Action onNewNote,
				Action onMinimizeAll,
				Action onRestoreAll,
				Action onSaveAll,
				Action onShowNotes,
				Action onExit)
		{
			var menu = new ContextMenuStrip();

			menu.Items.Add(new ToolStripMenuItem("New note  \tCtrl+N", null, (_, __) => onNewNote()));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Minimize all", null, (_, __) => onMinimizeAll()));
			menu.Items.Add(new ToolStripMenuItem("Restore all", null, (_, __) => onRestoreAll()));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Show notes", null, (_, __) => onShowNotes()));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Save all notes now", null, (_, __) => onSaveAll()));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, __) => onExit()));

			return menu;
		}


		public void ShowStillRunningNotice()
		{
			// Windows may suppress notifications depending on user settings; this is best-effort.
			_ni.BalloonTipTitle = "StickIt";
			_ni.BalloonTipText = "Notice: Re_Stickit is still running in the tray.";
			_ni.BalloonTipIcon = ToolTipIcon.Info;
			_ni.ShowBalloonTip(4000);
		}


		public void Dispose()
		{
			_ni.Visible = false;
			_ni.Dispose();
		}
	}
}
