namespace WindowsDebloat.Models;

public sealed class Snapshot
{
	public required DateTime CreatedAt { get; init; }
	public required List<HistoryEntry> Entries { get; init; }
}
