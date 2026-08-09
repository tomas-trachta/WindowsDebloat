using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace WindowsDebloat.Helpers;

public static class ServiceHelper
{
	const uint SC_MANAGER_CONNECT = 0x0001;
	const uint SERVICE_CHANGE_CONFIG = 0x0002;
	const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
	const uint SERVICE_DISABLED = 4;

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

	public static bool Disable(string serviceName, Action<string> log)
	{
		if (!ServiceExists(serviceName))
		{
			log($"    service {serviceName}: not present");
			return false;
		}

		StopIfRunning(serviceName);
		SetStartupDisabled(serviceName);
		log($"    service {serviceName}: stopped and disabled");
		return true;
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

	static void SetStartupDisabled(string serviceName)
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
					serviceHandle, SERVICE_NO_CHANGE, SERVICE_DISABLED, SERVICE_NO_CHANGE,
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
