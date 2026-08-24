using System.IO;
using System.Text.Json;
using WindowsDebloat.Models;

namespace WindowsDebloat.Helpers;

public static class SnapshotStore
{
	static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	public static string SnapshotsDir(string appDir) => Path.Combine(appDir, "snapshots");

	public static string? Save(string appDir, Snapshot snapshot)
	{
		if (snapshot.Entries.Count == 0)
			return null;

		var dir = SnapshotsDir(appDir);
		Directory.CreateDirectory(dir);

		var path = Path.Combine(dir, $"snapshot-{snapshot.CreatedAt:yyyy-MM-dd_HH-mm-ss}.json");
		File.WriteAllText(path, JsonSerializer.Serialize(snapshot, JsonOptions));
		return path;
	}

	public static List<string> ListFiles(string appDir)
	{
		var dir = SnapshotsDir(appDir);
		if (!Directory.Exists(dir))
			return new List<string>();

		return Directory.GetFiles(dir, "snapshot-*.json")
			.OrderByDescending(f => f)
			.ToList();
	}

	public static Snapshot Load(string path)
	{
		var json = File.ReadAllText(path);
		return JsonSerializer.Deserialize<Snapshot>(json)
			?? throw new InvalidOperationException($"Could not read snapshot file: {path}");
	}
}
