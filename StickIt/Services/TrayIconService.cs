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
            if (e.Button == MouseButtons.Left)
               System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(onNewNote));
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

         menu.Items.Add(new ToolStripMenuItem("New note  \tCtrl+N", null, (_, __) =>
         {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(onNewNote));
         }));
         menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Minimize all", null, (_, __) =>
         {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(onMinimizeAll));
         }));
			menu.Items.Add(new ToolStripMenuItem("Restore all", null, (_, __) =>
         {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(onRestoreAll));
         })); 
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Show notes", null, (_, __) =>
         {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(onShowNotes));
         })); 
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Save all notes now", null, (_, __) =>
         {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(onSaveAll));
         }));
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, __) =>
         {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(onExit));
         }));

			return menu;
		}

		public void ShowTrimmedLoadNotice(int opened, int total)
		{
			_ni.BalloonTipTitle = "StickIt";
			_ni.BalloonTipText = $"Loaded {opened} of {total} notes for safety. Open more by creating new notes (or restore from backup).";
			_ni.BalloonTipIcon = ToolTipIcon.Warning;
			_ni.ShowBalloonTip(5000);
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
