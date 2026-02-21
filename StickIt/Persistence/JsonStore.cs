using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace StickIt.Persistence
{
	public static class JsonStore
	{
		private static readonly JsonSerializerOptions Options = new()
		{
			WriteIndented = true
		};
		private static string? _lastSavedSnapshot;

		public static string GetStateFilePath()
		{
			var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var dir = Path.Combine(root, "969 Studios", "StickIt");
			Directory.CreateDirectory(dir);
			return Path.Combine(dir, "notes.json");
		}

		public static StickItState LoadOrDefault()
		{
			var path = GetStateFilePath();
			var bakPath = path + ".bak";

			if (!File.Exists(path))
			{
				var fresh = StateMigrator.MigrateToCurrent(new StickItState());
				Save(fresh);
				_lastSavedSnapshot = Snapshot(fresh);
				return fresh;
			}

			try
			{
				var loaded = LoadStateFromPath(path);
				var migrated = StateMigrator.MigrateToCurrent(loaded);
				SaveIfChanged(loaded, migrated);
				_lastSavedSnapshot = Snapshot(migrated);
				return migrated;
			}
			catch
			{
				if (File.Exists(bakPath))
				{
					try
					{
						var backupLoaded = LoadStateFromPath(bakPath);
						var backupMigrated = StateMigrator.MigrateToCurrent(backupLoaded);
						Save(backupMigrated);
						_lastSavedSnapshot = Snapshot(backupMigrated);
						return backupMigrated;
					}
					catch
					{
						// Fall through to fresh state.
					}
				}

				var fresh = StateMigrator.MigrateToCurrent(new StickItState());
				Save(fresh);
				_lastSavedSnapshot = Snapshot(fresh);
				return fresh;
			}
		}

		private static bool SaveIfChanged(StickItState before, StickItState after)
		{
			var a = JsonSerializer.Serialize(before, Options);
			var b = JsonSerializer.Serialize(after, Options);
			if (a == b) return false;
			Save(after);
			return true;
		}

		public static void Save(StickItState state)
		{
			var snapshot = Snapshot(state);
			if (_lastSavedSnapshot == snapshot)
				return;

			try
			{
				var path = GetStateFilePath();
				var tmp = path + ".tmp";
				var bak = path + ".bak";

				File.WriteAllText(tmp, snapshot, Encoding.UTF8);

				if (File.Exists(path))
				{
					try
					{
						File.Copy(path, bak, overwrite: true);
					}
					catch
					{
						// Backup failure should not block save
					}

					File.Replace(tmp, path, null);
				}
				else
				{
					File.Move(tmp, path);
				}

				_lastSavedSnapshot = snapshot;
			}
			catch
			{
				// Best-effort persistence.
			}
		}

		private static string Snapshot(StickItState state)
		{
			return JsonSerializer.Serialize(state, Options);
		}

		private static StickItState LoadStateFromPath(string path)
		{
			var json = File.ReadAllText(path, Encoding.UTF8);
			return JsonSerializer.Deserialize<StickItState>(json, Options) ?? new StickItState();
		}
	}
}
