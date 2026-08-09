using Microsoft.Win32;

namespace WindowsDebloat.Helpers;

public static class RegistryHelper
{
	public static void SetValue(string path, string name, object value, RegistryValueKind kind = RegistryValueKind.DWord)
	{
		var (root, subPath) = SplitHive(path);
		using var key = root.CreateSubKey(subPath, writable: true);
		key.SetValue(name, value, kind);
	}

	public static void RemoveValue(string path, string name)
	{
		var (root, subPath) = SplitHive(path);
		using var key = root.OpenSubKey(subPath, writable: true);
		key?.DeleteValue(name, throwOnMissingValue: false);
	}

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
