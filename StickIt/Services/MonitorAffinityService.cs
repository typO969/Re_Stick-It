using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using StickIt.Persistence;
using StickIt.Sticky.Services;

namespace StickIt.Services
{
	public static class MonitorAffinityService
	{
		public static void CaptureForWindow(NotePersist persist, Window w)
		{
			var screen = GetScreenForWindow(w);
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(w);
			double scaleX = Math.Max(0.01, dpi.DpiScaleX);
			double scaleY = Math.Max(0.01, dpi.DpiScaleY);

			persist.MonitorDeviceName = screen.DeviceName;
         persist.MonitorWorkAreaLeft = screen.WorkingArea.Left / scaleX;
			persist.MonitorWorkAreaTop = screen.WorkingArea.Top / scaleY;
			persist.MonitorWorkAreaWidth = screen.WorkingArea.Width / scaleX;
			persist.MonitorWorkAreaHeight = screen.WorkingArea.Height / scaleY;
		}

		public static Screen? TryGetByDeviceName(string? deviceName)
		{
			if (string.IsNullOrWhiteSpace(deviceName)) return null;
			return Screen.AllScreens.FirstOrDefault(s => s.DeviceName == deviceName);
		}

		public static Screen? TryGetByWorkArea(double? left, double? top, double? width, double? height)
		{
			if (left is null || top is null || width is null || height is null)
				return null;

			var target = new System.Drawing.Rectangle(
				(int)left.Value,
				(int)top.Value,
				(int)Math.Max(1, width.Value),
				(int)Math.Max(1, height.Value));

			Screen? best = null;
			int bestOverlap = 0;

			foreach (var screen in Screen.AllScreens)
			{
				var overlap = System.Drawing.Rectangle.Intersect(target, screen.WorkingArea);
				int area = overlap.Width * overlap.Height;
				if (area > bestOverlap)
				{
					bestOverlap = area;
					best = screen;
				}
			}

			if (best != null && bestOverlap > 0)
				return best;

			var centerX = target.Left + (target.Width / 2.0);
			var centerY = target.Top + (target.Height / 2.0);
			double bestDistance = double.MaxValue;

			foreach (var screen in Screen.AllScreens)
			{
				var wa = screen.WorkingArea;
				double clampedX = Math.Max(wa.Left, Math.Min(wa.Right, centerX));
				double clampedY = Math.Max(wa.Top, Math.Min(wa.Bottom, centerY));
				double dx = centerX - clampedX;
				double dy = centerY - clampedY;
				double dist = (dx * dx) + (dy * dy);
				if (dist < bestDistance)
				{
					bestDistance = dist;
					best = screen;
				}
			}

			return best;
		}

		public static Screen? TryResolveForPersist(NotePersist persist)
		{
			var screen = TryGetByDeviceName(persist.MonitorDeviceName);
			if (screen != null)
				return screen;

			return TryGetByWorkArea(
				persist.MonitorWorkAreaLeft,
				persist.MonitorWorkAreaTop,
				persist.MonitorWorkAreaWidth,
				persist.MonitorWorkAreaHeight);
		}

		public static Screen GetScreenForWindow(Window w)
		{
			var rect = GetWindowRectPx(w);
			return Screen.FromRectangle(rect);
		}

		private static System.Drawing.Rectangle GetWindowRectPx(Window w)
		{
			var hwnd = new WindowInteropHelper(w).Handle;
			if (hwnd != IntPtr.Zero && WindowRectService.TryGetWindowRect(hwnd, out var rect))
				return new System.Drawing.Rectangle((int)rect.X, (int)rect.Y,
					(int)Math.Max(1, rect.Width), (int)Math.Max(1, rect.Height));

			if (!w.IsLoaded || PresentationSource.FromVisual(w) == null)
			{
				var b = w.RestoreBounds;
				return new System.Drawing.Rectangle((int)b.Left, (int)b.Top,
					(int)Math.Max(1, b.Width), (int)Math.Max(1, b.Height));
			}

			var origin = w.PointToScreen(new System.Windows.Point(0, 0));
			var source = PresentationSource.FromVisual(w);
			var transform = source?.CompositionTarget?.TransformToDevice ?? System.Windows.Media.Matrix.Identity;
			var widthPx = Math.Max(1, w.ActualWidth * transform.M11);
			var heightPx = Math.Max(1, w.ActualHeight * transform.M22);

			return new System.Drawing.Rectangle((int)origin.X, (int)origin.Y,
				(int)widthPx, (int)heightPx);
		}
	}
}
