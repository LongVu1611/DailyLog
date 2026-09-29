using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DailyLogAssistant.Localization;
using DailyLogAssistant.Services;
using DailyLogAssistant.ViewModels;
using Microsoft.Win32;

namespace DailyLogAssistant.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ExportService _exports;
    private readonly IExcelReportService _excelReports;
    private readonly ILetterExportService _letterExports;
    private readonly INotificationService _notifications;
    private readonly System.Windows.Forms.NotifyIcon _trayIcon;
    private bool _exitRequested;

    public MainWindow(
        MainViewModel viewModel,
        ExportService exports,
        IExcelReportService excelReports,
        ILetterExportService letterExports,
        INotificationService notifications)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _exports = exports;
        _excelReports = excelReports;
        _letterExports = letterExports;
        _notifications = notifications;
        DataContext = viewModel;
        _viewModel.ReminderRequested += OnReminderRequested;
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        if (_notifications is NotificationService notificationService)
            notificationService.NotificationRequested += OnNotificationRequested;
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = LocalizationService.Translate("Personal Log Manager"),
            Visible = true,
            ContextMenuStrip = CreateTrayMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowDashboard();
        _trayIcon.BalloonTipClicked += (_, _) => ShowDailyLog();
        Microsoft.Win32.SystemEvents.SessionEnding += OnSessionEnding;
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Minimized && _viewModel.Settings.MinimizeToTray)
                HideToTray();
        };
        Closing += OnClosing;
    }

    public void ShowDashboard()
    {
        Dispatcher.Invoke(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
    }

    public void ShowDailyLog()
    {
        ShowDashboard();
        _ = _viewModel.OpenTodayWorkCommand.ExecuteAsync(null);
    }

    private System.Windows.Forms.ContextMenuStrip CreateTrayMenu()
    {
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add(LocalizationService.Translate("Open"), null, (_, _) => ShowDashboard());
        menu.Items.Add(LocalizationService.Translate("Today's Work Log"), null, (_, _) => ShowDailyLog());
        menu.Items.Add(LocalizationService.Translate("New Log"), null, (_, _) =>
            Dispatcher.Invoke(() => _viewModel.NewNoteCommand.Execute(null)));
        menu.Items.Add(LocalizationService.Translate("History"), null, (_, _) => NavigateFromTray("History"));
        menu.Items.Add(LocalizationService.Translate("Export"), null, (_, _) => NavigateFromTray("Export"));
        menu.Items.Add(LocalizationService.Translate("Settings"), null, (_, _) => NavigateFromTray("Settings"));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(LocalizationService.Translate("Exit"), null, (_, _) => Dispatcher.Invoke(() =>
        {
            _exitRequested = true;
            System.Windows.Application.Current.Shutdown();
        }));
        return menu;
    }

    private void NavigateFromTray(string section) => Dispatcher.Invoke(() =>
    {
        ShowDashboard();
        _viewModel.ActiveSection = section;
    });

    private void HideToTray()
    {
        Hide();
        _trayIcon.ShowBalloonTip(1200, LocalizationService.Translate("Personal Log Manager"),
            LocalizationService.Translate("The app is still running in the notification area."),
            System.Windows.Forms.ToolTipIcon.Info);
    }

    private void OnReminderRequested(object? sender, EventArgs e)
    {
        ShowDashboard();
        _notifications.Show(LocalizationService.Translate("Work Log Reminder"),
            LocalizationService.Translate("Time to record your work today."));
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        _trayIcon.Text = LocalizationService.Translate("Personal Log Manager");
        var menu = _trayIcon.ContextMenuStrip;
        _trayIcon.ContextMenuStrip = CreateTrayMenu();
        menu?.Dispose();
    }

    private void OnNotificationRequested(object? sender, NotificationEventArgs e)
    {
        if (_viewModel.Settings.ShowNotification)
            _trayIcon.ShowBalloonTip(8000, e.Title, e.Message, System.Windows.Forms.ToolTipIcon.Info);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_exitRequested && _viewModel.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _viewModel.ReminderRequested -= OnReminderRequested;
        LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
        if (_notifications is NotificationService notificationService)
            notificationService.NotificationRequested -= OnNotificationRequested;
        Microsoft.Win32.SystemEvents.SessionEnding -= OnSessionEnding;
    }

    private void OnSessionEnding(object? sender, Microsoft.Win32.SessionEndingEventArgs e)
    {
        _exitRequested = true;
        Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
    }

    private async void History_DoubleClick(object sender, MouseButtonEventArgs e) =>
        await _viewModel.OpenSelectedLogCommand.ExecuteAsync(null);

    private void QuickAdd_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.PrepareQuickAdd();
        new QuickAddWindow(_viewModel) { Owner = this }.ShowDialog();
    }

    private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedLog is null) return;
        var result = System.Windows.MessageBox.Show(
            string.Format(LocalizationService.Instance.CurrentCulture,
                LocalizationService.Translate("Delete '{0}'?"), _viewModel.SelectedLog.Title),
            LocalizationService.Translate("Delete log"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes) await _viewModel.DeleteLogCommand.ExecuteAsync(null);
    }

    private async void ExportCenter_Click(object sender, RoutedEventArgs e) => await ExportCenterAsync(_viewModel.ExportFormat);

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "SQLite backup (*.db)|*.db",
            FileName = $"PersonalLogManager-backup-{DateTime.Today:yyyyMMdd}.db"
        };
        if (dialog.ShowDialog() == true)
            await ExportAsync(() => _exports.ExportDatabaseAsync(dialog.FileName));
    }

    private async Task ExportCenterAsync(string format)
    {
        if (format == "Excel" && _viewModel.ExportCategory != "WORK")
        {
            System.Windows.MessageBox.Show(LocalizationService.Translate(
                    "Weekly Excel reports are available for the WORK category."),
                LocalizationService.Translate("Export"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var query = _viewModel.GetExportQuery();
        var fileName = $"{_viewModel.ExportCategory.ToLowerInvariant()}-logs-{DateTime.Today:yyyyMMdd}";
        var entries = await _viewModel.GetLogsForExportAsync(query);
        if (format == "TXT" && entries.Count == 1)
        {
            var entry = entries[0];
            var prefix = entry.Category?.Name == "LETTER" ? "Letter" :
                entry.Category?.Name == "NOTE" ? "Note" : entry.Category?.Name ?? "Log";
            fileName = $"{prefix}-{Slugify(entry.Title)}-{entry.Date:yyyy-MM-dd}";
        }
        var dialog = format switch
        {
            "Excel" => new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
                FileName = $"Bao-Cao-Tuan-{_viewModel.ExportStartDate:yyyy-MM-dd}.xlsx"
            },
            "CSV" => new Microsoft.Win32.SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = $"{fileName}.csv" },
            "TXT" => new Microsoft.Win32.SaveFileDialog { Filter = "Text files (*.txt)|*.txt", FileName = $"{fileName}.txt" },
            _ => new Microsoft.Win32.SaveFileDialog { Filter = "Markdown files (*.md)|*.md", FileName = $"{fileName}.md" }
        };
        if (dialog.ShowDialog() != true) return;
        await ExportAsync(async () =>
        {
            if (format == "Excel")
                await _excelReports.ExportWeeklyReportAsync(_viewModel.ExportStartDate,
                    _viewModel.ExportEndDate, dialog.FileName);
            else if (format == "CSV")
                await _exports.ExportCsvAsync(dialog.FileName, query);
            else if (format == "Markdown")
            {
                if (entries.Count == 1 && entries[0].Category?.Name == "LETTER")
                    await _letterExports.ExportMarkdownAsync(entries[0], dialog.FileName);
                else
                    await _exports.ExportMarkdownAsync(dialog.FileName, query);
            }
            else
            {
                if (entries.Count == 1 && entries[0].Category?.Name == "LETTER")
                    await _letterExports.ExportTxtAsync(entries[0], dialog.FileName);
                else
                {
                    var text = string.Join(Environment.NewLine + Environment.NewLine, entries.Select(entry =>
                        entry.Category?.Name == "LETTER"
                            ? LetterExportService.BuildText(entry)
                            : $"{entry.Title}{Environment.NewLine}{entry.DisplayContent}"));
                    await File.WriteAllTextAsync(dialog.FileName, text, new System.Text.UTF8Encoding(false));
                }
            }
        });
    }

    private async void ExportWeekly_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            FileName = $"Bao-Cao-Tuan-{_viewModel.ExportStartDate:yyyy-MM-dd}.xlsx"
        };
        if (dialog.ShowDialog() == true)
            await ExportAsync(() => _excelReports.ExportWeeklyReportAsync(
                _viewModel.ExportStartDate, _viewModel.ExportEndDate, dialog.FileName));
    }

    private async void ExportLetterTxt_Click(object sender, RoutedEventArgs e) => await ExportLetterAsync(markdown: false);
    private async void ExportLetterMarkdown_Click(object sender, RoutedEventArgs e) => await ExportLetterAsync(markdown: true);

    private async Task ExportLetterAsync(bool markdown)
    {
        if (_viewModel.SelectedCategory?.Name != "LETTER")
        {
            System.Windows.MessageBox.Show(LocalizationService.Translate(
                    "Choose the LETTER category to export a letter."),
                LocalizationService.Translate("Personal Log Manager"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var extension = markdown ? "md" : "txt";
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = markdown ? "Markdown files (*.md)|*.md" : "Text files (*.txt)|*.txt",
            FileName = $"Letter-{Slugify(_viewModel.Title)}-{_viewModel.SelectedDate:yyyy-MM-dd}.{extension}"
        };
        if (dialog.ShowDialog() != true) return;
        var draft = _viewModel.CreateDraft();
        await ExportAsync(() => markdown
            ? _letterExports.ExportMarkdownAsync(draft, dialog.FileName)
            : _letterExports.ExportTxtAsync(draft, dialog.FileName));
    }

    private void PreviewLetter_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedCategory?.Name != "LETTER")
        {
            System.Windows.MessageBox.Show(LocalizationService.Translate(
                    "Choose the LETTER category to preview a letter."),
                LocalizationService.Translate("Personal Log Manager"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        new LetterPreviewWindow(_viewModel.Title,
            LetterExportService.BuildText(_viewModel.CreateDraft())) { Owner = this }.ShowDialog();
    }

    private async void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
        {
            await _viewModel.SaveLogCommand.ExecuteAsync(null);
            e.Handled = true;
        }
        else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.N)
        {
            _viewModel.NewNoteCommand.Execute(null);
            e.Handled = true;
        }
    }

    private async void ChooseTemplate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx" };
        if (dialog.ShowDialog() != true) return;
        _viewModel.Settings.WorkReportTemplatePath = dialog.FileName;
        await _viewModel.SaveSettingsCommand.ExecuteAsync(null);
    }

    private async void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CategoryDialog();
        if (dialog.ShowDialog() != true) return;
        try { await _viewModel.CreateCategoryAsync(dialog.CategoryName, dialog.CategoryIcon, dialog.ColorHex); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(LocalizationService.TranslateException(ex),
                LocalizationService.Translate("Category"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteTag_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTag is null) return;
        var result = System.Windows.MessageBox.Show(string.Format(LocalizationService.Instance.CurrentCulture,
                LocalizationService.Translate("Delete tag '{0}'?"), _viewModel.SelectedTag.Name),
            LocalizationService.Translate("Delete tag"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes) await _viewModel.DeleteTagCommand.ExecuteAsync(null);
    }

    private static async Task ExportAsync(Func<Task> export)
    {
        try
        {
            await export();
            System.Windows.MessageBox.Show(LocalizationService.Translate("Export completed successfully."),
                LocalizationService.Translate("Personal Log Manager"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"{LocalizationService.Translate("Export failed.")}\n\n{LocalizationService.TranslateException(ex)}",
                LocalizationService.Translate("Personal Log Manager"),
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string Slugify(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var slug = string.Concat(value.Trim().Select(character =>
            invalid.Contains(character) || char.IsWhiteSpace(character) ? '-' : character));
        while (slug.Contains("--", StringComparison.Ordinal)) slug = slug.Replace("--", "-");
        return slug.Trim('-') is { Length: > 0 } clean ? clean : "untitled";
    }
}

internal sealed class CategoryDialog : Window
{
    private readonly System.Windows.Controls.TextBox _name = new()
    {
        MinWidth = 280, AcceptsReturn = false, Text = LocalizationService.Translate("New category")
    };
    private readonly System.Windows.Controls.TextBox _icon = new() { MinWidth = 280, AcceptsReturn = false, Text = "●" };
    private readonly System.Windows.Controls.TextBox _color = new() { MinWidth = 280, AcceptsReturn = false, Text = "#315C4C" };
    public string CategoryName => _name.Text.Trim();
    public string CategoryIcon => _icon.Text.Trim();
    public string ColorHex => _color.Text.Trim();

    public CategoryDialog()
    {
    Title = LocalizationService.Translate("Create custom category");
    WindowStartupLocation = WindowStartupLocation.CenterOwner;
    Owner = System.Windows.Application.Current.MainWindow;
    SizeToContent = SizeToContent.WidthAndHeight;
    ResizeMode = ResizeMode.NoResize;
    var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
    AddField(stack, LocalizationService.Translate("Name"), _name);
    AddField(stack, LocalizationService.Translate("Icon"), _icon);
    AddField(stack, LocalizationService.Translate("Color (hex)"), _color);
    var buttons = new System.Windows.Controls.StackPanel
    {
        Orientation = System.Windows.Controls.Orientation.Horizontal,
        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
    };
    var create = new System.Windows.Controls.Button
    {
        Content = LocalizationService.Translate("Create"), IsDefault = true, MinWidth = 80, Margin = new Thickness(4)
    };
    create.Click += (_, _) => DialogResult = !string.IsNullOrWhiteSpace(CategoryName);
    buttons.Children.Add(create);
    buttons.Children.Add(new System.Windows.Controls.Button
    {
        Content = LocalizationService.Translate("Cancel"), IsCancel = true, MinWidth = 80, Margin = new Thickness(4)
    });
    stack.Children.Add(buttons);
    Content = stack;
    }

    private static void AddField(
        System.Windows.Controls.Panel panel, string label, System.Windows.Controls.TextBox input)
    {
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });
        panel.Children.Add(input);
    }
}

internal sealed class LetterPreviewWindow : Window
{
    public LetterPreviewWindow(string title, string text)
    {
        Title = string.IsNullOrWhiteSpace(title) ? LocalizationService.Translate("Letter preview") :
            $"{LocalizationService.Translate("Preview")} — {title}";
        Width = 760;
        Height = 650;
        MinWidth = 560;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        Content = new System.Windows.Controls.TextBox
        {
            Text = text,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            FontSize = 16,
            Padding = new Thickness(20),
            BorderThickness = new Thickness(0),
            Background = System.Windows.Media.Brushes.Transparent
        };
    }
}
