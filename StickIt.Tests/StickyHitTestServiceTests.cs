using System.Reflection;

using StickIt.Sticky.Services;

namespace StickIt.Tests
{
   public class StickyHitTestServiceTests
   {
      [Fact]
      public void IsDesktopHost_RecognizesDesktopClassesOnly()
      {
         Assert.True(InvokeIsDesktopHost("Progman"));
         Assert.True(InvokeIsDesktopHost("WorkerW"));
         Assert.False(InvokeIsDesktopHost("Shell_TrayWnd"));
         Assert.False(InvokeIsDesktopHost("Notepad"));
      }

      [Fact]
      public void IsIgnoredHost_RecognizesTaskbarAndTrayHosts()
      {
         Assert.True(InvokeIsIgnoredHost("Shell_TrayWnd"));
         Assert.True(InvokeIsIgnoredHost("Shell_SecondaryTrayWnd"));
         Assert.True(InvokeIsIgnoredHost("NotifyIconOverflowWindow"));
         Assert.False(InvokeIsIgnoredHost("Progman"));
      }

      private static bool InvokeIsDesktopHost(string className)
      {
         var method = typeof(StickyHitTestService).GetMethod("IsDesktopHost", BindingFlags.NonPublic | BindingFlags.Static);
         Assert.NotNull(method);
         return (bool)method!.Invoke(null, new object[] { className })!;
      }

      private static bool InvokeIsIgnoredHost(string className)
      {
         var method = typeof(StickyHitTestService).GetMethod("IsIgnoredHost", BindingFlags.NonPublic | BindingFlags.Static);
         Assert.NotNull(method);
         return (bool)method!.Invoke(null, new object[] { className })!;
      }
   }
}
