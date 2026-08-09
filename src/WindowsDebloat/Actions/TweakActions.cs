using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using WindowsDebloat.Helpers;
using WindowsDebloat.Models;

namespace WindowsDebloat.Actions;

public static class TweakActions
{
	public static readonly IReadOnlyDictionary<string, Func<ActionContext, Task>> ById = Build();

	static IReadOnlyDictionary<string, Func<ActionContext, Task>> Build()
	{
		return new Dictionary<string, Func<ActionContext, Task>>
		{
			["telemetry"] = Telemetry,
			["bingsearch"] = BingSearch,
			["ads"] = Ads,
			["widgets"] = Widgets,
			["copilot"] = Copilot,
			["gamedvr"] = GameDvr,
			["dop2p"] = DeliveryOptimizationP2P,
			["edge"] = EdgeBackground,
			["activity"] = ActivityHistory,
			["sysmain"] = SysMain,
			["onedrive"] = OneDrive,
			["xboxsvc"] = XboxServices,
			["indexing"] = SearchIndexing,
			["bgapps"] = BackgroundApps,
			["visualfx"] = VisualEffects,
			["hibernate"] = Hibernation,
		};
	}

	static Task Telemetry(ActionContext ctx)
	{
		ServiceHelper.Disable("DiagTrack", ctx.Log);
		ServiceHelper.Disable("dmwappushservice", ctx.Log);
		RegistryHelper.SetValue(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
		LogRegistryWrite(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);

		ScheduledTaskHelper.Disable(@"\Microsoft\Windows\Application Experience\", "Microsoft Compatibility Appraiser", ctx.Log);
		ScheduledTaskHelper.Disable(@"\Microsoft\Windows\Application Experience\", "ProgramDataUpdater", ctx.Log);
		ScheduledTaskHelper.Disable(@"\Microsoft\Windows\Autochk\", "Proxy", ctx.Log);
		ScheduledTaskHelper.Disable(@"\Microsoft\Windows\Customer Experience Improvement Program\", "Consolidator", ctx.Log);
		ScheduledTaskHelper.Disable(@"\Microsoft\Windows\Customer Experience Improvement Program\", "UsbCeip", ctx.Log);
		ScheduledTaskHelper.Disable(@"\Microsoft\Windows\Customer Experience Improvement Program\", "KernelCeipTask", ctx.Log);
		ScheduledTaskHelper.Disable(@"\Microsoft\Windows\DiskDiagnostic\", "Microsoft-Windows-DiskDiagnosticDataCollector", ctx.Log);

		return Task.CompletedTask;
	}

	static Task BingSearch(ActionContext ctx)
	{
		SetReg(ctx, @"HKCU\SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);

		if (!ctx.IsWin11)
		{
			SetReg(ctx, @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
			SetReg(ctx, @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0);
			SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
		}

		return Task.CompletedTask;
	}

	static Task Ads(ActionContext ctx)
	{
		const string cdm = @"HKCU\Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
		var values = new Dictionary<string, int>
		{
			["ContentDeliveryAllowed"] = 0,
			["OemPreInstalledAppsEnabled"] = 0,
			["PreInstalledAppsEnabled"] = 0,
			["SilentInstalledAppsEnabled"] = 0,
			["SoftLandingEnabled"] = 0,
			["SystemPaneSuggestionsEnabled"] = 0,
			["RotatingLockScreenOverlayEnabled"] = 0,
			["SubscribedContent-310093Enabled"] = 0,
			["SubscribedContent-338387Enabled"] = 0,
			["SubscribedContent-338388Enabled"] = 0,
			["SubscribedContent-338389Enabled"] = 0,
			["SubscribedContent-353698Enabled"] = 0,
		};
		foreach (var (name, value) in values)
			SetReg(ctx, cdm, name, value);

		SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0);
		SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSyncProviderNotifications", 0);
		SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement", "ScoobeSystemSettingEnabled", 0);

		if (ctx.IsWin11)
			SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_IrisRecommendations", 0);

		return Task.CompletedTask;
	}

	static async Task Widgets(ActionContext ctx)
	{
		if (ctx.IsWin11)
		{
			SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
			SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0);
			SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarMn", 0);
			await AppxHelper.RemoveByNamePattern("MicrosoftWindows.Client.WebExperience", ctx.Log);
		}
		else
		{
			SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0);
			SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Feeds", "ShellFeedsTaskbarViewMode", 2);
		}
	}

	static async Task Copilot(ActionContext ctx)
	{
		SetReg(ctx, @"HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
		SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
		SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", 0);
		await AppxHelper.RemoveByNamePattern("Microsoft.Copilot", ctx.Log);
		SetReg(ctx, @"HKCU\Software\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1);
		SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsAI", "DisableAIDataAnalysis", 1);
	}

	static Task GameDvr(ActionContext ctx)
	{
		SetReg(ctx, @"HKCU\System\GameConfigStore", "GameDVR_Enabled", 0);
		SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
		return Task.CompletedTask;
	}

	static Task DeliveryOptimizationP2P(ActionContext ctx)
	{
		SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 0);
		return Task.CompletedTask;
	}

	static Task EdgeBackground(ActionContext ctx)
	{
		SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", 0);
		SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Edge", "BackgroundModeEnabled", 0);
		SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Edge", "HubsSidebarEnabled", 0);
		return Task.CompletedTask;
	}

	static Task ActivityHistory(ActionContext ctx)
	{
		SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0);
		SetReg(ctx, @"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0);
		return Task.CompletedTask;
	}

	static Task SysMain(ActionContext ctx)
	{
		ServiceHelper.Disable("SysMain", ctx.Log);
		return Task.CompletedTask;
	}

	static Task OneDrive(ActionContext ctx)
	{
		KillProcess("OneDrive");

		var setup = new[]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "OneDriveSetup.exe"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "OneDriveSetup.exe"),
		}.FirstOrDefault(File.Exists);

		if (setup is not null)
		{
			ctx.Log($"    running: {setup} /uninstall");
			using var process = Process.Start(new ProcessStartInfo(setup, "/uninstall") { UseShellExecute = true });
			process?.WaitForExit();
		}
		else if (TryFindOnPath("winget.exe") is not null)
		{
			ctx.Log("    OneDriveSetup.exe not found - trying winget...");
			RunProcess("winget", "uninstall --id Microsoft.OneDrive --silent");
		}
		else
		{
			ctx.Log("    OneDrive uninstaller not found.");
		}

		RegistryHelper.RemoveValue(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Run", "OneDrive");
		ctx.Log($"    note: local files in {Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\OneDrive are NOT deleted.");
		return Task.CompletedTask;
	}

	static Task XboxServices(ActionContext ctx)
	{
		foreach (var service in new[] { "XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc" })
			ServiceHelper.Disable(service, ctx.Log);

		return Task.CompletedTask;
	}

	static Task SearchIndexing(ActionContext ctx)
	{
		ServiceHelper.Disable("WSearch", ctx.Log);
		return Task.CompletedTask;
	}

	static Task BackgroundApps(ActionContext ctx)
	{
		if (ctx.IsWin11)
		{
			ctx.Log("    Windows 11 manages this per-app in Settings - nothing to do.");
		}
		else
		{
			SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1);
		}

		return Task.CompletedTask;
	}

	static Task VisualEffects(ActionContext ctx)
	{
		SetReg(ctx, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2);
		SetReg(ctx, @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0);
		RegistryHelper.SetValue(@"HKCU\Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String);
		ctx.Log(@"    reg: HKCU\Control Panel\Desktop\MenuShowDelay = 0");
		return Task.CompletedTask;
	}

	static Task Hibernation(ActionContext ctx)
	{
		RunProcess("powercfg.exe", "/hibernate off");
		ctx.Log("    hibernation off - hiberfil.sys removed, Fast Startup disabled.");
		return Task.CompletedTask;
	}

	static void SetReg(ActionContext ctx, string path, string name, int value)
	{
		RegistryHelper.SetValue(path, name, value);
		LogRegistryWrite(ctx, path, name, value);
	}

	static void LogRegistryWrite(ActionContext ctx, string path, string name, object value)
	{
		ctx.Log($"    reg: {path}\\{name} = {value}");
	}

	static void KillProcess(string name)
	{
		foreach (var process in Process.GetProcessesByName(name))
		{
			try { process.Kill(); } catch (Exception) { }
		}
	}

	static void RunProcess(string fileName, string arguments)
	{
		using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		});
		process?.WaitForExit();
	}

	static string? TryFindOnPath(string fileName)
	{
		var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? "";
		return pathVariable.Split(Path.PathSeparator)
			.Select(dir => Path.Combine(dir, fileName))
			.FirstOrDefault(File.Exists);
	}
}
