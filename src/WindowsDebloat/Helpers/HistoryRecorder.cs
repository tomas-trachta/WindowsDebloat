using WindowsDebloat.Models;

namespace WindowsDebloat.Helpers;

public sealed class HistoryRecorder
{
	readonly List<HistoryEntry> _entries = new();

	public IReadOnlyList<HistoryEntry> Entries => _entries;

	public void Add(HistoryEntry entry) => _entries.Add(entry);
}
