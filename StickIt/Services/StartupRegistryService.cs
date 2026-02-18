using System;
using Microsoft.Win32;

namespace StickIt.Services
{
	public static class StartupRegistryService
	{
		private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
		private const string AppName = "StickIt";

		public static void SetRunOnStartup(bool enabled, string executablePath)
		{
			using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true) ??
				Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

			if (key == null)
				return;

			if (enabled)
				key.SetValue(AppName, '"' + executablePath + '"');
			else
				key.DeleteValue(AppName, false);
		}
	}
}
