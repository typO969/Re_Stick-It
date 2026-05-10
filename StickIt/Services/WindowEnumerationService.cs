using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using StickIt.Sticky;

namespace StickIt.Services
{
	public static class WindowEnumerationService
	{
		public static List<StickyTargetInfo> GetTopLevelWindows()
		{
			var list = new List<StickyTargetInfo>();

			EnumWindows((hwnd, lParam) =>
			{
				if (hwnd == IntPtr.Zero) return true;

				// Basic visibility + "real window" checks
				if (!IsWindowVisible(hwnd)) return true;
				if (GetWindow(hwnd, GW_OWNER) != IntPtr.Zero) return true; // owned/popups
				if (GetWindowTextLength(hwnd) == 0) return true;           // no title = usually not user-facing

				// Exclude tool windows (Alt-Tab hidden)
				var exStyle = (long) GetWindowLongPtr(hwnd, GWL_EXSTYLE);
				if ((exStyle & WS_EX_TOOLWINDOW) == WS_EX_TOOLWINDOW) return true;

				if (!VirtualDesktopManagerService.IsWindowOnCurrentVirtualDesktop(hwnd))
					return true;

				// Exclude our own process windows
				GetWindowThreadProcessId(hwnd, out var pid);
				if (pid == Process.GetCurrentProcess().Id) return true;

				// Read title + class
				var title = GetWindowTextSafe(hwnd);
				if (string.IsNullOrWhiteSpace(title)) return true;

            var cls = GetClassNameSafe(hwnd);
				if (IsIgnoredClass(cls))
					return true;

				// Process name (best-effort)
				string? procName = null;
				try { procName = Process.GetProcessById((int) pid).ProcessName; } catch { }

				list.Add(new StickyTargetInfo
				{
					Hwnd = hwnd,
					ProcessId = (int) pid,
					ProcessName = procName,
					WindowTitle = title,
					ClassName = cls,
					CapturedUtc = DateTime.UtcNow
				});

				return true;
			}, IntPtr.Zero);

			return list;
		}

     private static bool IsIgnoredClass(string className)
			=> string.Equals(className, "Windows.UI.Core.CoreWindow", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "ApplicationFrameWindow", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "ApplicationFrameHost", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "TextInputHost", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "Windows.UI.Input.InputSite.WindowClass", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "Windows.UI.Composition.DesktopWindowContentBridge", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "XamlExplorerHostIslandWindow", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "ImmersiveLauncher", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "ShellExperienceHost", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "Start", StringComparison.OrdinalIgnoreCase);

		// --------- Win32 ---------

		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		private const int GWL_EXSTYLE = -20;
		private const long WS_EX_TOOLWINDOW = 0x00000080L;
		private const uint GW_OWNER = 4;

		// GetWindowLongPtr shim (.NET 4.0 safe)
		private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 8) return GetWindowLongPtr64(hWnd, nIndex);
			return new IntPtr(GetWindowLong32(hWnd, nIndex));
		}

		[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
		private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
		private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

		private static string GetWindowTextSafe(IntPtr hwnd)
		{
			var len = GetWindowTextLength(hwnd);
			if (len <= 0) return string.Empty;

			var sb = new StringBuilder(len + 1);
			GetWindowText(hwnd, sb, sb.Capacity);
			return sb.ToString();
		}

		private static string GetClassNameSafe(IntPtr hwnd)
		{
			var sb = new StringBuilder(256);
			GetClassName(hwnd, sb, sb.Capacity);
			return sb.ToString();
		}
	}
}
