using System.Linq;
using System.Windows;
using System.Windows.Forms;

using StickIt.Persistence;

namespace StickIt.Services
{
	public static class MonitorAffinityService
	{
		public static void CaptureForWindow(NotePersist persist, Window w)
		{
			var rect = new System.Drawing.Rectangle(
				(int)w.Left,
				(int)w.Top,
				(int)System.Math.Max(1, w.Width),
				(int)System.Math.Max(1, w.Height));

			var screen = Screen.FromRectangle(rect);
			persist.MonitorDeviceName = screen.DeviceName;
			persist.MonitorWorkAreaLeft = screen.WorkingArea.Left;
			persist.MonitorWorkAreaTop = screen.WorkingArea.Top;
			persist.MonitorWorkAreaWidth = screen.WorkingArea.Width;
			persist.MonitorWorkAreaHeight = screen.WorkingArea.Height;
		}

		public static Screen? TryGetByDeviceName(string? deviceName)
		{
			if (string.IsNullOrWhiteSpace(deviceName)) return null;
			return Screen.AllScreens.FirstOrDefault(s => s.DeviceName == deviceName);
		}
	}
}
