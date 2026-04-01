using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using StickIt.Services;

namespace StickIt
{
   public partial class NoteManagerWindow : Window
   {
      private readonly ObservableCollection<NoteManagerItem> _items = new();

      public NoteManagerWindow()
      {
         InitializeComponent();
         AppThemeService.ApplyDialogTheme(this);
         NotesGrid.ItemsSource = _items;
         Loaded += (_, __) => RefreshItems();
         Activated += (_, __) => RefreshItems();
      }

      private App AppInstance => (App)System.Windows.Application.Current;

      private void RefreshItems()
      {
         var windows = AppInstance.GetOpenWindowsSnapshot();

         _items.Clear();
         foreach (var w in windows)
         {
            _items.Add(NoteManagerItem.FromWindow(w));
         }
      }

      private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshItems();

      private void Done_Click(object sender, RoutedEventArgs e) => Close();

      private NoteWindow? GetSelectedWindow()
      {
         return (NotesGrid.SelectedItem as NoteManagerItem)?.Window;
      }

      private void AddNote_Click(object sender, RoutedEventArgs e)
      {
         AppInstance.CreateNewNoteNear(GetSelectedWindow());
         RefreshItems();
      }

      private void SaveNotesNow_Click(object sender, RoutedEventArgs e)
      {
         AppInstance.SaveAllNotesNow();
      }

      private void SyncNow_Click(object sender, RoutedEventArgs e)
      {
         if (AppInstance.TrySyncNow(out var message))
            System.Windows.MessageBox.Show(this, message, "Sync", MessageBoxButton.OK, MessageBoxImage.Information);
         else
            System.Windows.MessageBox.Show(this, message, "Sync", MessageBoxButton.OK, MessageBoxImage.Warning);
      }

      private void PullNow_Click(object sender, RoutedEventArgs e)
      {
         if (AppInstance.TryPullFromSync(out var message))
            System.Windows.MessageBox.Show(this, message, "Sync Pull", MessageBoxButton.OK, MessageBoxImage.Information);
         else
            System.Windows.MessageBox.Show(this, message, "Sync Pull", MessageBoxButton.OK, MessageBoxImage.Warning);

         RefreshItems();
      }

      private void PushNow_Click(object sender, RoutedEventArgs e)
      {
         if (AppInstance.TryPushToSync(out var message))
            System.Windows.MessageBox.Show(this, message, "Sync Push", MessageBoxButton.OK, MessageBoxImage.Information);
         else
            System.Windows.MessageBox.Show(this, message, "Sync Push", MessageBoxButton.OK, MessageBoxImage.Warning);
      }

      private void ExportSelected_Click(object sender, RoutedEventArgs e)
      {
         var selected = GetSelectedWindow();
         if (selected == null)
         {
            System.Windows.MessageBox.Show(this, "Select a note first.", "Export note", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
         }

         selected.ExportNoteFromManager();
      }

      private void LoadIntoSelected_Click(object sender, RoutedEventArgs e)
      {
         var selected = GetSelectedWindow();
         if (selected == null)
         {
            System.Windows.MessageBox.Show(this, "Select a note first.", "Load note", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
         }

         selected.ImportNoteFromManager();
         RefreshItems();
      }

      private void Show_Click(object sender, RoutedEventArgs e)
      {
         if (sender is not FrameworkElement element || element.Tag is not NoteWindow w) return;

         w.Show();
         if (w.WindowState == WindowState.Minimized)
            w.WindowState = WindowState.Normal;

         w.Activate();
         w.Topmost = true;
         w.Topmost = false;

         AppInstance.QueueSaveFromWindow();
         RefreshItems();
      }

      private void Minimize_Click(object sender, RoutedEventArgs e)
      {
         if (sender is not FrameworkElement element || element.Tag is not NoteWindow w) return;

         w.WindowState = WindowState.Minimized;
         AppInstance.QueueSaveFromWindow();
         RefreshItems();
      }

      private void Restore_Click(object sender, RoutedEventArgs e)
      {
         if (sender is not FrameworkElement element || element.Tag is not NoteWindow w) return;

         w.Show();
         w.WindowState = WindowState.Normal;
         w.Activate();

         AppInstance.QueueSaveFromWindow();
         RefreshItems();
      }

      private void Close_Click(object sender, RoutedEventArgs e)
      {
         if (sender is not FrameworkElement element || element.Tag is not NoteWindow w) return;

         w.Close();
         AppInstance.QueueSaveFromWindow();
         RefreshItems();
      }
   }

   public sealed class NoteManagerItem
   {
      public string Title { get; init; } = string.Empty;
      public string Color { get; init; } = string.Empty;
      public string Sticky { get; init; } = string.Empty;
      public string Modified { get; init; } = string.Empty;
      public string IsMinimized { get; init; } = string.Empty;
      public NoteWindow Window { get; init; } = null!;

      public static NoteManagerItem FromWindow(NoteWindow w)
      {
         return new NoteManagerItem
         {
            Title = string.IsNullOrWhiteSpace(w.GetTitle()) ? "Untitled" : w.GetTitle(),
            Color = w.GetColorKey().ToString(),
            Sticky = StickyLabel(w.GetStuckMode()),
            Modified = FormatDate(w.GetModifiedUtc()),
            IsMinimized = w.GetIsMinimized() ? "Yes" : "No",
            Window = w
         };
      }

      private static string StickyLabel(int mode) => mode switch
      {
         0 => "Not stuck",
         1 => "Always on top",
         2 => "Stuck to window",
         _ => $"Unknown ({mode})"
      };

      private static string FormatDate(DateTime dt)
      {
         if (dt == default)
            return "Unknown";

         return dt.ToLocalTime().ToString("g");
      }
   }
}
