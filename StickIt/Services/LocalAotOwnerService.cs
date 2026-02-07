using System;
using System.Runtime.InteropServices;

namespace StickIt.Services
{
	/// <summary>
	/// Implements "local AOT" by setting a WPF window's Win32 owner to a target HWND.
	/// Owned windows stay above their owner without being globally TopMost.
	/// </summary>
	public static class LocalAotOwnerService
	{
		// GWL_HWNDPARENT is the owner (not parent) for top-level windows.
		private const int GWL_HWNDPARENT = -8;

		public static IntPtr GetOwner(IntPtr hwnd)
		{
			return GetWindowLongPtr(hwnd, GWL_HWNDPARENT);
		}

		public static void SetOwner(IntPtr hwnd, IntPtr ownerHwnd)
		{
			SetWindowLongPtr(hwnd, GWL_HWNDPARENT, ownerHwnd);
		}

		public static void ClearOwner(IntPtr hwnd)
		{
			SetOwner(hwnd, IntPtr.Zero);
		}

		#region PInvoke (32/64 safe)

		private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 8) return GetWindowLongPtr64(hWnd, nIndex);
			return new IntPtr(GetWindowLong32(hWnd, nIndex));
		}

		private static void SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
		{
			if (IntPtr.Size == 8) SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
			else SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32());
		}

		[DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
		private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
		private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
		private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
		private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

		#endregion
	}
}
