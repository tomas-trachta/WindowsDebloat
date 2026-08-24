using System.Diagnostics;
using WindowsDebloat.Models;

namespace WindowsDebloat.Helpers;

public static class ScheduledTaskHelper
{
	public static void Disable(string folderPath, string taskName, Action<string> log, HistoryRecorder? recorder = null)
	{
		var fullPath = folderPath.TrimEnd('\\') + "\\" + taskName;
		var wasEnabled = IsEnabled(fullPath);

		var exitCode = RunSchtasks("/Change", "/TN", fullPath, "/Disable");

		if (exitCode != 0)
		{
			log($"    task {fullPath}: not found / already disabled");
			return;
		}

		log($"    task {fullPath}: disabled");

		if (wasEnabled == true)
			recorder?.Add(new HistoryEntry
			{
				Type = HistoryEntryType.ScheduledTask,
				Description = $"task {fullPath}",
				TaskFolderPath = folderPath,
				TaskName = taskName
			});
	}

	public static void Restore(HistoryEntry entry, Action<string> log)
	{
		var fullPath = entry.TaskFolderPath!.TrimEnd('\\') + "\\" + entry.TaskName;
		var exitCode = RunSchtasks("/Change", "/TN", fullPath, "/Enable");
		log(exitCode == 0 ? $"    task {fullPath}: re-enabled" : $"    task {fullPath}: could not re-enable");
	}

	static bool? IsEnabled(string fullPath)
	{
		var startInfo = new ProcessStartInfo("schtasks.exe")
		{
			ArgumentList = { "/Query", "/TN", fullPath, "/V", "/FO", "LIST" },
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		using var process = Process.Start(startInfo)!;
		var output = process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		if (process.ExitCode != 0)
			return null;

		var line = output.Split('\n').FirstOrDefault(l => l.TrimStart().StartsWith("Scheduled Task State", StringComparison.OrdinalIgnoreCase));
		return line?.Contains("Enabled", StringComparison.OrdinalIgnoreCase);
	}

	static int RunSchtasks(params string[] args)
	{
		var startInfo = new ProcessStartInfo("schtasks.exe")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		foreach (var arg in args)
			startInfo.ArgumentList.Add(arg);

		using var process = Process.Start(startInfo)!;
		process.WaitForExit();
		return process.ExitCode;
	}
}
