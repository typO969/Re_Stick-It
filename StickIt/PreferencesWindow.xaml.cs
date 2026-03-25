using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

using StickIt.Persistence;

namespace StickIt
{
	public partial class PreferencesWindow : Window
	{
		private readonly PreferencesViewModel _viewModel;

		public PreferencesWindow(AppPreferences preferences)
		{
			InitializeComponent();
			_viewModel = PreferencesViewModel.FromPreferences(preferences);
			DataContext = _viewModel;
		}

		private App AppInstance => (App)System.Windows.Application.Current;

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			ApplyChanges();
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

		private void DesktopArea_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new DesktopAreaWindow(_viewModel.DesktopAreaLeft, _viewModel.DesktopAreaTop, _viewModel.DesktopAreaWidth, _viewModel.DesktopAreaHeight)
			{
				Owner = this
			};

			if (dlg.ShowDialog() != true || dlg.SelectedArea == null)
				return;

			_viewModel.DesktopAreaLeft = dlg.SelectedArea.Left;
			_viewModel.DesktopAreaTop = dlg.SelectedArea.Top;
			_viewModel.DesktopAreaWidth = dlg.SelectedArea.Width;
			_viewModel.DesktopAreaHeight = dlg.SelectedArea.Height;
			_viewModel.RefreshDesktopAreaSummary();
		}

