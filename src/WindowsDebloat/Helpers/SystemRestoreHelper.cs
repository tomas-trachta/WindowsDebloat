using System.Management;

namespace WindowsDebloat.Helpers;

public static class SystemRestoreHelper
{
	const uint MODIFY_SETTINGS = 12;
	const uint BEGIN_SYSTEM_CHANGE = 100;

	public static void CreateRestorePoint(string description, Action<string> log)
	{
		try
		{
			EnableOnSystemDrive();
			InvokeCreateRestorePoint(description);
			log("    restore point created.");
		}
		catch (Exception ex)
		{
			log($"    WARNING: restore point failed: {ex.Message}");
			log("    (Windows allows only one per 24h - a recent existing point also triggers this.)");
		}
	}

	static void EnableOnSystemDrive()
	{
		using var systemRestoreClass = new ManagementClass("root\\default", "SystemRestore", null);
		using var parameters = systemRestoreClass.GetMethodParameters("Enable");
		parameters["Drive"] = Environment.GetEnvironmentVariable("SystemDrive") + "\\";
		systemRestoreClass.InvokeMethod("Enable", parameters, null);
	}

	static void InvokeCreateRestorePoint(string description)
	{
		using var systemRestoreClass = new ManagementClass("root\\default", "SystemRestore", null);
		using var parameters = systemRestoreClass.GetMethodParameters("CreateRestorePoint");
		parameters["Description"] = description;
		parameters["RestorePointType"] = MODIFY_SETTINGS;
		parameters["EventType"] = BEGIN_SYSTEM_CHANGE;

		using var result = systemRestoreClass.InvokeMethod("CreateRestorePoint", parameters, null);
		var returnValue = Convert.ToUInt32(result?["ReturnValue"] ?? 0u);
		if (returnValue != 0)
			throw new InvalidOperationException($"CreateRestorePoint returned error code {returnValue}");
	}
}
