//GEMINI IN

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using StickIt.Services;

namespace StickIt.Sticky.Services
{
    public static class StickyHitTestService
    {
        /// <summary>
        /// Retrieves a Z-ordered list of valid application windows physically located underneath the note's center.
        /// </summary>
        
        public static List<StickyTargetInfo> GetValidTargetsUnderNote(System.Windows.Point noteCenter, IntPtr excludeHwnd)
        {
            var considerList = new List<StickyTargetInfo>();

            try
            {
                // 1) BYPASS OLD TARGETING: Use the robust enumeration service (already filters invisible/tool windows)
                var allWindows = WindowEnumerationService.GetTopLevelWindows();

                foreach (var win in allWindows)
                {
                    // 2) CANDIDATE FILTERING: Exclude the note itself
                    if (win.Hwnd == excludeHwnd) 
                        continue;

                    // 3) BOUNDARY STRIPPING: Exclude minimized (iconic) windows
                    if (IsIconic(win.Hwnd)) 
                        continue;

                    // 3) BOUNDARY STRIPPING: Ensure valid dimensions and spatial intersection
                    if (WindowRectService.TryGetWindowRect(win.Hwnd, out var rect))
                    {
                        if (rect.Width <= 0 || rect.Height <= 0) 
                            continue;

                        // Check if the note's center point is physically inside this window's bounding box
                        if (noteCenter.X >= rect.X && noteCenter.X <= rect.X + rect.Width &&
                            noteCenter.Y >= rect.Y && noteCenter.Y <= rect.Y + rect.Height)
                        {
                            considerList.Add(win);
                        }
                    }
                }
            }
            catch
            {
                // 6) ERROR ISOLATION: Fail silently without crashing the UI thread
            }

            // 4) Z-ORDER TRACKING: EnumWindows inherently returns top-to-bottom Z-order. 
            // The first item in this list is the primary target; the rest are fallbacks.
            return considerList;
        }

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
    }
}

//GEMINI OUT