using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StickIt.Sticky.Services
{
	public static class DesktopTargetService
	{
    public static StickyTargetInfo? TryGetDesktopTargetAtPoint(int screenX, int screenY)
		{
			var pt = new POINT { X = screenX, Y = screenY };
			var hwnd = WindowFromPoint(pt);
			if (hwnd != IntPtr.Zero)
			{
				var root = GetAncestor(hwnd, GA_ROOT);
				if (root != IntPtr.Zero)
					hwnd = root;

				var cls = GetClassNameSafe(hwnd);
				if (IsDesktopClass(cls) && IsGood(hwnd))
					return Build(hwnd);
			}

			return TryGetDesktopTarget();
		}

		// Best-effort “desktop” target: Shell window / Progman / WorkerW
		public static StickyTargetInfo? TryGetDesktopTarget()
		{
       // 1) Progman (desktop manager)
			var hwnd = FindWindow("Progman", null);
			if (IsGood(hwnd)) return Build(hwnd);

       // 2) WorkerW (common desktop host)
			hwnd = FindWindow("WorkerW", null);
			if (IsGood(hwnd)) return Build(hwnd);

			// 3) Shell window fallback
			hwnd = GetShellWindow();
			if (IsGood(hwnd)) return Build(hwnd);

			return null;
		}

		private static bool IsGood(IntPtr hwnd) => hwnd != IntPtr.Zero && IsWindow(hwnd);

		private static bool IsDesktopClass(string className)
			=> string.Equals(className, "Progman", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "WorkerW", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "#32769", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "Desktop", StringComparison.OrdinalIgnoreCase);

		private static StickyTargetInfo Build(IntPtr hwnd)
		{
			GetWindowThreadProcessId(hwnd, out var pid);

			string? procName = null;
			try { procName = Process.GetProcessById((int) pid).ProcessName; } catch { }

			return new StickyTargetInfo
			{
				Hwnd = hwnd,
				ProcessId = (int) pid,
				ProcessName = procName,
				WindowTitle = GetWindowTextSafe(hwnd),
				ClassName = GetClassNameSafe(hwnd),
				CapturedUtc = DateTime.UtcNow
			};
		}

		// ---- Win32 ----

		[DllImport("user32.dll")]
		private static extern IntPtr GetShellWindow();

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT { public int X; public int Y; }

		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(POINT pt);

		[DllImport("user32.dll")]
		private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

		private const uint GA_ROOT = 2;

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

		[DllImport("user32.dll")]
		private static extern bool IsWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

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
