using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

using StickIt.Models;
using StickIt.Persistence;
using StickIt.Services;

namespace StickIt
{
	public partial class PreferencesWindow : Window
	{
		private readonly PreferencesViewModel _viewModel;

		public PreferencesWindow(AppPreferences preferences)
		{
			InitializeComponent();
			_viewModel = PreferencesViewModel.FromPreferences(preferences);
        _viewModel.LoadSkins(AppInstance.GetBuiltInSkinsSnapshot(), AppInstance.GetUserSkinsSnapshot());
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
        AppInstance.SaveUserSkins(_viewModel.GetUserSkinPersists());
		}

		private void SkinNew_Click(object sender, RoutedEventArgs e)
		{
			_viewModel.AddNewSkin();
		}

		private void SkinDuplicate_Click(object sender, RoutedEventArgs e)
		{
			_viewModel.DuplicateSelectedSkin();
		}

		private void SkinDelete_Click(object sender, RoutedEventArgs e)
		{
			_viewModel.DeleteSelectedSkin();
		}

		private void SkinBrowseImage_Click(object sender, RoutedEventArgs e)
		{
       if (_viewModel.SelectedSkin is null)
				return;

			if (!_viewModel.SelectedSkinIsEditable)
				_viewModel.DuplicateSelectedSkin();

			if (_viewModel.SelectedSkin is null || !_viewModel.SelectedSkinIsEditable)
				return;

        var dlg = new Microsoft.Win32.OpenFileDialog
			{
				Title = "Select paper background image",
				Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All files|*.*",
				CheckFileExists = true,
				Multiselect = false
			};

			if (dlg.ShowDialog(this) != true)
				return;

			_viewModel.SelectedSkin.PaperImagePath = dlg.FileName;
		}
	}

	public sealed class EditableSkinViewModel : INotifyPropertyChanged
	{
		public string DisplayName => IsBuiltIn ? $"{Name} (built-in)" : Name;

		public string Id { get => _id; set { if (SetField(ref _id, value)) OnPropertyChanged(nameof(DisplayName)); } }
		public string Name { get => _name; set { if (SetField(ref _name, value)) OnPropertyChanged(nameof(DisplayName)); } }
		public string PaperHex { get => _paperHex; set => SetField(ref _paperHex, value); }
		public string ControlBarHex { get => _controlBarHex; set => SetField(ref _controlBarHex, value); }
		public string ButtonHex { get => _buttonHex; set => SetField(ref _buttonHex, value); }
		public string TextHex { get => _textHex; set => SetField(ref _textHex, value); }
		public string? HighlightHex { get => _highlightHex; set => SetField(ref _highlightHex, value); }
		public string? OutlineHex { get => _outlineHex; set => SetField(ref _outlineHex, value); }
      public string? PaperImagePath { get => _paperImagePath; set => SetField(ref _paperImagePath, value); }
		public bool IsBuiltIn { get => _isBuiltIn; set { if (SetField(ref _isBuiltIn, value)) OnPropertyChanged(nameof(DisplayName)); } }

		private string _id = string.Empty;
		private string _name = string.Empty;
		private string _paperHex = "#F7E03D";
		private string _controlBarHex = "#EAC52C";
		private string _buttonHex = "#C59A00";
		private string _textHex = "#1C1C1C";
		private string? _highlightHex;
		private string? _outlineHex;
      private string? _paperImagePath;
		private bool _isBuiltIn;

		public event PropertyChangedEventHandler? PropertyChanged;

		public static EditableSkinViewModel FromSkin(NoteSkin skin, bool isBuiltIn)
		{
			return new EditableSkinViewModel
			{
				Id = skin.Id,
				Name = skin.Name,
				PaperHex = skin.PaperHex,
				ControlBarHex = skin.ControlBarHex,
				ButtonHex = skin.ButtonHex,
				TextHex = skin.TextHex,
				HighlightHex = skin.HighlightHex,
				OutlineHex = skin.OutlineHex,
            PaperImagePath = skin.PaperImagePath,
				IsBuiltIn = isBuiltIn
			};
		}

