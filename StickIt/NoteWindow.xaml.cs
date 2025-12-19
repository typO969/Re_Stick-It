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
		private readonly NoteModel _note;

		public NoteWindow(NoteModel note)
		{
			InitializeComponent();
			_note = note;
			DataContext = _note;

			ControlBar.MouseLeftButtonDown += (_, __) => DragMove();
		}

		private void ColorMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem menuItem)
				return;

			if (menuItem.Tag is string tag && Enum.TryParse(tag, out NoteColors.NoteColor color))
				_note.ColorKey = color;
		}

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
  
		private void btnMinimize_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void txtNoteTitle_GotFocus(object sender, RoutedEventArgs e)
		{
			// Add your logic here, or leave empty if not needed
		}

		private void txtNoteTitle_LostFocus(object sender, RoutedEventArgs e)
		{
			// Add logic here if needed, or leave empty if not required
		}

		private void txtNoteContent_GotFocus(object sender, RoutedEventArgs e)
		{
			// Add your logic here if needed, or leave empty if not required
		}

		private void txtNoteContent_LostFocus(object sender, RoutedEventArgs e)
		{
			// TODO: Add logic to handle when the RichTextBox loses focus, if needed.
		}

        private void Note_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // You can implement logic here, for example, to focus the RichTextBox or handle drag, etc.
            // For now, this is a placeholder to resolve the event handler error.
        }
    }
}
