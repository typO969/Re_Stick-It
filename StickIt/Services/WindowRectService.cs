using System;
using System.Runtime.InteropServices;

namespace StickIt.Sticky.Services
{
	public static class WindowRectService
	{
		public static bool TryGetWindowRect(IntPtr hwnd, out RectD rect)
		{
			rect = default;
			if (hwnd == IntPtr.Zero) return false;

			if (!GetWindowRect(hwnd, out var r))
				return false;

			rect = new RectD(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
			return rect.Width > 0 && rect.Height > 0;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		public readonly struct RectD
		{
			public readonly double X, Y, Width, Height;
			public RectD(double x, double y, double w, double h) { X = x; Y = y; Width = w; Height = h; }
		}

		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
	}
}
