using System.Diagnostics;

namespace WindowsDebloat.Helpers;

public static class ScheduledTaskHelper
{
	public static void Disable(string folderPath, string taskName, Action<string> log)
	{
		var fullPath = folderPath.TrimEnd('\\') + "\\" + taskName;

		var startInfo = new ProcessStartInfo("schtasks.exe")
		{
			ArgumentList = { "/Change", "/TN", fullPath, "/Disable" },
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = Process.Start(startInfo)!;
		process.WaitForExit();

		if (process.ExitCode == 0)
			log($"    task {fullPath}: disabled");
		else
			log($"    task {fullPath}: not found / already disabled");
	}
}
