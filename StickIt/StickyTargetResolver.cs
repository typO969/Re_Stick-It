using System;
using System.Linq;

using StickIt.Persistence;

namespace StickIt.Sticky
{
	public static class StickyTargetResolver
	{
		public static StickyTargetInfo? TryResolve(StickyTargetPersist p)
		{
			if (p == null) return null;

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

			return score;
		}
	}
}
