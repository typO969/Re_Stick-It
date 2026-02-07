using System;
using System.Runtime.InteropServices;

namespace StickIt.Sticky.Services
{
	public static class WindowMoveService
	{
		public static bool MoveWindow(IntPtr hwnd, int x, int y, IntPtr insertAfter)
		{
			const uint SWP_NOSIZE = 0x0001;
			const uint SWP_NOACTIVATE = 0x0010;
			const uint SWP_SHOWWINDOW = 0x0040;

			return SetWindowPos(hwnd, insertAfter, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
		}

		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
		private static extern bool SetWindowPos(
			IntPtr hWnd,
			IntPtr hWndInsertAfter,
			int X,
			int Y,
			int cx,
			int cy,
			uint uFlags);

	}
}
