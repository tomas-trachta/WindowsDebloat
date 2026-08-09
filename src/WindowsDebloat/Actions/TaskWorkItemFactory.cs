using WindowsDebloat.Helpers;
using WindowsDebloat.Models;

namespace WindowsDebloat.Actions;

public static class TaskWorkItemFactory
{
	public static TaskWorkItem ForRestorePoint()
	{
		return new TaskWorkItem { Name = RestorePointAction.TaskName, Run = RestorePointAction.Run };
	}

	public static TaskWorkItem ForCatalogItem(CatalogItem item)
	{
		var title = item.Title.Replace("&&", "&");

		if (item.IsApp)
			return new TaskWorkItem { Name = "Remove app: " + title, Run = ctx => RemovePackages(item.Packages!, ctx) };

		return new TaskWorkItem { Name = title, Run = TweakActions.ById[item.Id] };
	}

	static async Task RemovePackages(IReadOnlyList<string> packages, ActionContext ctx)
	{
		foreach (var package in packages)
			await AppxHelper.RemoveByNamePattern(package, ctx.Log);
	}
}
