using Windows.ApplicationModel;
using Windows.Management.Deployment;
using WindowsDebloat.Models;

namespace WindowsDebloat.Helpers;

public static class AppxHelper
{
	public static async Task RemoveByNamePattern(string namePattern, Action<string> log, HistoryRecorder? recorder = null)
	{
		var packageManager = new PackageManager();
		var found = false;

		found |= await RemoveInstalled(packageManager, namePattern, log, recorder);
		found |= RemoveProvisioned(packageManager, namePattern, log, recorder);

		if (!found)
			log($"    not installed: {namePattern}");
	}

	static async Task<bool> RemoveInstalled(PackageManager packageManager, string namePattern, Action<string> log, HistoryRecorder? recorder)
	{
		var found = false;

		var matches = packageManager.FindPackages()
			.Where(p => WildcardMatcher.IsMatch(p.Id.Name, namePattern))
			.ToList();

		foreach (var package in matches)
		{
			found = true;
			try
			{
				var result = await packageManager.RemovePackageAsync(package.Id.FullName, RemovalOptions.RemoveForAllUsers);
				if (result.ExtendedErrorCode is null)
				{
					log($"    removed: {package.Id.Name}");
					recorder?.Add(AppxRemovedEntry(package.Id.Name));
				}
				else
				{
					log($"    could not remove {package.Id.Name}: {result.ErrorText}");
				}
			}
			catch (Exception ex)
			{
				log($"    could not remove {package.Id.Name}: {ex.Message}");
			}
		}

		return found;
	}

	static bool RemoveProvisioned(PackageManager packageManager, string namePattern, Action<string> log, HistoryRecorder? recorder)
	{
		var found = false;

		IEnumerable<Package> provisioned;
		try
		{
			provisioned = packageManager.FindProvisionedPackages();
		}
		catch (Exception)
		{
			return false;
		}

		var matches = provisioned.Where(p => WildcardMatcher.IsMatch(p.Id.Name, namePattern)).ToList();

		foreach (var package in matches)
		{
			found = true;
			try
			{
				packageManager.DeprovisionPackageForAllUsersAsync(package.Id.FamilyName).AsTask().GetAwaiter().GetResult();
				log($"    deprovisioned: {package.Id.Name}");
				recorder?.Add(AppxRemovedEntry(package.Id.Name));
			}
			catch (Exception)
			{
				// best-effort: package may already be gone from the provisioning list.
			}
		}

		return found;
	}

	static HistoryEntry AppxRemovedEntry(string packageName)
	{
		return new HistoryEntry
		{
			Type = HistoryEntryType.AppxRemoved,
			Description = $"app {packageName}",
			AppxPackageName = packageName
		};
	}
}
