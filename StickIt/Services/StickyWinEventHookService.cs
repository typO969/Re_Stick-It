using System;
using System.Runtime.InteropServices;

namespace StickIt.Sticky.Services
{
	/// <summary>
	/// Global WinEvent hook for window location changes.
	/// Emits hwnd for EVENT_OBJECT_LOCATIONCHANGE events.
	/// </summary>
	public sealed class StickyWinEventHookService : IDisposable
	{
		public event Action<IntPtr>? TargetMoved;

		private IntPtr _hook = IntPtr.Zero;
		private WinEventProc? _proc; // keep delegate alive
		private bool _disposed;

		private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
		private const uint EVENT_OBJECT_REORDER = 0x8004;
		private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
		private const int OBJID_WINDOW = 0x00000000;

		private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
		private const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

		public void Hook()
		{
			if (_disposed) throw new ObjectDisposedException(nameof(StickyWinEventHookService));
			if (_hook != IntPtr.Zero) return;

			_proc = Callback;

			// Global hook; we filter in the consumer (NoteWindow) by hwnd
			_hook = SetWinEventHook(
				EVENT_SYSTEM_FOREGROUND,
				EVENT_OBJECT_LOCATIONCHANGE,
				IntPtr.Zero,
				_proc,
				0,
				0,
				WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
		}

		public void Unhook()
		{
			if (_hook == IntPtr.Zero) return;
			UnhookWinEvent(_hook);
			_hook = IntPtr.Zero;
		}

		private void Callback(
			IntPtr hWinEventHook,
			uint eventType,
			IntPtr hwnd,
			int idObject,
			int idChild,
			uint dwEventThread,
			uint dwmsEventTime)
		{
			if (eventType != EVENT_OBJECT_LOCATIONCHANGE &&
				eventType != EVENT_SYSTEM_FOREGROUND &&
				eventType != EVENT_OBJECT_REORDER)
				return;
			if (hwnd == IntPtr.Zero) return;
			if (idObject != OBJID_WINDOW) return;
			if (idChild != 0) return;

			var handler = TargetMoved;
			if (handler != null)
				handler(hwnd);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			Unhook();
			_proc = default!;
		}

		#region PInvoke

		private delegate void WinEventProc(
			IntPtr hWinEventHook,
			uint eventType,
			IntPtr hwnd,
			int idObject,
			int idChild,
			uint dwEventThread,
			uint dwmsEventTime);

		[DllImport("user32.dll")]
		private static extern IntPtr SetWinEventHook(
			uint eventMin,
			uint eventMax,
			IntPtr hmodWinEventProc,
			WinEventProc lpfnWinEventProc,
			uint idProcess,
			uint idThread,
			uint dwFlags);

		[DllImport("user32.dll")]
		private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

		#endregion
	}
}
