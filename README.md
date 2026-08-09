# windows-debloat

A toolkit that removes the parts of Windows 10/11 that make the system heavy,
slow, or annoying — preinstalled bloatware, telemetry, Bing search in Start,
ads/suggestions, Widgets, Copilot, Game DVR, and a few hungry services.
Pure C# / .NET 9 WPF, no PowerShell or third-party tools at runtime.

It deliberately does **not** touch Windows Defender, Windows Update, UAC, or
the firewall.

## Project layout

| Path | What it is |
|---|---|
| `WindowsDebloat.sln` | Solution file. |
| `src/WindowsDebloat/WindowsDebloat.csproj` | The WPF app (`net9.0-windows10.0.19041.0`). |
| `src/WindowsDebloat/MainWindow.xaml(.cs)` | The UI: checkbox list, options, progress bar, log pane. |
| `src/WindowsDebloat/Catalog/` | Static data — the list of apps and tweaks shown in the UI. |
| `src/WindowsDebloat/Actions/` | What each tweak/app-removal actually does, run on a background thread. |
| `src/WindowsDebloat/Helpers/` | Registry, service, scheduled task, Appx (WinRT `PackageManager`) and System Restore (WMI) helpers. |
| `setup/WindowsDebloat.iss` | [Inno Setup](https://jrsoftware.org/isinfo.php) script that packages the published exe into `WindowsDebloat-Setup-<version>.exe`. |

## Building and running

Requires the [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

```powershell
dotnet build -c Release
dotnet run --project src/WindowsDebloat
```

The app carries a `requireAdministrator` app manifest, so double-clicking the
built/published exe triggers a UAC prompt automatically — no self-elevation
code needed.

## Publishing a standalone exe

```powershell
dotnet publish src/WindowsDebloat -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

This produces `publish\WindowsDebloat.exe` — a single, self-contained file
(bundles the .NET 9 runtime, ~150 MB) that runs on any Windows 10/11 x64
machine with no prerequisites.

## Building the installer

Requires [Inno Setup 6](https://jrsoftware.org/isdl.php) (`winget install
JRSoftware.InnoSetup`). Publish the standalone exe first (previous step),
then compile the installer:

```powershell
iscc setup\WindowsDebloat.iss
```

This produces `setup\Output\WindowsDebloat-Setup-<version>.exe` — a ~47 MB
compressed installer that installs to Program Files, adds Start Menu
shortcuts, an optional desktop icon, and a proper uninstaller entry. It
requires admin rights to install (and the app itself always requires admin
to run).

## Usage

Launch `WindowsDebloat.exe` (or `dotnet run`). It opens a window with three
sections:

- **Preinstalled apps** — one checkbox per app (Cortana, News, Solitaire,
  Teams Chat, Xbox apps, Candy Crush, …). Each can be reinstalled from the
  Microsoft Store later. Mail & Calendar, OneNote and Xbox Identity Provider
  are unchecked by default — read their descriptions.
- **System tweaks (recommended)** — telemetry, Bing search, ads, Widgets,
  Copilot, Game DVR, Edge background, SysMain, … checked by default.
- **Advanced** — OneDrive removal, Xbox services, search indexing,
  background apps, visual effects, hibernation. Unchecked by default and
  the app asks for confirmation before running them.

Buttons: *Select recommended* restores the default selection, *Select all* /
*Select none* do what they say. Options let you skip the System Restore point
or the Explorer restart. Click **Apply selected**; tasks run on a background
thread with live output in the log pane (also saved to
`debloat-gui-*.log` next to the exe). Reboot when it finishes.

The app is **idempotent** — running it again is safe. A System Restore point
is created first via WMI (skip with the checkbox; Windows only allows one per
24 h).

## How it works (no PowerShell at runtime)

- **Registry** edits use `Microsoft.Win32.Registry` directly.
- **Services** are stopped via `System.ServiceProcess.ServiceController` and
  disabled via a P/Invoke call to `advapi32.dll` (`ChangeServiceConfig`).
- **Scheduled tasks** are disabled by shelling out to `schtasks.exe`
  (a built-in Windows tool, not PowerShell).
- **Store apps** are removed/deprovisioned via the WinRT
  `Windows.Management.Deployment.PackageManager` API, projected directly
  into .NET.
- **System Restore** points are created via WMI (`root\default:SystemRestore`),
  the same mechanism `Checkpoint-Computer` uses internally.
- **OneDrive removal** runs `OneDriveSetup.exe /uninstall` (falls back to
  `winget`); **hibernation** toggling runs `powercfg.exe /hibernate off`.

## What the toggles do

### Safe defaults (on)

| Toggle | What it does |
|---|---|
| Preinstalled apps | Uninstalls + deprovisions the selected Store apps. All reinstallable from the Store. |
| Telemetry | Disables the DiagTrack service, the `CompatTelRunner.exe` scheduled tasks (a classic random 100 %-disk cause) and CEIP tasks; sets telemetry policy to minimum. |
| Bing search | Stops Start-menu search from sending keystrokes to Bing and showing web results. |
| Ads & suggestions | Kills Content Delivery Manager: sponsored auto-installed apps, Start/lock-screen/Settings tips and ads, advertising ID, Explorer OneDrive ads, "finish setting up your device" nag, Win11 Start "recommendations". |
| Widgets & News | Win10: News & Interests off. Win11: Widgets policy off + removes the Windows Web Experience Pack (the permanent WebView2 RAM hog behind it). |
| Copilot & Recall | Copilot policy + taskbar button off, removes the Copilot app, disables Recall screen analysis (no-op on non-Copilot+ PCs). |
| Game DVR | Turns off Xbox background game recording (measurable FPS cost). |
| Delivery Optimization P2P | Updates still download normally; your PC just stops uploading them to other machines. |
| Edge background | Edge Startup Boost, background mode and sidebar off — no more Edge processes when Edge is closed. |
| Activity history | Stops publishing/uploading activity history. |
| SysMain | Disables Superfetch — the #1 "100 % disk" culprit on HDDs, pointless on SSDs. |

### Opt-in (off by default)

| Toggle | What it does / why it's opt-in |
|---|---|
| OneDrive removal | Full uninstall. Local files in `%USERPROFILE%\OneDrive` are kept, but sync stops. |
| Xbox services | Disables `XblAuthManager`, `XblGameSave`, `XboxNetApiSvc`, `XboxGipSvc`. Breaks Game Pass, Xbox sign-in, and Minecraft launcher login. |
| Search indexing | Disables `WSearch`. Background disk churn stops, but file search in Start/Explorer becomes slow. |
| Background apps | Win10 only: global "background apps" off. Store-app notifications and alarms may stop working. |
| Visual effects | Animations and transparency off — noticeably snappier on weak hardware, but looks plain. |
| Hibernation | `powercfg /h off`. Frees disk (~40 % of RAM size) and disables Fast Startup (which itself causes weird boot issues on some machines), but removes Hibernate as a power option. |

## Reverting

- **Everything at once:** run `rstrui.exe` and pick the "Windows Debloat
  Toolkit" restore point.
- **Apps:** reinstall from the Microsoft Store.
- **Services:**
  ```powershell
  Set-Service DiagTrack -StartupType Automatic; Start-Service DiagTrack
  Set-Service SysMain   -StartupType Automatic; Start-Service SysMain
  sc.exe config WSearch start= delayed-auto; Start-Service WSearch
  ```
- **Registry policies:** most settings live under
  `HKLM:\SOFTWARE\Policies\Microsoft\...` or
  `HKCU:\Software\Policies\Microsoft\...` — delete the key to restore default
  behavior, e.g.:
  ```powershell
  Remove-Item "HKLM:\SOFTWARE\Policies\Microsoft\Dsh" -Recurse
  Remove-Item "HKCU:\SOFTWARE\Policies\Microsoft\Windows\Explorer" -Recurse
  ```
- **Hibernation:** `powercfg /h on`
- **OneDrive:** reinstall from https://www.microsoft.com/microsoft-365/onedrive/download
- **Widgets (Win11):** `winget install 9MSSGKG348SP` (Windows Web Experience Pack)

## Notes

- Works on Windows 10 and 11, Home included (no gpedit needed — the policy
  registry keys are written directly). On Home/Pro the telemetry level can
  only be lowered to "Required", not fully off; that's a Windows limitation.
- Scheduled tasks and services that don't exist on your build are skipped and
  logged, not errors.
- Run at your own risk; review the selection before running. The restore
  point is your undo button.
