# Daily Log Assistant

A private, offline Windows desktop app for recording one daily log, reviewing/searching previous entries, and getting a configurable end-of-day reminder. It uses WPF, MVVM, SQLite, and a Windows notification-area icon; no account, server, or network connection is required at runtime.

## Features

- Daily entries with work completed, blockers, tomorrow's plan, and notes; one entry per calendar day.
- Dashboard, searchable history, editable/deletable entries, and streak/month statistics.
- CSV and Markdown export, plus a SQLite database backup.
- Configurable local-time reminder, snooze, dismiss-for-today, unlock/resume checks, and a "Test reminder now" action.
- Notification-area menu, minimize-to-tray, current-user Windows startup option, and single-instance activation.
- Rolling Serilog files and a local SQLite database.

## Requirements

- Windows 10 or later.
- .NET 8 SDK to build and test. The published self-contained application does not require the SDK or a separate .NET runtime.

## Build and run

From the repository root:

```powershell
dotnet restore .\DailyLogAssistant.sln
dotnet build .\DailyLogAssistant.sln
dotnet run --project .\DailyLogAssistant\DailyLogAssistant.csproj
```

Run automated tests:

```powershell
dotnet test .\DailyLogAssistant.sln
```

Publish a self-contained Windows x64 application:

```powershell
dotnet publish .\DailyLogAssistant\DailyLogAssistant.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:PublishTrimmed=false `
  -o .\publish\win-x64
```

Run `.\publish\win-x64\DailyLogAssistant.exe`. The publish folder contains the standalone Windows executable and its required files. Keep the folder intact when distributing the app.

## Create an installer for another PC

Build the self-contained publish folder first using the command above. Install [Inno Setup 6](https://jrsoftware.org/isinfo.php) on the build PC, then compile the installer script from the repository root:

```powershell
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" .\installer\DailyLogAssistant.iss
```

The generated `.\publish\DailyLogAssistant-Setup-1.0.0.exe` installs the app for the current Windows user (no administrator access required), creates a Start Menu shortcut, offers an optional Desktop shortcut, and registers an uninstaller. Copy this setup file to the other 64-bit Windows PC and run it there. The app is self-contained; .NET does not need to be installed on the destination PC. The installer does not include existing user logs or settings.

## First use

Open **Settings** to choose a reminder time (24-hour local time, default `17:00`), enable reminders, and configure startup/tray behavior. **Test reminder now** exercises the reminder UI without waiting. Saving an entirely blank entry is rejected. Closing or minimizing the main window hides it in the notification area when **Minimize to tray** is enabled; choose **Exit** in the tray menu to quit.

The tray menu can open the dashboard, today's log, history, or settings. A second launch signals the running instance to show its dashboard.

## Data and logs

- Database: `%LOCALAPPDATA%\DailyLogAssistant\DailyLogAssistant.db`
- Logs: `%LOCALAPPDATA%\DailyLogAssistant\Logs\application-YYYYMMDD.log`

The database directory is created on first launch. Back up or export entries from the app before removing or replacing the database.

## Windows startup

Enable **Start application with Windows** in Settings and save. The app creates/removes a value under the current user's `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` key; administrator access is not required.

## Troubleshooting

- **The app does not start:** check the latest log file. If the database is damaged or inaccessible, close the app and restore a backup; do not delete the only copy of the database.
- **A reminder was missed:** reminders use the computer's local clock. Starting after the configured time, waking from sleep, or unlocking the session causes the app to check again if today's log is still missing.
- **The app appears closed:** it may be minimized to the notification area. Use the tray icon to reopen it or choose **Exit** to stop it.
- **Publish fails:** run `dotnet restore` and `dotnet publish` on Windows with the .NET 8 SDK installed. Publishing targets `win-x64`.
- **Windows notification behavior:** the app uses a notification-area balloon for compatibility; Windows notification settings may suppress balloons. The in-app reminder remains available while the app is running.

## Architecture

`DailyLogAssistant.sln` contains the WPF desktop app and its unit-test project. The app separates EF Core/SQLite data access, reminder/settings/export/statistics services, an MVVM view model, and the WPF views. `EnsureCreated` initializes the local database on first run. Settings and reminder state are stored locally in SQLite.

## Limitations

- Reminders run only while the application is running; launch-at-login is optional and must be enabled. A fully shut-down computer cannot display a reminder until Windows and the app start again.
- This initial version uses the system notification-area balloon rather than interactive Windows toast actions.
- Statistics use the current local calendar and do not count future days in the current month's completion-rate denominator.
