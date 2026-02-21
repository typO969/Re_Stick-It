using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace StickIt
{
	public partial class FontSettingsWindow : Window
	{
		private readonly FontSettingsViewModel _viewModel;

		public FontSettingsData? Settings { get; private set; }

		public FontSettingsWindow(FontSettingsData initial)
		{
			InitializeComponent();
			_viewModel = FontSettingsViewModel.FromSettings(initial ?? new FontSettingsData());
			DataContext = _viewModel;
		}

		private void Ok_Click(object sender, RoutedEventArgs e)
		{
			Settings = _viewModel.ToSettings();
			DialogResult = true;
			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}

	public sealed class FontSettingsData
	{
		public string FontFamily { get; set; } = "Segoe UI";
		public double FontSize { get; set; } = 14.0;
		public bool IsBold { get; set; }
		public bool IsItalic { get; set; }
		public bool IsUnderline { get; set; }
		public System.Windows.Media.Color Color { get; set; } = System.Windows.Media.Colors.Black;
		public bool ApplyToSelection { get; set; } = true;
		public bool ApplyToEntireNote { get; set; }
	}

	public sealed class FontSettingsViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<System.Windows.Media.FontFamily> FontFamilies { get; }
		public ObservableCollection<double> FontSizes { get; }
		public ObservableCollection<ColorItem> Colors { get; }

		public System.Windows.Media.FontFamily FontFamily { get => _fontFamily; set => SetField(ref _fontFamily, value); }
		public double FontSize { get => _fontSize; set => SetField(ref _fontSize, value); }
		public bool IsBold { get => _isBold; set => SetField(ref _isBold, value); }
		public bool IsItalic { get => _isItalic; set => SetField(ref _isItalic, value); }
		public bool IsUnderline { get => _isUnderline; set => SetField(ref _isUnderline, value); }
		public ColorItem SelectedColor { get => _selectedColor; set => SetField(ref _selectedColor, value); }

		public bool ApplyToSelection
		{
			get => _applyToSelection;
			set
			{
				if (SetField(ref _applyToSelection, value))
				{
					if (value)
						_applyToEntireNote = false;
					OnPropertyChanged(nameof(ApplyToDefaults));
					OnPropertyChanged(nameof(ApplyToEntireNote));
				}
			}
		}

		public bool ApplyToEntireNote
		{
			get => _applyToEntireNote;
			set
			{
				if (SetField(ref _applyToEntireNote, value))
				{
					if (value)
						_applyToSelection = false;
					OnPropertyChanged(nameof(ApplyToDefaults));
					OnPropertyChanged(nameof(ApplyToSelection));
				}
			}
		}

		public bool ApplyToDefaults
		{
			get => !_applyToSelection && !_applyToEntireNote;
			set
			{
				if (value)
				{
					ApplyToSelection = false;
					ApplyToEntireNote = false;
				}
			}
		}

		public FontWeight PreviewFontWeight => IsBold ? FontWeights.Bold : FontWeights.Normal;
		public System.Windows.FontStyle PreviewFontStyle => IsItalic ? FontStyles.Italic : FontStyles.Normal;
		public TextDecorationCollection? PreviewTextDecorations => IsUnderline ? TextDecorations.Underline : null;

		private System.Windows.Media.FontFamily _fontFamily = new("Segoe UI");
		private double _fontSize = 14.0;
		private bool _isBold;
		private bool _isItalic;
		private bool _isUnderline;
		private ColorItem _selectedColor = ColorItem.FromColor("Black", System.Windows.Media.Colors.Black);
		private bool _applyToSelection = true;
		private bool _applyToEntireNote;

		public event PropertyChangedEventHandler? PropertyChanged;

		private FontSettingsViewModel()
		{
			FontFamilies = new ObservableCollection<System.Windows.Media.FontFamily>(Fonts.SystemFontFamilies.OrderBy(f => f.Source));
			FontSizes = new ObservableCollection<double>(new[] { 8d, 9d, 10d, 11d, 12d, 13d, 14d, 15d, 16d, 18d, 19d, 20d, 22d, 24d, 28d, 32d, 36d, 48d, 72d });
			Colors = new ObservableCollection<ColorItem>(ColorItem.Defaults);
		}

		public static FontSettingsViewModel FromSettings(FontSettingsData settings)
		{
			var vm = new FontSettingsViewModel
			{
				FontFamily = new System.Windows.Media.FontFamily(settings.FontFamily),
				FontSize = settings.FontSize,
				IsBold = settings.IsBold,
				IsItalic = settings.IsItalic,
				IsUnderline = settings.IsUnderline,
				ApplyToSelection = settings.ApplyToSelection,
				ApplyToEntireNote = settings.ApplyToEntireNote
			};

			vm.SelectedColor = vm.Colors.FirstOrDefault(c => c.Color == settings.Color) ?? vm.Colors.First();
			return vm;
		}

		public FontSettingsData ToSettings()
		{
			return new FontSettingsData
			{
				FontFamily = FontFamily.Source,
				FontSize = FontSize,
				IsBold = IsBold,
				IsItalic = IsItalic,
				IsUnderline = IsUnderline,
				Color = SelectedColor.Color,
				ApplyToSelection = ApplyToSelection,
				ApplyToEntireNote = ApplyToEntireNote
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

			if (name == nameof(IsBold))
				OnPropertyChanged(nameof(PreviewFontWeight));
			if (name == nameof(IsItalic))
				OnPropertyChanged(nameof(PreviewFontStyle));
			if (name == nameof(IsUnderline))
				OnPropertyChanged(nameof(PreviewTextDecorations));

			return true;
		}
	}

	public sealed class ColorItem
	{
		public string Name { get; init; } = string.Empty;
		public System.Windows.Media.Color Color { get; init; }
		public SolidColorBrush Brush { get; init; } = new(System.Windows.Media.Colors.Black);

		public static ColorItem FromColor(string name, System.Windows.Media.Color color)
		{
			return new ColorItem
			{
				Name = name,
				Color = color,
				Brush = new SolidColorBrush(color)
			};
		}

		public static readonly ColorItem[] Defaults =
		{
			FromColor("Black", System.Windows.Media.Colors.Black),
			FromColor("Dark Gray", System.Windows.Media.Colors.DarkGray),
			FromColor("Gray", System.Windows.Media.Colors.Gray),
			FromColor("Light Gray", System.Windows.Media.Colors.LightGray),
			FromColor("White", System.Windows.Media.Colors.White),
			FromColor("Red", System.Windows.Media.Colors.Firebrick),
			FromColor("Orange", System.Windows.Media.Colors.DarkOrange),
			FromColor("Yellow", System.Windows.Media.Colors.Goldenrod),
			FromColor("Green", System.Windows.Media.Colors.SeaGreen),
			FromColor("Blue", System.Windows.Media.Colors.SteelBlue),
			FromColor("Purple", System.Windows.Media.Colors.MediumPurple)
		};
	}
}
