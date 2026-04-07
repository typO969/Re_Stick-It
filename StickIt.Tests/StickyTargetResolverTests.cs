using System;
using System.Reflection;

using StickIt.Persistence;
using StickIt.Sticky;

namespace StickIt.Tests
{
   public class StickyTargetResolverTests
   {
      [Fact]
      public void Score_WithPidProcessClassAndExactTitle_HasHighScore()
      {
         var persist = new StickyTargetPersist
         {
            ProcessId = 1234,
            ProcessName = "notepad",
            ClassName = "Notepad",
            WindowTitle = "notes.txt"
         };

         var candidate = new StickyTargetInfo
         {
            Hwnd = IntPtr.Zero,
            ProcessId = 1234,
            ProcessName = "Notepad",
            ClassName = "Notepad",
            WindowTitle = "notes.txt"
         };

         var score = InvokeScore(candidate, persist);

         Assert.Equal(14, score);
      }

      [Fact]
      public void Score_WithPartialTitleMatch_GetsLowerTitleScoreThanExact()
      {
         var persist = new StickyTargetPersist
         {
            ProcessId = 100,
            ProcessName = "app",
            ClassName = "MainWin",
            WindowTitle = "Document - ProjectA"
         };

         var exact = new StickyTargetInfo
         {
            ProcessId = 100,
            ProcessName = "app",
            ClassName = "MainWin",
            WindowTitle = "Document - ProjectA"
         };

         var partial = new StickyTargetInfo
         {
            ProcessId = 100,
            ProcessName = "app",
            ClassName = "MainWin",
            WindowTitle = "ProjectA"
         };

         var exactScore = InvokeScore(exact, persist);
         var partialScore = InvokeScore(partial, persist);

         Assert.True(exactScore > partialScore);
         Assert.Equal(2, exactScore - partialScore);
      }

      [Fact]
      public void Score_WithAnchorAndZeroHwnd_DoesNotGetAnchorBonus()
      {
         var persist = new StickyTargetPersist
         {
            ProcessId = 1,
            TargetAnchorX = 100,
            TargetAnchorY = 100
         };

         var candidate = new StickyTargetInfo
         {
            Hwnd = IntPtr.Zero,
            ProcessId = 1
         };

         var score = InvokeScore(candidate, persist);

         Assert.Equal(3, score);
      }

      [Fact]
      public void IsDesktopClass_RecognizesDesktopHostClasses()
      {
         Assert.True(InvokeIsDesktopClass("Progman"));
         Assert.True(InvokeIsDesktopClass("WorkerW"));
         Assert.True(InvokeIsDesktopClass("#32769"));
         Assert.True(InvokeIsDesktopClass("Desktop"));
         Assert.False(InvokeIsDesktopClass("Notepad"));
      }

      private static int InvokeScore(StickyTargetInfo candidate, StickyTargetPersist persist)
      {
         var method = typeof(StickyTargetResolver).GetMethod("Score", BindingFlags.NonPublic | BindingFlags.Static);
         Assert.NotNull(method);
         return (int)method!.Invoke(null, new object[] { candidate, persist })!;
      }

      private static bool InvokeIsDesktopClass(string className)
      {
         var method = typeof(StickyTargetResolver).GetMethod("IsDesktopClass", BindingFlags.NonPublic | BindingFlags.Static);
         Assert.NotNull(method);
         return (bool)method!.Invoke(null, new object?[] { className })!;
      }
   }
}
