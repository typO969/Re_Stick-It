using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace StickIt
{
   public partial class NoteManagerWindow : Window
   {
      private readonly ObservableCollection<NoteManagerItem> _items = new();

      public NoteManagerWindow()
      {
         InitializeComponent();
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
