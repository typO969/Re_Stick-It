using System.Windows;
using StickIt.Models;
using StickIt.Services;

namespace StickIt
{
	public partial class DebugColorsWindow : Window
	{
		public DebugColorsWindow(NoteModel note)
		{
			InitializeComponent();
        AppThemeService.ApplyDialogTheme(this);
			DataContext = note;
		}
	}
}
