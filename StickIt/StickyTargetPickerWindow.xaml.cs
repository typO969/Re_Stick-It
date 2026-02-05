using System.Collections.Generic;
using System.Windows;

using StickIt.Services;

namespace StickIt.Sticky
{
	public partial class StickyTargetPickerWindow : Window
	{
		public StickyTargetInfo? SelectedTarget { get; private set; }

		public StickyTargetPickerWindow()
		{
			InitializeComponent();
			Refresh();

		}

		private void Refresh()
		{
			List<StickyTargetInfo> items = WindowEnumerationService.GetTopLevelWindows();
			lv.ItemsSource = items;
		}

		private void btnRefresh_Click(object sender, RoutedEventArgs e) => Refresh();

		private void btnSelect_Click(object sender, RoutedEventArgs e) => SelectAndClose();

		private void lv_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => SelectAndClose();

		private void SelectAndClose()
		{
			SelectedTarget = lv.SelectedItem as StickyTargetInfo;
			if (SelectedTarget == null) return;

			DialogResult = true;
			Close();
		}
	}
}
