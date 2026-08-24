using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WindowsDebloat.Actions;
using WindowsDebloat.Helpers;
using WindowsDebloat.Models;

namespace WindowsDebloat;

public partial class MainWindow : Window
{
	sealed record CatalogEntry(CheckBox CheckBox, CatalogItem Item);

	readonly List<CatalogEntry> _entries = new();
	readonly bool _isWin11;
	readonly string _appDir;

	bool _running;
	string? _logFilePath;

	public MainWindow()
	{
		InitializeComponent();

		_isWin11 = Environment.OSVersion.Version.Build >= 22000;
		_appDir = AppDomain.CurrentDomain.BaseDirectory;

		OsText.Text = $"Windows {(_isWin11 ? "11" : "10")} (build {Environment.OSVersion.Version.Build})  |  administrator";

		BuildTaskPanel();
		UpdateStatus();
	}

	void BuildTaskPanel()
	{
		AddSectionHeader("Preinstalled apps", "Each can be reinstalled from the Microsoft Store later.");
		foreach (var item in WindowsDebloat.Catalog.AppCatalog.Items)
			AddItemCheckbox(item);

		AddSectionHeader("System tweaks (recommended)", "Telemetry, ads and background weight. Safe for normal use.");
		foreach (var item in WindowsDebloat.Catalog.TweakCatalog.Items.Where(i => i.Group == CatalogGroup.Tweak))
			AddItemCheckbox(item);

		AddSectionHeader("Advanced", "These change system behavior - read the descriptions before enabling.");
		foreach (var item in WindowsDebloat.Catalog.TweakCatalog.Items.Where(i => i.Group == CatalogGroup.Advanced))
			AddItemCheckbox(item);
	}

