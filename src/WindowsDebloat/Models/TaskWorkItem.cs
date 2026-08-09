namespace WindowsDebloat.Models;

public sealed class TaskWorkItem
{
	public required string Name { get; init; }
	public required Func<ActionContext, Task> Run { get; init; }
}
