using System.Windows.Media;

namespace StickIt.Services
{
	public static class AppThemeService
	{
		public const string BackgroundBrushKey = "AppBackgroundBrush";
		public const string ForegroundBrushKey = "AppForegroundBrush";
		public const string PanelBrushKey = "AppPanelBrush";
		public const string BorderBrushKey = "AppBorderBrush";

		public static void ApplyTheme(bool darkMode)
		{
			EnsureResources();
			var resources = System.Windows.Application.Current.Resources;

			if (darkMode)
			{
				resources[BackgroundBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
				resources[ForegroundBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 235, 235));
				resources[PanelBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45));
				resources[BorderBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 70, 70));
			}
			else
			{
				resources[BackgroundBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250));
				resources[ForegroundBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
				resources[PanelBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
				resources[BorderBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
			}
		}

		private static void EnsureResources()
		{
			var resources = System.Windows.Application.Current.Resources;
			resources[BackgroundBrushKey] ??= new SolidColorBrush(System.Windows.Media.Color.FromRgb(250, 250, 250));
			resources[ForegroundBrushKey] ??= new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
			resources[PanelBrushKey] ??= new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
			resources[BorderBrushKey] ??= new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200));
		}
	}
}
