using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StickIt.Sticky.Services
{
	public static class StickyHitTestService
	{
		public static StickyTargetInfo? GetTopmostWindowUnderPoint(
	int screenX,
	int screenY,
	int excludePid,
	IntPtr excludeHwnd)
		{
			var pt = new POINT { X = screenX, Y = screenY };

			IntPtr hwnd = WindowFromPoint(pt);
			if (hwnd == IntPtr.Zero) return null;

			// Walk down the z-order until we find a usable window
			for (int i = 0; i < 80 && hwnd != IntPtr.Zero; i++)
			{
				var root = GetAncestor(hwnd, GA_ROOT);
				if (root != IntPtr.Zero) hwnd = root;

				// Skip our own note window
				if (excludeHwnd != IntPtr.Zero && hwnd == excludeHwnd)
				{
					hwnd = GetWindow(hwnd, GW_HWNDNEXT);
					continue;
				}

				if (!IsWindowVisible(hwnd))
				{
					hwnd = GetWindow(hwnd, GW_HWNDNEXT);
					continue;
				}

				GetWindowThreadProcessId(hwnd, out var pid);
				if ((int) pid == excludePid)
				{
					hwnd = GetWindow(hwnd, GW_HWNDNEXT);
					continue;
				}

				var title = GetWindowTextSafe(hwnd);
				if (string.IsNullOrWhiteSpace(title))
				{
					hwnd = GetWindow(hwnd, GW_HWNDNEXT);
					continue;
				}

				var cls = GetClassNameSafe(hwnd);

				string? procName = null;
				try { procName = Process.GetProcessById((int) pid).ProcessName; } catch { }

				return new StickyTargetInfo
				{
					Hwnd = hwnd,
					ProcessId = (int) pid,
					ProcessName = procName,
					WindowTitle = title,
					ClassName = cls,
					CapturedUtc = DateTime.UtcNow
				};
			}

			return null;
		}


		// ---- Win32 ----

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT { public int X; public int Y; }

		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(POINT pt);

		[DllImport("user32.dll")]
		private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

		private const uint GA_ROOT = 2;

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern int GetWindowTextLength(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

		private const uint GW_HWNDNEXT = 2;


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
