using System;
using System.IO;
using System.Text;
using System.Text.Json;

using StickIt.Persistence;

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

			if (!File.Exists(path))
			{
				var fresh = StateMigrator.MigrateToCurrent(new StickItState());
				Save(fresh);

				_lastSavedSnapshot = Snapshot(fresh);

				return fresh;
			}


			try
			{
				var json = File.ReadAllText(path, Encoding.UTF8);

				var loaded = JsonSerializer.Deserialize<StickItState>(json, Options) ?? new StickItState();
				var migrated = StateMigrator.MigrateToCurrent(loaded);
				SaveIfChanged(loaded, migrated);

				// Initialize snapshot so first autosave doesn’t re-write
				_lastSavedSnapshot = Snapshot(migrated);

				return migrated;

			}
			catch
			{
				// If file is corrupted, don't crash the app.
				var fresh = StateMigrator.MigrateToCurrent(new StickItState());
				Save(fresh);

				_lastSavedSnapshot = Snapshot(fresh);

				return fresh;
			}

		}


		private static bool SaveIfChanged(StickItState before, StickItState after)
		{
			// Serialize both with same options to compare stable JSON output.
			var a = JsonSerializer.Serialize(before, Options);
			var b = JsonSerializer.Serialize(after, Options);

			if (a == b) return false;

			Save(after);
			return true;
		}


		public static void Save(StickItState state)
		{
			var snapshot = Snapshot(state);

			// Skip write if nothing changed
			if (_lastSavedSnapshot == snapshot)
				return;

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


		private static string Snapshot(StickItState state)
		{
			// Stable JSON snapshot for comparison (same options every time)
			return JsonSerializer.Serialize(state, Options);
		}


	}

}