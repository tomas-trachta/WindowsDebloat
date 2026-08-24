using System.Windows;
using System.Windows.Controls;
using WindowsDebloat.Helpers;
using WindowsDebloat.Models;

namespace WindowsDebloat;

public partial class RevertWindow : Window
{
	sealed record SnapshotEntry(string Path, Snapshot Snapshot)
	{
		public override string ToString() => $"{Snapshot.CreatedAt:yyyy-MM-dd HH:mm:ss}  ({Snapshot.Entries.Count} changes)";
	}

	readonly string _appDir;
	bool _reverting;

	public RevertWindow(string appDir)
	{
		InitializeComponent();
		_appDir = appDir;
		LoadSnapshots();
	}

	void LoadSnapshots()
	{
		var files = SnapshotStore.ListFiles(_appDir);

		foreach (var file in files)
		{
			try { SnapshotList.Items.Add(new SnapshotEntry(file, SnapshotStore.Load(file))); }
			catch (Exception) { /* skip unreadable/corrupt snapshot files */ }
		}

		if (SnapshotList.Items.Count == 0)
			LogBox.Text = "No snapshots found yet - run 'Apply selected' at least once first.";
	}

	void SnapshotList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (SnapshotList.SelectedItem is not SnapshotEntry entry)
		{
			BtnRevert.IsEnabled = false;
			return;
		}

		BtnRevert.IsEnabled = true;
		LogBox.Text = string.Join("\r\n", entry.Snapshot.Entries.Select(en => "  - " + en.Description));
	}

	async void BtnRevert_Click(object sender, RoutedEventArgs e)
	{
		if (_reverting || SnapshotList.SelectedItem is not SnapshotEntry entry)
			return;

		var result = MessageBox.Show(this,
			$"Undo the {entry.Snapshot.Entries.Count} changes from {entry.Snapshot.CreatedAt:yyyy-MM-dd HH:mm:ss}?",
			"Confirm revert", MessageBoxButton.YesNo, MessageBoxImage.Warning);
		if (result != MessageBoxResult.Yes)
			return;

		_reverting = true;
		SetUiEnabled(false);
		LogBox.Clear();

		await System.Threading.Tasks.Task.Run(() => RevertRunner.Apply(entry.Snapshot, AppendLog));

		AppendLog("");
		AppendLog("Revert finished. A reboot is recommended.");
		_reverting = false;
		SetUiEnabled(true);
	}

	void AppendLog(string message)
	{
		Dispatcher.Invoke(() =>
		{
			LogBox.AppendText(message + "\r\n");
			LogBox.ScrollToEnd();
		});
	}

	void SetUiEnabled(bool enabled)
	{
		SnapshotList.IsEnabled = enabled;
		BtnRevert.IsEnabled = enabled && SnapshotList.SelectedItem is not null;
		BtnClose.IsEnabled = enabled;
	}

	void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
