using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32;
using WindowsDebloat.Models;

namespace WindowsDebloat.Helpers;

public static class ServiceHelper
{
	const uint SC_MANAGER_CONNECT = 0x0001;
	const uint SERVICE_CHANGE_CONFIG = 0x0002;
	const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
	const uint SERVICE_DISABLED = 4;
	const int SERVICE_DEMAND_START = 3;

	[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint desiredAccess);

	[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	static extern IntPtr OpenService(IntPtr scmHandle, string serviceName, uint desiredAccess);

	[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	static extern bool ChangeServiceConfig(
		IntPtr serviceHandle, uint serviceType, uint startType, uint errorControl,
		string? binaryPathName, string? loadOrderGroup, IntPtr tagId,
		string? dependencies, string? serviceStartName, string? password, string? displayName);

	[DllImport("advapi32.dll", SetLastError = true)]
	static extern bool CloseServiceHandle(IntPtr handle);

	public static bool Disable(string serviceName, Action<string> log, HistoryRecorder? recorder = null)
	{
		if (!ServiceExists(serviceName))
		{
			log($"    service {serviceName}: not present");
			return false;
		}

		recorder?.Add(CaptureEntry(serviceName));

		StopIfRunning(serviceName);
		ChangeStartType(serviceName, SERVICE_DISABLED);
		log($"    service {serviceName}: stopped and disabled");
		return true;
	}

	public static void Restore(HistoryEntry entry, Action<string> log)
	{
		var serviceName = entry.ServiceName!;

		if (!ServiceExists(serviceName))
		{
			log($"    service {serviceName}: not present, skipping");
			return;
		}

		ChangeStartType(serviceName, (uint)entry.ServicePreviousStartType);
		log($"    service {serviceName}: startup type restored");

		if (entry.ServiceWasRunning)
			StartIfStopped(serviceName, log);
	}

	static void StartIfStopped(string serviceName, Action<string> log)
	{
		try
		{
			using var controller = new ServiceController(serviceName);
			if (controller.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
				return;

			controller.Start();
			controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
			log($"    service {serviceName}: restarted");
		}
		catch (Exception ex)
		{
			log($"    service {serviceName}: could not restart - {ex.Message}");
		}
	}

	static HistoryEntry CaptureEntry(string serviceName)
	{
		return new HistoryEntry
		{
			Type = HistoryEntryType.ServiceStartType,
			Description = $"service {serviceName}",
			ServiceName = serviceName,
			ServicePreviousStartType = GetStartType(serviceName) ?? SERVICE_DEMAND_START,
			ServiceWasRunning = IsRunning(serviceName)
		};
	}

	static int? GetStartType(string serviceName)
	{
		using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}");
		return key?.GetValue("Start") as int?;
	}

	static bool IsRunning(string serviceName)
	{
		using var controller = new ServiceController(serviceName);
		return controller.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending;
	}

	static bool ServiceExists(string serviceName)
	{
		return ServiceController.GetServices().Any(s => string.Equals(s.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
	}

	static void StopIfRunning(string serviceName)
	{
		using var controller = new ServiceController(serviceName);

		if (controller.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
			return;

		try
		{
			controller.Stop();
			controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
		}
		catch (Exception)
		{
			// service may refuse to stop (e.g. dependents); disabling the startup type still prevents it starting on boot.
		}
	}

	static void ChangeStartType(string serviceName, uint startType)
	{
		var scmHandle = OpenSCManager(null, null, SC_MANAGER_CONNECT);
		if (scmHandle == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastWin32Error());

		try
		{
			var serviceHandle = OpenService(scmHandle, serviceName, SERVICE_CHANGE_CONFIG);
			if (serviceHandle == IntPtr.Zero)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			try
			{
				var ok = ChangeServiceConfig(
					serviceHandle, SERVICE_NO_CHANGE, startType, SERVICE_NO_CHANGE,
					null, null, IntPtr.Zero, null, null, null, null);

				if (!ok)
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			finally
			{
				CloseServiceHandle(serviceHandle);
			}
		}
		finally
		{
			CloseServiceHandle(scmHandle);
		}
	}
}
