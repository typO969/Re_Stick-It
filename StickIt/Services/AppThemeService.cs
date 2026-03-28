using System.Windows.Media;
using Wpf = System.Windows;
using WpfControls = System.Windows.Controls;
using WpfMedia = System.Windows.Media;

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

    public static void ApplyDialogTheme(Wpf.Window window)
		{
			if (window == null)
				return;

			EnsureResources();
			var resources = System.Windows.Application.Current.Resources;

        var bg = (WpfMedia.Brush)resources[BackgroundBrushKey];
			var fg = (WpfMedia.Brush)resources[ForegroundBrushKey];
			var panel = (WpfMedia.Brush)resources[PanelBrushKey];
			var border = (WpfMedia.Brush)resources[BorderBrushKey];

			window.Background = bg;
			window.Foreground = fg;

         window.Resources[Wpf.SystemColors.WindowBrushKey] = panel;
			window.Resources[Wpf.SystemColors.WindowTextBrushKey] = fg;
			window.Resources[Wpf.SystemColors.ControlBrushKey] = panel;
			window.Resources[Wpf.SystemColors.ControlTextBrushKey] = fg;

			static Wpf.Style CreateStyle(Type targetType, params Wpf.Setter[] setters)
			{
				var style = new Wpf.Style(targetType);
				foreach (var setter in setters)
					style.Setters.Add(setter);
				return style;
			}

			window.Resources[typeof(WpfControls.TextBox)] = CreateStyle(
				typeof(WpfControls.TextBox),
				new Wpf.Setter(WpfControls.Control.BackgroundProperty, panel),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg),
				new Wpf.Setter(WpfControls.Control.BorderBrushProperty, border));

			window.Resources[typeof(WpfControls.ComboBox)] = CreateStyle(
				typeof(WpfControls.ComboBox),
				new Wpf.Setter(WpfControls.Control.BackgroundProperty, panel),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg),
				new Wpf.Setter(WpfControls.Control.BorderBrushProperty, border));

			window.Resources[typeof(WpfControls.CheckBox)] = CreateStyle(
				typeof(WpfControls.CheckBox),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg));

			window.Resources[typeof(WpfControls.RadioButton)] = CreateStyle(
				typeof(WpfControls.RadioButton),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg));

			window.Resources[typeof(WpfControls.GroupBox)] = CreateStyle(
				typeof(WpfControls.GroupBox),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg),
				new Wpf.Setter(WpfControls.Control.BorderBrushProperty, border));

			window.Resources[typeof(WpfControls.TabControl)] = CreateStyle(
				typeof(WpfControls.TabControl),
				new Wpf.Setter(WpfControls.Control.BackgroundProperty, bg),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg),
				new Wpf.Setter(WpfControls.Control.BorderBrushProperty, border));

			window.Resources[typeof(WpfControls.TabItem)] = CreateStyle(
				typeof(WpfControls.TabItem),
				new Wpf.Setter(WpfControls.Control.BackgroundProperty, panel),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg),
				new Wpf.Setter(WpfControls.Control.BorderBrushProperty, border));

			window.Resources[typeof(WpfControls.ListView)] = CreateStyle(
				typeof(WpfControls.ListView),
				new Wpf.Setter(WpfControls.Control.BackgroundProperty, panel),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg),
				new Wpf.Setter(WpfControls.Control.BorderBrushProperty, border));

			window.Resources[typeof(WpfControls.DataGrid)] = CreateStyle(
				typeof(WpfControls.DataGrid),
				new Wpf.Setter(WpfControls.Control.BackgroundProperty, panel),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg),
				new Wpf.Setter(WpfControls.Control.BorderBrushProperty, border));

			window.Resources[typeof(WpfControls.DataGridCell)] = CreateStyle(
				typeof(WpfControls.DataGridCell),
				new Wpf.Setter(WpfControls.Control.BackgroundProperty, panel),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg),
				new Wpf.Setter(WpfControls.Control.BorderBrushProperty, border));

			window.Resources[typeof(WpfControls.DataGridRow)] = CreateStyle(
				typeof(WpfControls.DataGridRow),
				new Wpf.Setter(WpfControls.Control.BackgroundProperty, panel),
				new Wpf.Setter(WpfControls.Control.ForegroundProperty, fg));
		}
	}
}
