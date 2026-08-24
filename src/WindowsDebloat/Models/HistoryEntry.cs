using Microsoft.Win32;

namespace WindowsDebloat.Models;

public enum HistoryEntryType
{
	RegistryValue,
	ServiceStartType,
	ScheduledTask,
	AppxRemoved
}

public sealed class HistoryEntry
{
	public required HistoryEntryType Type { get; init; }
	public required string Description { get; init; }

	public string? RegistryPath { get; init; }
	public string? RegistryName { get; init; }
	public bool RegistryValueExisted { get; init; }
	public string? RegistryPreviousValue { get; init; }
	public RegistryValueKind? RegistryPreviousKind { get; init; }

	public string? ServiceName { get; init; }
	public int ServicePreviousStartType { get; init; }
	public bool ServiceWasRunning { get; init; }

	public string? TaskFolderPath { get; init; }
	public string? TaskName { get; init; }

	public string? AppxPackageName { get; init; }
}
