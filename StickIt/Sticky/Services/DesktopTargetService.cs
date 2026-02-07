using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StickIt.Sticky.Services
{
	public static class DesktopTargetService
	{
		// Best-effort “desktop” target: Shell window / Progman / WorkerW
		public static StickyTargetInfo? TryGetDesktopTarget()
		{
			// 1) Shell window (often works)
			var hwnd = GetShellWindow();
			if (IsGood(hwnd)) return Build(hwnd);

			// 2) Progman
			hwnd = FindWindow("Progman", null);
			if (IsGood(hwnd)) return Build(hwnd);

			// 3) WorkerW (common desktop host)
			hwnd = FindWindow("WorkerW", null);
			if (IsGood(hwnd)) return Build(hwnd);

			return null;
		}

		private static bool IsGood(IntPtr hwnd) => hwnd != IntPtr.Zero && IsWindow(hwnd);

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
