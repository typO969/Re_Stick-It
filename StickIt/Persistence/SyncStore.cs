using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace StickIt.Persistence
{
	public static class SyncStore
	{
		private static readonly JsonSerializerOptions Options = new()
		{
			WriteIndented = true
		};

    public static SyncDocument LoadDocument(string path)
		{
			var json = File.ReadAllText(path, Encoding.UTF8);
			var payload = JsonSerializer.Deserialize<SyncPayload>(json, Options);
        var state = StateMigrator.MigrateToCurrent(payload?.State ?? new StickItState());

			return new SyncDocument
			{
				DeviceId = payload?.DeviceId ?? string.Empty,
				ExportedUtc = payload?.ExportedUtc ?? DateTime.MinValue,
				State = state
			};
		}

		public static StickItState Load(string path) => LoadDocument(path).State;

		public static void Save(string path, StickItState state, string deviceId)
		{
			var payload = new SyncPayload
			{
				FormatVersion = 1,
				ExportedUtc = DateTime.UtcNow,
				DeviceId = deviceId,
				State = state
			};

			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(dir))
				Directory.CreateDirectory(dir);

			var json = JsonSerializer.Serialize(payload, Options);
			var tmp = path + ".tmp";

			File.WriteAllText(tmp, json, Encoding.UTF8);

			if (File.Exists(path))
				File.Replace(tmp, path, null);
			else
				File.Move(tmp, path);
		}

		public static DateTime GetNotesModifiedUtc(StickItState state)
		{
			if (state.Notes == null || state.Notes.Count == 0)
				return DateTime.MinValue;

			return state.Notes.Max(n => n.ModifiedUtc);
		}

		private sealed class SyncPayload
		{
			public int FormatVersion { get; set; } = 1;
			public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;
			public string DeviceId { get; set; } = string.Empty;
			public StickItState State { get; set; } = new();
		}
	}

	public sealed class SyncDocument
	{
		public string DeviceId { get; set; } = string.Empty;
		public DateTime ExportedUtc { get; set; }
		public StickItState State { get; set; } = new();
	}
}
