using System;
using System.Runtime.InteropServices;

namespace StickIt.Services
{
   public static class VirtualDesktopManagerService
   {
      private static readonly Lazy<IVirtualDesktopManager?> Manager = new(() =>
      {
         try
         {
            var type = Type.GetTypeFromCLSID(VirtualDesktopManagerClsid, throwOnError: false);
            return type == null ? null : (IVirtualDesktopManager?)Activator.CreateInstance(type);
         }
         catch
         {
            return null;
         }
      });

      private static readonly Guid VirtualDesktopManagerClsid = new("aa509086-5ca9-4c25-8f95-589d3c07b48a");

      public static bool IsWindowOnCurrentVirtualDesktop(IntPtr hwnd)
      {
         if (hwnd == IntPtr.Zero)
            return true;

         var manager = Manager.Value;
         if (manager == null)
            return true;

         try
         {
            return manager.IsWindowOnCurrentVirtualDesktop(hwnd, out var onCurrent) == 0 && onCurrent;
         }
         catch
         {
            return true;
         }
      }

      [ComImport]
      [Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
      [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
      private interface IVirtualDesktopManager
      {
         int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out bool onCurrentDesktop);

         int GetWindowDesktopId(IntPtr topLevelWindow, out Guid desktopId);

         int MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
      }
   }
}
