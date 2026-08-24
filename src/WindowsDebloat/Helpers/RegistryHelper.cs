using Microsoft.Win32;
using WindowsDebloat.Models;

namespace WindowsDebloat.Helpers;

public static class RegistryHelper
{
	public static void SetValue(string path, string name, object value, RegistryValueKind kind = RegistryValueKind.DWord, HistoryRecorder? recorder = null)
	{
		var (root, subPath) = SplitHive(path);
		using var key = root.CreateSubKey(subPath, writable: true);

		recorder?.Add(CaptureEntry(path, name, key));

		key.SetValue(name, value, kind);
	}

	public static void RemoveValue(string path, string name, HistoryRecorder? recorder = null)
	{
		var (root, subPath) = SplitHive(path);
		using var key = root.OpenSubKey(subPath, writable: true);
		if (key is null)
			return;

		recorder?.Add(CaptureEntry(path, name, key));

		key.DeleteValue(name, throwOnMissingValue: false);
	}

	public static void RestoreValue(HistoryEntry entry, Action<string> log)
	{
		var path = entry.RegistryPath!;
		var name = entry.RegistryName!;

		if (entry.RegistryValueExisted)
		{
			var value = FromStorable(entry.RegistryPreviousValue!, entry.RegistryPreviousKind!.Value);
			SetValue(path, name, value, entry.RegistryPreviousKind!.Value);
			log($"    reg: {path}\\{name} restored to {entry.RegistryPreviousValue}");
		}
		else
		{
			RemoveValue(path, name);
			log($"    reg: {path}\\{name} removed (did not exist before)");
		}
	}

	static HistoryEntry CaptureEntry(string path, string name, RegistryKey key)
	{
		var existingValue = key.GetValue(name);
		var existed = existingValue is not null;
		var kind = existed ? key.GetValueKind(name) : (RegistryValueKind?)null;

		return new HistoryEntry
		{
			Type = HistoryEntryType.RegistryValue,
			Description = $"reg {path}\\{name}",
			RegistryPath = path,
			RegistryName = name,
			RegistryValueExisted = existed,
			RegistryPreviousValue = existed ? ToStorable(existingValue!, kind!.Value) : null,
			RegistryPreviousKind = kind
		};
	}

	static string ToStorable(object value, RegistryValueKind kind) => kind switch
	{
		RegistryValueKind.DWord or RegistryValueKind.QWord => Convert.ToString(value)!,
		RegistryValueKind.MultiString => string.Join("\n", (string[])value),
		RegistryValueKind.Binary => Convert.ToHexString((byte[])value),
		_ => (string)value
	};

	static object FromStorable(string stored, RegistryValueKind kind) => kind switch
	{
		RegistryValueKind.DWord => int.Parse(stored),
		RegistryValueKind.QWord => long.Parse(stored),
		RegistryValueKind.MultiString => stored.Split('\n'),
		RegistryValueKind.Binary => Convert.FromHexString(stored),
		_ => stored
	};

	static (RegistryKey Root, string SubPath) SplitHive(string path)
	{
		var separatorIndex = path.IndexOf('\\');
		var hive = path[..separatorIndex];
		var subPath = path[(separatorIndex + 1)..];

		RegistryKey root = hive switch
		{
			"HKLM" => Registry.LocalMachine,
			"HKCU" => Registry.CurrentUser,
			"HKCR" => Registry.ClassesRoot,
			"HKU" => Registry.Users,
			_ => throw new ArgumentException($"Unsupported registry hive: {hive}")
		};

		return (root, subPath);
	}
}