	void AddSectionHeader(string text, string sub)
	{
		TaskPanel.Children.Add(new TextBlock
		{
			Text = text,
			FontSize = 15,
			FontWeight = FontWeights.SemiBold,
			Margin = new Thickness(0, 16, 0, 0)
		});

		TaskPanel.Children.Add(new TextBlock
		{
			Text = sub,
			FontSize = 12,
			Foreground = Brush("#FF9BA3AF"),
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0, 1, 0, 2)
		});
	}

	void AddItemCheckbox(CatalogItem item)
	{
		var checkBox = new CheckBox
		{
			IsChecked = item.Default,
			Margin = new Thickness(0, 9, 0, 0)
		};

		var panel = new StackPanel();
		panel.Children.Add(new TextBlock
		{
			Text = item.Title.Replace("&&", "&"),
			FontWeight = FontWeights.SemiBold,
			TextWrapping = TextWrapping.Wrap
		});
		panel.Children.Add(new TextBlock
		{
			Text = item.Desc,
			TextWrapping = TextWrapping.Wrap,
			FontSize = 12,
			Foreground = Brush("#FF9BA3AF"),
			Margin = new Thickness(0, 1, 0, 0)
		});
		checkBox.Content = panel;

		checkBox.Checked += (_, _) => UpdateStatus();
		checkBox.Unchecked += (_, _) => UpdateStatus();

		TaskPanel.Children.Add(checkBox);
		_entries.Add(new CatalogEntry(checkBox, item));
	}

	static SolidColorBrush Brush(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

	void UpdateStatus()
	{
		if (_running)
			return;

		var selected = _entries.Count(e => e.CheckBox.IsChecked == true);
		StatusText.Text = $"{selected} of {_entries.Count} tasks selected.";
	}

	void BtnRecommended_Click(object sender, RoutedEventArgs e)
	{
		foreach (var entry in _entries)
			entry.CheckBox.IsChecked = entry.Item.Default;
	}

	void BtnAll_Click(object sender, RoutedEventArgs e)
	{
		foreach (var entry in _entries)
			entry.CheckBox.IsChecked = true;
	}

	void BtnNone_Click(object sender, RoutedEventArgs e)
	{
		foreach (var entry in _entries)
			entry.CheckBox.IsChecked = false;
	}

	async void BtnRun_Click(object sender, RoutedEventArgs e)
	{
		if (_running)
			return;

		var selected = _entries.Where(en => en.CheckBox.IsChecked == true).ToList();
		if (selected.Count == 0)
		{
			MessageBox.Show(this, "Select at least one task first.", "Nothing selected", MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}

		var advanced = selected.Where(en => en.Item.Group == CatalogGroup.Advanced).ToList();
		if (advanced.Count > 0)
		{
			var names = string.Join("\n", advanced.Select(en => "  - " + en.Item.Title.Replace("&&", "&")));
			var result = MessageBox.Show(this,
				$"These advanced tweaks change system behavior:\n\n{names}\n\nContinue?",
				"Confirm advanced tweaks", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result != MessageBoxResult.Yes)
				return;
		}

		var workItems = new List<TaskWorkItem>();
		if (ChkRestorePoint.IsChecked == true)
			workItems.Add(TaskWorkItemFactory.ForRestorePoint());

		workItems.AddRange(selected.Select(en => TaskWorkItemFactory.ForCatalogItem(en.Item)));

		await RunTasksAsync(workItems, ChkRestartExplorer.IsChecked == true);
	}

	async Task RunTasksAsync(List<TaskWorkItem> workItems, bool restartExplorer)
	{
		_running = true;
		SetUiEnabled(false);
		LogBox.Clear();

		_logFilePath = Path.Combine(_appDir, $"debloat-gui-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
		AppendLog($"Log file: {_logFilePath}");

		Progress.Maximum = Math.Max(1, workItems.Count);
		Progress.Value = 0;

		var recorder = new HistoryRecorder();
		var ctx = new ActionContext { Log = AppendLog, IsWin11 = _isWin11, Recorder = recorder };

		var completed = 0;
		foreach (var task in workItems)
		{
			StatusText.Text = $"Working...  {completed}/{workItems.Count}   {task.Name}";
			AppendLog("");
			AppendLog($"=== {task.Name} ===");

			try
			{
				await Task.Run(() => task.Run(ctx));
			}
			catch (Exception ex)
			{
				AppendLog($"    ERROR: {ex.Message}");
			}

			completed++;
			Progress.Value = completed;
		}

		if (restartExplorer)
		{
			AppendLog("");
			AppendLog("Restarting Explorer to apply taskbar/Start changes...");
			KillExplorer();
		}

		SaveSnapshot(recorder);

		AppendLog("");
		AppendLog("All selected tasks finished. A reboot is recommended.");

		Progress.Value = Progress.Maximum;
		StatusText.Text = "Finished - a reboot is recommended.";
		_running = false;
		SetUiEnabled(true);
		BtnRun.Content = "Apply again";
	}

	void SaveSnapshot(HistoryRecorder recorder)
	{
		var snapshot = new Snapshot { CreatedAt = DateTime.Now, Entries = recorder.Entries.ToList() };
		var path = SnapshotStore.Save(_appDir, snapshot);

		if (path is not null)
			AppendLog($"Snapshot saved: {path}  (use Revert... to undo these changes)");
	}

	void BtnRevert_Click(object sender, RoutedEventArgs e)
	{
		if (_running)
			return;

		new RevertWindow(_appDir) { Owner = this }.ShowDialog();
	}

	static void KillExplorer()
	{
		foreach (var process in System.Diagnostics.Process.GetProcessesByName("explorer"))
		{
			try { process.Kill(); } catch (Exception) { }
		}
	}

	void AppendLog(string message)
	{
		var line = $"[{DateTime.Now:HH:mm:ss}] {message}";

		Dispatcher.Invoke(() =>
		{
			LogBox.AppendText(line + "\r\n");
			LogBox.ScrollToEnd();
		});

		if (_logFilePath is not null)
		{
			try { File.AppendAllText(_logFilePath, line + "\r\n"); }
			catch (Exception) { }
		}
	}

	void SetUiEnabled(bool enabled)
	{
		BtnRun.IsEnabled = enabled;
		BtnRevert.IsEnabled = enabled;
		BtnAll.IsEnabled = enabled;
		BtnNone.IsEnabled = enabled;
		BtnRecommended.IsEnabled = enabled;
		TaskPanel.IsEnabled = enabled;
		ChkRestorePoint.IsEnabled = enabled;
		ChkRestartExplorer.IsEnabled = enabled;
	}

	void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		if (!_running)
			return;

		var result = MessageBox.Show("Tasks are still running. Close anyway?", "Still working", MessageBoxButton.YesNo, MessageBoxImage.Warning);
		if (result != MessageBoxResult.Yes)
			e.Cancel = true;
	}
}
