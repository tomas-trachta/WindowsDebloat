using WindowsDebloat.Helpers;
using WindowsDebloat.Models;

namespace WindowsDebloat.Actions;

public static class RestorePointAction
{
	public const string TaskName = "Create System Restore point";

	public static Task Run(ActionContext ctx)
	{
		SystemRestoreHelper.CreateRestorePoint("Windows Debloat Toolkit", ctx.Log);
		return Task.CompletedTask;
	}
}
