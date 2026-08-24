using WindowsDebloat.Models;

namespace WindowsDebloat.Helpers;

public static class RevertRunner
{
	public static void Apply(Snapshot snapshot, Action<string> log)
	{
		foreach (var entry in Enumerable.Reverse(snapshot.Entries))
			ApplyEntry(entry, log);
	}

	static void ApplyEntry(HistoryEntry entry, Action<string> log)
	{
		try
		{
			switch (entry.Type)
			{
				case HistoryEntryType.RegistryValue:
					RegistryHelper.RestoreValue(entry, log);
					break;

				case HistoryEntryType.ServiceStartType:
					ServiceHelper.Restore(entry, log);
					break;

				case HistoryEntryType.ScheduledTask:
					ScheduledTaskHelper.Restore(entry, log);
					break;

				case HistoryEntryType.AppxRemoved:
					log($"    app {entry.AppxPackageName}: not reinstalled automatically - install it again from the Microsoft Store if needed.");
					break;
			}
		}
		catch (Exception ex)
		{
			log($"    ERROR reverting {entry.Description}: {ex.Message}");
		}
	}
}
