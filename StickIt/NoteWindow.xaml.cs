using System.Windows;
using System.Windows.Controls;

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

			TitleBar.MouseLeftButtonDown += (_, __) => DragMove();

			// Right-click menu
			ContextMenu = BuildContextMenu();
		}

		private ContextMenu BuildContextMenu()
		{
			var menu = new ContextMenu();

			var colors = new MenuItem { Header = "Color" };
			foreach (var kv in NoteColors.Hex)
			{
				var item = new MenuItem { Header = $"{kv.Key} ({kv.Value})" };
				item.Click += (_, __) => _note.ColorKey = kv.Key;
				colors.Items.Add(item);
			}

			menu.Items.Add(colors);
			return menu;
		}
	}
}
