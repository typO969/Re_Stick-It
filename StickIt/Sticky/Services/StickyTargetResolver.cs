using System;
using System.Linq;

using StickIt.Persistence;
using StickIt.Services;

namespace StickIt.Sticky
{
	public static class StickyTargetResolver
	{
		public static StickyTargetInfo? TryResolve(StickyTargetPersist p)
		{
			if (p == null) return null;

			if (IsDesktopClass(p.ClassName))
			{
				if (p.TargetAnchorX.HasValue && p.TargetAnchorY.HasValue)
				{
             return StickIt.Sticky.Services.DesktopTargetService.TryGetDesktopTargetAtPoint(
						(int)Math.Round(p.TargetAnchorX.Value),
						(int)Math.Round(p.TargetAnchorY.Value));
				}

            return StickIt.Sticky.Services.DesktopTargetService.TryGetDesktopTarget();
			}

			var candidates = WindowEnumerationService.GetTopLevelWindows();

			// 1) Hard match: same PID (best if process still running)
			if (p.ProcessId > 0)
			{
				var byPid = candidates.Where(c => c.ProcessId == p.ProcessId).ToList();
				if (byPid.Count == 1) return byPid[0];

				// If multiple windows for same PID, choose best title/class match
				var bestPid = byPid
					.OrderByDescending(c => Score(c, p))
					.FirstOrDefault();

				if (bestPid != null && Score(bestPid, p) >= 2)
					return bestPid;
			}

			// 2) Soft match: process name + title/class hints
			var byProc = candidates
				.Where(c => !string.IsNullOrWhiteSpace(p.ProcessName) &&
								string.Equals(c.ProcessName, p.ProcessName, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(c => Score(c, p))
				.FirstOrDefault();

			if (byProc != null && Score(byProc, p) >= 3)
				return byProc;

			// 3) Last resort: title/class only (risky; require strong score)
			var byHints = candidates
				.OrderByDescending(c => Score(c, p))
				.FirstOrDefault();

			if (byHints != null && Score(byHints, p) >= 4)
				return byHints;

			return null;
		}

		private static bool IsDesktopClass(string? className)
			=> string.Equals(className, "Progman", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "WorkerW", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "#32769", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(className, "Desktop", StringComparison.OrdinalIgnoreCase);

		private static bool CandidateContainsAnchor(StickyTargetInfo c, StickyTargetPersist p)
		{
			if (!p.TargetAnchorX.HasValue || !p.TargetAnchorY.HasValue)
				return true;

			if (c.Hwnd == IntPtr.Zero)
				return false;

       if (!StickIt.Sticky.Services.WindowRectService.TryGetWindowRect(c.Hwnd, out var rect))
				return false;

			var ax = p.TargetAnchorX.Value;
			var ay = p.TargetAnchorY.Value;
			return ax >= rect.X && ax <= rect.X + rect.Width && ay >= rect.Y && ay <= rect.Y + rect.Height;
		}

		private static int Score(StickyTargetInfo c, StickyTargetPersist p)
		{
			int score = 0;

			if (p.ProcessId > 0 && c.ProcessId == p.ProcessId) score += 3;

			if (!string.IsNullOrWhiteSpace(p.ProcessName) &&
				 string.Equals(c.ProcessName, p.ProcessName, StringComparison.OrdinalIgnoreCase))
				score += 2;

			if (!string.IsNullOrWhiteSpace(p.ClassName) &&
				 string.Equals(c.ClassName, p.ClassName, StringComparison.Ordinal))
				score += 2;

			if (!string.IsNullOrWhiteSpace(p.WindowTitle) &&
				 !string.IsNullOrWhiteSpace(c.WindowTitle))
			{
				// Exact title gets more; containment is weaker
				if (string.Equals(c.WindowTitle, p.WindowTitle, StringComparison.Ordinal))
					score += 3;
				else if (c.WindowTitle.IndexOf(p.WindowTitle, StringComparison.OrdinalIgnoreCase) >= 0 ||
							p.WindowTitle.IndexOf(c.WindowTitle, StringComparison.OrdinalIgnoreCase) >= 0)
					score += 1;
			}

			if (CandidateContainsAnchor(c, p))
				score += 4;

			return score;
		}
	}
}
