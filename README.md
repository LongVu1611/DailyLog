# Personal Log Manager

Personal Log Manager is an offline-first Windows desktop app for work logs, personal diary entries, letters, notes, and custom categories. Logs are stored locally in SQLite and can be searched, tagged, reported, and exported without an account, server, or network connection.

## Features

- **Log categories:** Work, Personal, Letter, Note, and user-created categories with an icon and color.
- **Work tracking:** project, task, status, result, blockers, notes, tags, and a daily work reminder.
- **Personal diary:** subject/person, mood, content, reminders-to-self, notes, and tags.
- **Letters:** recipient, title, opening, body, closing, and signature; clean UTF-8 TXT and Markdown export.
- **Tags:** comma-separated tags are created automatically while saving a log; the Tags page can rename, recolor, and delete tags.
- **History:** global search across titles, content, project, result, blockers, notes, recipient, and tags; category, tag, status, and date filters; edit, duplicate, and confirmed delete.
- **Weekly Excel report:** completed, unfinished, and following-week sections, with Vietnamese report columns and a bundled, replaceable workbook template.
- **Other exports:** filtered CSV, TXT, Markdown, and an online-safe SQLite backup.
- **Dashboard and statistics:** recent entries, work completion, streak, monthly/yearly totals, and category counts.
- **Settings:** local 17:00 work reminder, snooze, dismiss for today, tray behavior, Windows startup, Light/Dark/System theme, and accent color.
- Single-instance activation, notification-area menu, rolling Serilog logs, and migration of the previous Daily Log database.

The daily reminder only checks for a **WORK** log. A Personal, Letter, or Note entry never satisfies or triggers the work reminder.

## Requirements

- Windows 10/11, 64-bit.
- .NET 8 SDK to build and test.
- Inno Setup 6 to compile the optional Windows installer.

The self-contained published application does not require .NET to be installed on the destination computer.

## Build and run

Run these commands from the repository root in PowerShell:

```powershell
dotnet restore .\DailyLogAssistant.sln
dotnet build .\DailyLogAssistant.sln
dotnet run --project .\DailyLogAssistant\DailyLogAssistant.csproj
```

Run unit and SQLite/Excel integration tests:

```powershell
dotnet test .\DailyLogAssistant.sln
```

## Publish and create an installer

Publish a self-contained, single-file Windows x64 app (the Excel template is copied alongside the executable):

```powershell
dotnet publish .\DailyLogAssistant\DailyLogAssistant.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:PublishTrimmed=false `
  -o .\publish\win-x64
```

Run `.\publish\win-x64\PersonalLogManager.exe` directly, or compile the user-level setup program after installing Inno Setup 6:

```powershell
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" .\installer\DailyLogAssistant.iss
```

The installer is written to `.\publish\PersonalLogManager-Setup-2.0.0.exe`. It installs under the current user (no administrator access), adds a Start Menu shortcut, offers a Desktop shortcut, and registers an uninstaller. Copy the installer to another 64-bit Windows computer and run it there. User databases and settings are intentionally not included in the installer.

## First use

Select **Work**, **Personal**, **Letters**, or **Notes** in the sidebar to create an entry. Logs may be edited by opening them from History; delete asks for confirmation. Add one or more comma-separated tags in an entry, or manage tag names/colors in **Tags**. **+ Add custom category** asks for its name, icon, and color.

The **History** page supports full-text search plus category, tag, status, and date filtering. **Export** exports the selected date/category/tag range. The Excel option creates a weekly Work report; its date range also includes planned Work entries for the seven days after the report end date. The Letter editor has preview, TXT, and Markdown actions. Letter filenames use the `Letter-{slug}-{date}` pattern.

The default work reminder is **17:00 local time**. Change it in **Settings**, save, and use **Test reminder now** to exercise the UI. A reminder checks whether a Work entry exists for the day; it does not require a particular Work status. Snoozes are 15 minutes, 30 minutes, or one hour. Dismiss suppresses reminders for that date. Starting after the reminder time, resuming from sleep, or unlocking Windows causes another due check. Enable **Start application with Windows** to receive reminders after sign-in.

The app can minimize to the notification area. Use the tray menu to open the app, open today's Work editor, create a Note, open History/Export/Settings, or exit.

## Weekly Excel template

`DailyLogAssistant\Resources\WorkReportTemplate.xlsx` is the bundled default template. It contains the three report sections and Vietnamese column headings described in the project requirements. Change **Settings → Work report template** to select a different `.xlsx`; templates must contain recognizable completed, unfinished, and next-week section headings. The exporter discovers section/header rows and maps columns by their heading text, inserts rows when needed, and preserves existing workbook styles where possible. If a selected workbook cannot be mapped, export falls back to the bundled-style report layout.

The separate reference workbook named `Vu-Bao Cao Tuan - 21-9-2026.xlsx` was not present in the project files when this update was implemented. The bundled template follows the documented layout; select the original workbook in Settings to use its exact formatting if it is available.

## Database, settings, and logs

- SQLite database: `%LOCALAPPDATA%\PersonalLogManager\PersonalLogManager.db`
- Application logs: `%LOCALAPPDATA%\PersonalLogManager\Logs\application-YYYYMMDD.log`
- Default Excel template copied on first launch: `%LOCALAPPDATA%\PersonalLogManager\Templates\WorkReportTemplate.xlsx`
- The previous `%LOCALAPPDATA%\DailyLogAssistant\DailyLogAssistant.db` is copied and migrated on first launch, preserving daily entries and reminder settings. The old database is left untouched.

The schema contains `Logs`, `Tags`, `LogTags`, `Categories`, and `AppSettings`, with date/category/status/title indexes and a many-to-many tag relationship. Category-specific fields are nullable in meaning and stored as empty strings where not applicable. User-provided content remains on the local machine unless exported by the user.

## Troubleshooting and limitations

- **A reminder did not appear:** the app must be running (or enabled at Windows sign-in); reminders use local Windows time. Check that today's Work entry is missing, the reminder is enabled, and the date was not dismissed.
- **The app is not visible:** it may be in the notification area. Reopen it from the tray menu or choose **Exit** there.
- **An Excel template was not applied:** use `.xlsx` and ensure it contains recognizable section titles and column headings. The report remains exportable using the default layout if mapping fails.
- **Database migration:** migration preserves the previous Daily Log database and leaves the source file intact. The new database lives in the Personal Log Manager folder.
- **Windows notification delivery:** notifications use the Windows notification-area balloon API; Windows notification settings can suppress balloons.
- **Custom categories currently cannot be deleted from the UI.** Built-in categories are protected.
- Reminders require a running app; a powered-off computer cannot show one until the app starts after Windows boots.

## Architecture and dependencies

The WPF application uses CommunityToolkit.Mvvm, dependency injection and hosted services, EF Core/SQLite, Serilog, and ClosedXML. UI state is in the MVVM view model; data access, categories/tags, reminder, theme, letter export, and Excel reporting are separate services behind service contracts. `DailyLogAssistant.Tests` covers reminder/date logic, SQLite CRUD/search/migration, tag/date filters, letter export, and Excel report/template generation.