		private void ApplyChanges()
		{
			var updated = _viewModel.ToPreferences();
			AppInstance.ApplyPreferences(updated, persist: true);
		}
	}

	public sealed class PreferencesViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<System.Windows.Media.FontFamily> FontFamilies { get; }
		public ObservableCollection<double> FontSizes { get; }

		public bool RunOnStartup { get => _runOnStartup; set => SetField(ref _runOnStartup, value); }
		public bool DarkMode { get => _darkMode; set => SetField(ref _darkMode, value); }
		public bool ShowTaskbarIcon { get => _showTaskbarIcon; set => SetField(ref _showTaskbarIcon, value); }
		public bool ShowTrayIcon { get => _showTrayIcon; set => SetField(ref _showTrayIcon, value); }

		public bool AlwaysStickNewNotesToDesktop { get => _alwaysStickNewNotesToDesktop; set => SetField(ref _alwaysStickNewNotesToDesktop, value); }
		public bool SnapNotesToGrid { get => _snapNotesToGrid; set => SetField(ref _snapNotesToGrid, value); }
		public bool KeepNotesInsideDesktopArea { get => _keepNotesInsideDesktopArea; set => SetField(ref _keepNotesInsideDesktopArea, value); }
		public bool ConfirmOnDelete { get => _confirmOnDelete; set => SetField(ref _confirmOnDelete, value); }
		public bool HideNotesOnShowDesktop { get => _hideNotesOnShowDesktop; set => SetField(ref _hideNotesOnShowDesktop, value); }
		public bool TreatNotesAsTopLevelWindows { get => _treatNotesAsTopLevelWindows; set => SetField(ref _treatNotesAsTopLevelWindows, value); }
		public bool SeparateNotesPerDesktopArea { get => _separateNotesPerDesktopArea; set => SetField(ref _separateNotesPerDesktopArea, value); }

		public System.Windows.Media.FontFamily TitleFontFamily { get => _titleFontFamily; set => SetField(ref _titleFontFamily, value); }
		public double TitleFontSize { get => _titleFontSize; set => SetField(ref _titleFontSize, value); }
		public System.Windows.Media.FontFamily BodyFontFamily { get => _bodyFontFamily; set => SetField(ref _bodyFontFamily, value); }
		public double BodyFontSize { get => _bodyFontSize; set => SetField(ref _bodyFontSize, value); }
		public bool ShowDateAlongTitle { get => _showDateAlongTitle; set => SetField(ref _showDateAlongTitle, value); }
		public bool EnableDropShadow { get => _enableDropShadow; set => SetField(ref _enableDropShadow, value); }

		public double? DesktopAreaLeft { get => _desktopAreaLeft; set => SetField(ref _desktopAreaLeft, value); }
		public double? DesktopAreaTop { get => _desktopAreaTop; set => SetField(ref _desktopAreaTop, value); }
		public double? DesktopAreaWidth { get => _desktopAreaWidth; set => SetField(ref _desktopAreaWidth, value); }
		public double? DesktopAreaHeight { get => _desktopAreaHeight; set => SetField(ref _desktopAreaHeight, value); }

		public string DesktopAreaSummary { get => _desktopAreaSummary; private set => SetField(ref _desktopAreaSummary, value); }

		private bool _runOnStartup;
		private bool _darkMode;
		private bool _showTaskbarIcon = true;
		private bool _showTrayIcon = true;
		private bool _alwaysStickNewNotesToDesktop;
		private bool _snapNotesToGrid;
		private bool _keepNotesInsideDesktopArea;
		private bool _confirmOnDelete;
		private bool _hideNotesOnShowDesktop;
		private bool _treatNotesAsTopLevelWindows = true;
		private bool _separateNotesPerDesktopArea;
		private System.Windows.Media.FontFamily _titleFontFamily = new("Helvetica");
		private double _titleFontSize = 19.0;
		private System.Windows.Media.FontFamily _bodyFontFamily = new("Segoe UI");
		private double _bodyFontSize = 14.0;
		private bool _showDateAlongTitle;
		private bool _enableDropShadow = true;
		private double? _desktopAreaLeft;
		private double? _desktopAreaTop;
		private double? _desktopAreaWidth;
		private double? _desktopAreaHeight;
		private string _desktopAreaSummary = "Not set";

		public event PropertyChangedEventHandler? PropertyChanged;

		private PreferencesViewModel()
		{
			FontFamilies = new ObservableCollection<System.Windows.Media.FontFamily>(Fonts.SystemFontFamilies.OrderBy(f => f.Source));
			FontSizes = new ObservableCollection<double>(new[] { 8d, 9d, 10d, 11d, 12d, 13d, 14d, 15d, 16d, 18d, 19d, 20d, 22d, 24d, 28d, 32d });
		}

		public static PreferencesViewModel FromPreferences(AppPreferences prefs)
		{
			var vm = new PreferencesViewModel
			{
				RunOnStartup = prefs.RunOnStartup,
				DarkMode = prefs.DarkMode,
				ShowTaskbarIcon = prefs.ShowTaskbarIcon,
				ShowTrayIcon = prefs.ShowTrayIcon,
				AlwaysStickNewNotesToDesktop = prefs.AlwaysStickNewNotesToDesktop,
				SnapNotesToGrid = prefs.SnapNotesToGrid,
				KeepNotesInsideDesktopArea = prefs.KeepNotesInsideDesktopArea,
				ConfirmOnDelete = prefs.ConfirmOnDelete,
				HideNotesOnShowDesktop = prefs.HideNotesOnShowDesktop,
				TreatNotesAsTopLevelWindows = prefs.TreatNotesAsTopLevelWindows,
				SeparateNotesPerDesktopArea = prefs.SeparateNotesPerDesktopArea,
				TitleFontFamily = new System.Windows.Media.FontFamily(prefs.TitleFontFamily),
				TitleFontSize = prefs.TitleFontSize,
				BodyFontFamily = new System.Windows.Media.FontFamily(prefs.BodyFontFamily),
				BodyFontSize = prefs.BodyFontSize,
				ShowDateAlongTitle = prefs.ShowDateAlongTitle,
				EnableDropShadow = prefs.EnableDropShadow,
				DesktopAreaLeft = prefs.DesktopAreaLeft,
				DesktopAreaTop = prefs.DesktopAreaTop,
				DesktopAreaWidth = prefs.DesktopAreaWidth,
				DesktopAreaHeight = prefs.DesktopAreaHeight
			};

			vm.RefreshDesktopAreaSummary();
			return vm;
		}

		public AppPreferences ToPreferences()
		{
			return new AppPreferences
			{
				RunOnStartup = RunOnStartup,
				DarkMode = DarkMode,
				ShowTaskbarIcon = ShowTaskbarIcon,
				ShowTrayIcon = ShowTrayIcon,
				AlwaysStickNewNotesToDesktop = AlwaysStickNewNotesToDesktop,
				SnapNotesToGrid = SnapNotesToGrid,
				KeepNotesInsideDesktopArea = KeepNotesInsideDesktopArea,
				ConfirmOnDelete = ConfirmOnDelete,
				HideNotesOnShowDesktop = HideNotesOnShowDesktop,
				TreatNotesAsTopLevelWindows = TreatNotesAsTopLevelWindows,
				SeparateNotesPerDesktopArea = SeparateNotesPerDesktopArea,
				TitleFontFamily = TitleFontFamily.Source,
				TitleFontSize = TitleFontSize,
				BodyFontFamily = BodyFontFamily.Source,
				BodyFontSize = BodyFontSize,
				ShowDateAlongTitle = ShowDateAlongTitle,
				EnableDropShadow = EnableDropShadow,
				DesktopAreaLeft = DesktopAreaLeft,
				DesktopAreaTop = DesktopAreaTop,
				DesktopAreaWidth = DesktopAreaWidth,
				DesktopAreaHeight = DesktopAreaHeight
			};
		}

		public void RefreshDesktopAreaSummary()
		{
			if (DesktopAreaLeft is null || DesktopAreaTop is null || DesktopAreaWidth is null || DesktopAreaHeight is null)
			{
				DesktopAreaSummary = "Not set";
				return;
			}

			DesktopAreaSummary = $"Left {DesktopAreaLeft:0}, Top {DesktopAreaTop:0}, {DesktopAreaWidth:0} x {DesktopAreaHeight:0}";
		}

		private void OnPropertyChanged([CallerMemberName] string? name = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
		{
			if (Equals(field, value))
				return;

			field = value;
			OnPropertyChanged(name);

			if (name == nameof(DesktopAreaLeft) || name == nameof(DesktopAreaTop) || name == nameof(DesktopAreaWidth) || name == nameof(DesktopAreaHeight))
				RefreshDesktopAreaSummary();
		}
	}
}