		public EditableSkinViewModel CloneAsCustom()
		{
			return new EditableSkinViewModel
			{
				Id = $"custom-{Guid.NewGuid():N}"[..15],
				Name = string.IsNullOrWhiteSpace(Name) ? "Custom skin" : $"{Name} copy",
				PaperHex = PaperHex,
				ControlBarHex = ControlBarHex,
				ButtonHex = ButtonHex,
				TextHex = TextHex,
				HighlightHex = HighlightHex,
				OutlineHex = OutlineHex,
          PaperImagePath = PaperImagePath,
				IsBuiltIn = false
			};
		}

		public NoteSkin ToSkin()
		{
			return new NoteSkin
			{
				Id = Id,
				Name = string.IsNullOrWhiteSpace(Name) ? Id : Name,
				PaperHex = PaperHex,
				ControlBarHex = ControlBarHex,
				ButtonHex = ButtonHex,
				TextHex = TextHex,
				HighlightHex = string.IsNullOrWhiteSpace(HighlightHex) ? null : HighlightHex,
           OutlineHex = string.IsNullOrWhiteSpace(OutlineHex) ? null : OutlineHex,
				PaperImagePath = string.IsNullOrWhiteSpace(PaperImagePath) ? null : PaperImagePath
			};
		}

		private void OnPropertyChanged([CallerMemberName] string? name = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

		private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
		{
			if (Equals(field, value))
				return false;

			field = value;
			OnPropertyChanged(name);
			return true;
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

		public ObservableCollection<EditableSkinViewModel> SkinLibrary { get; } = new();
		public EditableSkinViewModel? SelectedSkin
		{
			get => _selectedSkin;
			set
			{
				if (SetField(ref _selectedSkin, value))
					OnPropertyChanged(nameof(SelectedSkinIsEditable));
			}
		}

		public bool SelectedSkinIsEditable => SelectedSkin is { IsBuiltIn: false };

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
		private EditableSkinViewModel? _selectedSkin;

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

		public void LoadSkins(IEnumerable<NoteSkin> builtInSkins, IEnumerable<NoteSkin> userSkins)
		{
			SkinLibrary.Clear();

			foreach (var builtIn in builtInSkins.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase))
				SkinLibrary.Add(EditableSkinViewModel.FromSkin(builtIn, isBuiltIn: true));

			foreach (var custom in userSkins.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase))
				SkinLibrary.Add(EditableSkinViewModel.FromSkin(custom, isBuiltIn: false));

			SelectedSkin = SkinLibrary.FirstOrDefault();
		}

		public void AddNewSkin()
		{
			var index = SkinLibrary.Count(s => !s.IsBuiltIn) + 1;
			var skin = new EditableSkinViewModel
			{
				Id = $"custom-{Guid.NewGuid():N}"[..15],
				Name = $"Custom {index}",
				PaperHex = "#F7E03D",
				ControlBarHex = "#EAC52C",
				ButtonHex = "#C59A00",
				TextHex = "#1C1C1C",
				HighlightHex = "#FFE86A",
				OutlineHex = "#9A7700",
          PaperImagePath = null,
				IsBuiltIn = false
			};

			SkinLibrary.Add(skin);
			SelectedSkin = skin;
		}

		public void DuplicateSelectedSkin()
		{
			if (SelectedSkin == null)
				return;

			var skin = SelectedSkin.CloneAsCustom();
			SkinLibrary.Add(skin);
			SelectedSkin = skin;
		}

		public void DeleteSelectedSkin()
		{
			if (SelectedSkin == null || SelectedSkin.IsBuiltIn)
				return;

			var idx = SkinLibrary.IndexOf(SelectedSkin);
			SkinLibrary.Remove(SelectedSkin);
			SelectedSkin = idx >= 0 && idx < SkinLibrary.Count ? SkinLibrary[idx] : SkinLibrary.FirstOrDefault();
		}

		public IReadOnlyList<NoteSkinPersist> GetUserSkinPersists()
		{
			return SkinLibrary
				.Where(s => !s.IsBuiltIn)
				.Select(s => SkinService.ToPersist(s.ToSkin()))
				.ToList();
		}

		private void OnPropertyChanged([CallerMemberName] string? name = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
		{
			if (Equals(field, value))
           return false;

			field = value;
			OnPropertyChanged(name);

			if (name == nameof(DesktopAreaLeft) || name == nameof(DesktopAreaTop) || name == nameof(DesktopAreaWidth) || name == nameof(DesktopAreaHeight))
				RefreshDesktopAreaSummary();

			return true;
		}
	}
}
