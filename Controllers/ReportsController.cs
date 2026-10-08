using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProductionMeeting.Helpers;
using ProductionMeeting.Models;
using ProductionMeeting.Services;
using ProductionMeeting.ViewModels.Reports;

namespace ProductionMeeting.Controllers;

[Authorize(Policy = PolicyNames.RequireViewerOrAbove)]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(IReportService reportService, UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _userManager = userManager;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;
    private bool IsAdmin => User.IsInRole(Roles.Admin);

    public async Task<IActionResult> Index(ReportIndexViewModel filters, CancellationToken cancellationToken)
    {
        var vm = await _reportService.GetReportAsync(filters, CurrentUserId, IsAdmin, cancellationToken);
        return View(vm);
    }

    public async Task<IActionResult> Weekly(WeeklyReportViewModel filters, CancellationToken cancellationToken)
    {
        var vm = await _reportService.GetWeeklyReportAsync(filters, CurrentUserId, IsAdmin, cancellationToken);
        return View(vm);
    }

    public async Task<IActionResult> ExportWeekly(WeeklyReportViewModel filters, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetWeeklyReportAsync(filters, CurrentUserId, IsAdmin, cancellationToken);
        if (report.RangeError != null)
        {
            return BadRequest(report.RangeError);
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Weekly Comparison");
        var lastColumn = 5 + report.Weeks.Count * 4;
        sheet.Cell(1, 1).Value = $"Weekly KPI Comparison — {report.StartWeekStart:dd MMM yyyy} to {report.EndWeekStart:dd MMM yyyy}";
        sheet.Range(1, 1, 1, lastColumn).Merge();
        sheet.Cell(1, 1).Style.Font.Bold = true;

        sheet.Cell(3, 1).Value = "Plant";
        sheet.Cell(3, 2).Value = "Shop";
        sheet.Cell(3, 3).Value = "Indicator";
        sheet.Cell(3, 4).Value = "KPI";
        sheet.Cell(3, 5).Value = "Unit";
        for (var weekIndex = 0; weekIndex < report.Weeks.Count; weekIndex++)
        {
            var column = 6 + weekIndex * 4;
            sheet.Cell(2, column).Value = report.Weeks[weekIndex].Label;
            sheet.Range(2, column, 2, column + 3).Merge();
            sheet.Cell(3, column).Value = "Value";
            sheet.Cell(3, column + 1).Value = "Target";
            sheet.Cell(3, column + 2).Value = "Change";
            sheet.Cell(3, column + 3).Value = "Status";
        }

        for (var column = 1; column <= lastColumn; column++)
        {
            sheet.Cell(2, column).Style.Font.Bold = true;
            sheet.Cell(3, column).Style.Font.Bold = true;
        }

        var row = 4;
        foreach (var item in report.Rows)
        {
            sheet.Cell(row, 1).Value = item.PlantName;
            sheet.Cell(row, 2).Value = item.LineName;
            sheet.Cell(row, 3).Value = item.IndicatorCode;
            sheet.Cell(row, 4).Value = item.KpiDescription;
            sheet.Cell(row, 5).Value = item.UnitName;
            for (var weekIndex = 0; weekIndex < item.Values.Count; weekIndex++)
            {
                var value = item.Values[weekIndex];
                var column = 6 + weekIndex * 4;
                sheet.Cell(row, column).Value = value.Value;
                sheet.Cell(row, column + 1).Value = value.Target;
                sheet.Cell(row, column + 2).Value = value.Change;
                sheet.Cell(row, column + 3).Value = value.Status;
            }
            row++;
        }

        if (report.Rows.Count > 0)
        {
            sheet.Range(3, 1, row - 1, lastColumn).SetAutoFilter();
        }
        sheet.SheetView.FreezeRows(3);
        sheet.SheetView.FreezeColumns(5);
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"ProductionMeeting_WeeklyReport_{report.StartWeekStart:yyyyMMdd}_{report.EndWeekStart:yyyyMMdd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<IActionResult> Export(ReportIndexViewModel filters, CancellationToken cancellationToken)
    {
        var rows = await _reportService.GetReportRowsForExportAsync(filters, CurrentUserId, IsAdmin, cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("KPI Report");

        string[] headers =
        {
            "Date", "Plant", "Line", "Indicator Code", "Indicator", "KPI", "Unit", "Model",
            "F26", "F27 / L4 Target", "Week", "Month Cum.", "YTD", "Remarks"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var r in rows)
        {
            sheet.Cell(row, 1).Value = r.MeetingDate;
            sheet.Cell(row, 1).Style.DateFormat.Format = "dd-MMM-yyyy";
            sheet.Cell(row, 2).Value = r.PlantName;
            sheet.Cell(row, 3).Value = r.LineName;
            sheet.Cell(row, 4).Value = r.IndicatorCode;
            sheet.Cell(row, 5).Value = r.IndicatorName;
            sheet.Cell(row, 6).Value = r.KpiDescription;
            sheet.Cell(row, 7).Value = r.UnitName;
            sheet.Cell(row, 8).Value = r.ModelName ?? "";
            sheet.Cell(row, 9).Value = r.F26Value;
            sheet.Cell(row, 10).Value = r.F27Value;
            sheet.Cell(row, 11).Value = r.WeekValue;
            sheet.Cell(row, 12).Value = r.MonthCumValue;
            sheet.Cell(row, 13).Value = r.YtdValue;
            sheet.Cell(row, 14).Value = r.Remarks ?? "";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"ProductionMeeting_KPIReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
