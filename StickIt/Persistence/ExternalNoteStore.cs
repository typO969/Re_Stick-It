using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace StickIt.Persistence
{
	public static class ExternalNoteStore
	{
		private static readonly JsonSerializerOptions Options = new()
		{
			WriteIndented = true
		};

		public static void Save(string path, NotePersist note)
		{
			var payload = new ExternalNotePayload
			{
				FormatVersion = 1,
				ExportedUtc = DateTime.UtcNow,
				Note = note
			};

			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(dir))
				Directory.CreateDirectory(dir);

			var json = JsonSerializer.Serialize(payload, Options);
			File.WriteAllText(path, json, Encoding.UTF8);
		}

		public static NotePersist Load(string path)
		{
			var json = File.ReadAllText(path, Encoding.UTF8);
			var payload = JsonSerializer.Deserialize<ExternalNotePayload>(json, Options);
			var note = payload?.Note ?? new NotePersist();

			var migrated = StateMigrator.MigrateToCurrent(new StickItState
			{
				Notes = new System.Collections.Generic.List<NotePersist> { note },
				Preferences = new AppPreferences()
			});

			return migrated.Notes.Count > 0 ? migrated.Notes[0] : new NotePersist();
		}

		private sealed class ExternalNotePayload
		{
			public int FormatVersion { get; set; } = 1;
			public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;
			public NotePersist Note { get; set; } = new();
		}
	}
}
