using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Navigation;

namespace StickIt
{
   public partial class aboutWindow : Window
   {
      public aboutWindow()
      {
         InitializeComponent();
         LoadAssemblyVersion();
      }

      private void LoadAssemblyVersion()
      {
         // Dynamically extracts version string from your Assembly info configuration
         var version = Assembly.GetExecutingAssembly().GetName().Version;
         txtVersion.Text = $"Version {version?.Major ?? 1}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
      }

      private void CloseButton_Click(object sender, RoutedEventArgs e)
      {
         this.Close();
      }

      private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
      {
         try
         {
            // Safely launches the user's default browser targeting the hyperlink Uri
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
         }
         catch (Exception ex)
         {
            Debug.WriteLine($"Failed to open hyperlink: {ex.Message}");
         }
      }
   }
}

