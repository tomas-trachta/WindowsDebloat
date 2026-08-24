using WindowsDebloat.Helpers;

namespace WindowsDebloat.Models;

public sealed class ActionContext
{
	public required Action<string> Log { get; init; }
	public required bool IsWin11 { get; init; }
	public HistoryRecorder? Recorder { get; init; }
}
