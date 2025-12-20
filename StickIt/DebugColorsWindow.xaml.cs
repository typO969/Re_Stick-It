using System.Windows;
using StickIt.Models;

namespace StickIt
{
	public partial class DebugColorsWindow : Window
	{
		public DebugColorsWindow(NoteModel note)
		{
			InitializeComponent();
			DataContext = note;
		}
	}
}
