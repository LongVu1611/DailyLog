using ClosedXML.Excel;
using System.IO;
using DailyLogAssistant.Data;
using DailyLogAssistant.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DailyLogAssistant.Services;

public sealed class ExcelReportService(
    IDbContextFactory<AppDbContext> factory,
    LogService logs) : IExcelReportService
{

    public async Task ExportWeeklyReportAsync(
        DateOnly from, DateOnly to, string path, CancellationToken cancellationToken = default)
    {
        if (to < from) throw new ArgumentException("The report end date must not be before the start date.");
        var all = await logs.SearchAsync(new LogQuery(CategoryId: 1, From: from, To: to.AddDays(7)),
            cancellationToken);
        var completed = all.Where(log => log.Date <= to && log.Status == "Completed").ToList();
        var incomplete = all.Where(log => log.Date <= to && log.Status != "Completed" && log.Status != "Planned").ToList();
        var planned = all.Where(log => log.Date > to &&
            (log.Status == "Planned" || log.Status == "In Progress")).ToList();
        var templatePath = await GetTemplatePathAsync(cancellationToken);

        using var workbook = !string.IsNullOrWhiteSpace(templatePath) && File.Exists(templatePath)
            ? new XLWorkbook(templatePath)
            : new XLWorkbook();
        var sheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("Báo cáo tuần");
        sheet.Name = "Báo cáo tuần";
        if (!string.IsNullOrWhiteSpace(templatePath) && File.Exists(templatePath) &&
            TryWriteTemplate(sheet, from, to, completed, incomplete, planned))
        {
            workbook.SaveAs(path);
            Log.Information("Excel work report exported from template to {Path}", path);
            return;
        }

        CreateReportSheet(sheet, from, to, completed, incomplete, planned);
        workbook.SaveAs(path);
        Log.Information("Excel work report exported to {Path}", path);
    }

    private async Task<string> GetTemplatePathAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var settings = await db.Settings.AsNoTracking().SingleOrDefaultAsync(item => item.Id == 1, cancellationToken);
        return settings?.WorkReportTemplatePath ?? "";
    }

    private bool TryWriteTemplate(
        IXLWorksheet sheet, DateOnly from, DateOnly to, List<LogEntry> completed,
        List<LogEntry> incomplete, List<LogEntry> planned)
    {
        List<(string Header, List<LogEntry> Entries, bool IsIncomplete)> sections =
        [
            ("VIỆC ĐÃ HOÀN THÀNH TRONG TUẦN", completed, false),
            ("VIỆC CHƯA HOÀN THÀNH TRONG TUẦN", incomplete, true),
            ("KẾ HOẠCH CÔNG VIỆC TUẦN TIẾP THEO", planned, false)
        ];
        var anchors = new Dictionary<string, IXLCell>(StringComparer.Ordinal);
        foreach (var row in sheet.RowsUsed())
        foreach (var cell in row.CellsUsed())
        {
            var text = cell.GetString().Trim();
            foreach (var section in sections)
                if (text.Contains(section.Header, StringComparison.OrdinalIgnoreCase))
                    anchors.TryAdd(section.Header, cell);
        }
        if (anchors.Count != sections.Count) return false;

        foreach (var section in sections.OrderByDescending(item => anchors[item.Header].Address.RowNumber))
        {
            var anchor = anchors[section.Header];
            var headerRow = anchor.Address.RowNumber + 1;
            var dataRow = headerRow + 1;
            var nextAnchor = anchors.Values.Where(cell => cell.Address.RowNumber > anchor.Address.RowNumber)
                .MinBy(cell => cell.Address.RowNumber);
            var lastUsedRow = sheet.LastRowUsed()?.RowNumber() ?? dataRow;
            var availableRows = nextAnchor is null
                ? Math.Max(0, lastUsedRow - dataRow + 1)
                : Math.Max(0, nextAnchor.Address.RowNumber - dataRow);
            var rowsToInsert = section.Entries.Count - availableRows;
            if (rowsToInsert > 0)
            {
                var insertAt = nextAnchor?.Address.RowNumber ?? lastUsedRow + 1;
                sheet.Row(insertAt).InsertRowsAbove(rowsToInsert);
                for (var rowNumber = insertAt; rowNumber < insertAt + rowsToInsert; rowNumber++)
                    for (var column = 1; column <= 5; column++)
                        sheet.Cell(rowNumber, column).Style = sheet.Cell(dataRow, column).Style;
            }
            var mapping = GetColumnMapping(sheet, headerRow);
            for (var index = 0; index < section.Entries.Count; index++)
                WriteTemplateRow(sheet, dataRow + index, index + 1, section.Entries[index],
                    mapping, section.IsIncomplete);
        }
        sheet.Cell(1, 1).Value = $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}";
        return true;
    }

    private static ColumnMapping GetColumnMapping(IXLWorksheet sheet, int headerRow)
    {
        var columns = sheet.Row(headerRow).CellsUsed().ToDictionary(
            cell => cell.Address.ColumnNumber, cell => cell.GetString().Trim().ToLowerInvariant());
        int Find(params string[] names) => columns.FirstOrDefault(pair =>
            names.Any(name => pair.Value.Contains(name, StringComparison.Ordinal))).Key;
        return new ColumnMapping(
            Find("stt", "no.") is var number && number > 0 ? number : 1,
            Find("thời gian", "date", "time") is var date && date > 0 ? date : 2,
            Find("công việc", "task", "work") is var task && task > 0 ? task : 3,
            Find("kết quả", "result") is var result && result > 0 ? result : 4,
            Find("ghi chú", "notes") is var notes && notes > 0 ? notes : 5,
            Find("lý do", "đề xuất", "problem") is var problem && problem > 0 ? problem : 5);
    }

    private static void WriteTemplateRow(
        IXLWorksheet sheet, int row, int number, LogEntry entry, ColumnMapping mapping, bool incomplete)
    {
        sheet.Cell(row, mapping.Number).Value = number;
        sheet.Cell(row, mapping.Date).Value = entry.Date.ToDateTime(TimeOnly.MinValue);
        sheet.Cell(row, mapping.Date).Style.DateFormat.Format = "dd/MM/yyyy";
        sheet.Cell(row, mapping.Task).Value = entry.Title;
        sheet.Cell(row, mapping.Result).Value =
            string.IsNullOrWhiteSpace(entry.Result)
                ? entry.Status == "Planned" ? "" : entry.Status
                : entry.Result;
        sheet.Cell(row, incomplete ? mapping.Problem : mapping.Notes).Value =
            incomplete ? entry.Problems : entry.Notes;
        sheet.Row(row).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    public static void CreateDefaultTemplate(string path)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Báo cáo tuần");
        CreateReportSheet(sheet, new DateOnly(2000, 1, 1), new DateOnly(2000, 1, 7), [], [], []);
        sheet.Cell(1, 1).Value = "Từ ngày - Đến ngày";
        workbook.SaveAs(path);
    }

    private static void CreateReportSheet(
        IXLWorksheet sheet, DateOnly from, DateOnly to, List<LogEntry> completed,
        List<LogEntry> incomplete, List<LogEntry> planned)
    {
        sheet.Clear();
        sheet.Style.Font.FontName = "Segoe UI";
        var row = 1;
        sheet.Range(row, 1, row, 5).Merge();
        sheet.Cell(row, 1).Value = $"{from:dd/MM/yyyy} - {to:dd/MM/yyyy}";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 16;
        sheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row += 2;
        row = WriteSection(sheet, row, "VIỆC ĐÃ HOÀN THÀNH TRONG TUẦN", completed, false);
        row = WriteSection(sheet, row + 1, "VIỆC CHƯA HOÀN THÀNH TRONG TUẦN", incomplete, true);
        _ = WriteSection(sheet, row + 1, "KẾ HOẠCH CÔNG VIỆC TUẦN TIẾP THEO", planned, false);

        sheet.Column(1).Width = 8;
        sheet.Column(2).Width = 16;
        sheet.Column(3).Width = 42;
        sheet.Column(4).Width = 35;
        sheet.Column(5).Width = 38;
        sheet.Columns(1, 5).Style.Alignment.WrapText = true;
        sheet.Columns(1, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.SheetView.FreezeRows(3);
    }

    private static int WriteSection(IXLWorksheet sheet, int row, string title, List<LogEntry> logs, bool incomplete)
    {
        sheet.Range(row, 1, row, 5).Merge();
        var heading = sheet.Cell(row, 1);
        heading.Value = title;
        heading.Style.Font.Bold = true;
        heading.Style.Font.FontSize = 12;
        heading.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDE9E1");
        heading.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        row++;
        var columns = incomplete
            ? new[] { "STT", "Thời gian", "Công việc", "Kết quả", "Lý do, đề xuất cần giúp đỡ" }
            : new[] { "STT", "Thời gian", "Công việc", "Kết quả", "Ghi chú" };
        for (var column = 0; column < columns.Length; column++)
        {
            var cell = sheet.Cell(row, column + 1);
            cell.Value = columns[column];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2EF");
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        row++;
        for (var index = 0; index < logs.Count; index++, row++)
        {
            var item = logs[index];
            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = item.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy";
            sheet.Cell(row, 3).Value = item.Title;
            sheet.Cell(row, 4).Value = string.IsNullOrWhiteSpace(item.Result)
                ? item.Status == "Planned" ? "" : item.Status
                : item.Result;
            sheet.Cell(row, 5).Value = incomplete ? item.Problems : item.Notes;
            for (var column = 1; column <= 5; column++)
                sheet.Cell(row, column).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        if (logs.Count == 0)
            for (var column = 1; column <= 5; column++)
                sheet.Cell(row, column).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        return row;
    }
}
