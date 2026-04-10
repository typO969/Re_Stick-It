using System;
using System.Reflection;

using StickIt.Sticky;

namespace StickIt.Tests
{
   public class NoteWindowStickyClassificationTests
   {
      [Fact]
      public void IsDesktopLikeTarget_ReturnsTrue_ForDesktopHostClasses()
      {
         Assert.True(InvokeIsDesktopLikeTarget(new StickyTargetInfo { ClassName = "Progman" }));
         Assert.True(InvokeIsDesktopLikeTarget(new StickyTargetInfo { ClassName = "workerw" }));
         Assert.True(InvokeIsDesktopLikeTarget(new StickyTargetInfo { ClassName = "#32769" }));
         Assert.True(InvokeIsDesktopLikeTarget(new StickyTargetInfo { ClassName = "Desktop" }));
      }

      [Fact]
      public void IsDesktopLikeTarget_ReturnsFalse_ForNonDesktopClassesOrNull()
      {
         Assert.False(InvokeIsDesktopLikeTarget(new StickyTargetInfo { ClassName = "Notepad" }));
         Assert.False(InvokeIsDesktopLikeTarget(new StickyTargetInfo { ClassName = "Shell_TrayWnd" }));
         Assert.False(InvokeIsDesktopLikeTarget(new StickyTargetInfo { ClassName = "" }));
         Assert.False(InvokeIsDesktopLikeTarget(null));
      }

      [Fact]
      public void IsDesktopLikeTarget_TrimsWhitespaceInClassName()
      {
         Assert.True(InvokeIsDesktopLikeTarget(new StickyTargetInfo { ClassName = "  WorkerW  " }));
      }

      private static bool InvokeIsDesktopLikeTarget(StickyTargetInfo? target)
      {
         var method = typeof(NoteWindow).GetMethod("IsDesktopLikeTarget", BindingFlags.NonPublic | BindingFlags.Static);
         Assert.NotNull(method);
         return (bool)method!.Invoke(null, new object?[] { target })!;
      }
   }
}
