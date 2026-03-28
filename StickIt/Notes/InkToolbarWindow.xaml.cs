using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinForms = System.Windows.Forms;
using WpfBorder = System.Windows.Controls.Border;
using WpfSlider = System.Windows.Controls.Slider;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace StickIt
{
	public partial class InkToolbarWindow : Window
	{
		public event EventHandler? PenRequested;
		public event EventHandler? EraseRequested;
		public event EventHandler? ClearRequested;
		public event EventHandler? DoneRequested;
		public event Action<double>? ThicknessChanged;
    public event Action<System.Windows.Media.Color>? ColorChanged;

		private bool _suppressThicknessEvents;
      private WpfSlider? _sldThickness;
		private WpfTextBox? _txtThickness;
		private WpfBorder? _colorSwatch;

		public InkToolbarWindow()
		{
			InitializeComponent();
        _sldThickness = FindName("sldThickness") as WpfSlider;
			_txtThickness = FindName("txtThickness") as WpfTextBox;
			_colorSwatch = FindName("ColorSwatch") as WpfBorder;
		}

		public void SetMode(bool inkEnabled, bool eraseEnabled)
		{
			btnPen.FontWeight = (inkEnabled && !eraseEnabled) ? FontWeights.Bold : FontWeights.Normal;
			btnErase.FontWeight = (inkEnabled && eraseEnabled) ? FontWeights.Bold : FontWeights.Normal;
		}

		public void SetThickness(double thickness)
		{
			var clamped = Math.Max(1.0, Math.Min(10.0, thickness));
			if (_sldThickness == null || _txtThickness == null)
				return;

			_suppressThicknessEvents = true;
			try
			{
          _sldThickness.Value = clamped;
				_txtThickness.Text = clamped.ToString("0.#");
			}
			finally
			{
				_suppressThicknessEvents = false;
			}
		}

      public void SetColor(System.Windows.Media.Color color)
		{
        if (_colorSwatch != null)
				_colorSwatch.Background = new SolidColorBrush(color);
		}

		private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				DragMove();
		}

		private void Pen_Click(object sender, RoutedEventArgs e) => PenRequested?.Invoke(this, EventArgs.Empty);
		private void Erase_Click(object sender, RoutedEventArgs e) => EraseRequested?.Invoke(this, EventArgs.Empty);
		private void Clear_Click(object sender, RoutedEventArgs e) => ClearRequested?.Invoke(this, EventArgs.Empty);
		private void Done_Click(object sender, RoutedEventArgs e) => DoneRequested?.Invoke(this, EventArgs.Empty);

		private void sldThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (_suppressThicknessEvents)
				return;

			var v = Math.Round(e.NewValue, 1);
       if (_txtThickness != null)
				_txtThickness.Text = v.ToString("0.#");
			ThicknessChanged?.Invoke(v);
		}

		private void txtThickness_LostFocus(object sender, RoutedEventArgs e)
		{
       if (_txtThickness == null || _sldThickness == null)
				return;

			if (!double.TryParse(_txtThickness.Text, out var parsed))
				parsed = _sldThickness.Value;

			parsed = Math.Max(1.0, Math.Min(10.0, parsed));
			SetThickness(parsed);
			ThicknessChanged?.Invoke(parsed);
		}

		private void Color_Click(object sender, RoutedEventArgs e)
		{
			using var dlg = new WinForms.ColorDialog
			{
				AllowFullOpen = true,
				FullOpen = true
			};

			if (dlg.ShowDialog() != WinForms.DialogResult.OK)
				return;

        var selected = System.Windows.Media.Color.FromArgb(dlg.Color.A, dlg.Color.R, dlg.Color.G, dlg.Color.B);
			SetColor(selected);
			ColorChanged?.Invoke(selected);
		}
	}
}
