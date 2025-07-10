using EventManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

public class StatisticsController : Controller
{
    private readonly EventManagementDb1Context _context;

    public StatisticsController(EventManagementDb1Context context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var events = _context.Events
            .Include(e => e.Tickets)
            .Where(e => !e.IsDeleted)
            .ToList();

        var eventStats = events.Select(e => new
        {
            e.Title,
            e.Price,
            TicketsSold = e.Tickets.Where(t => t.IsPaid).Sum(t => t.Quantity),
            Revenue = e.Tickets.Where(t => t.IsPaid).Sum(t => t.Quantity * e.Price)
        }).ToList();

        ViewBag.EventStats = eventStats;

        return View();
    }

    public IActionResult ExportToExcel()
    {
        var events = _context.Events
            .Include(e => e.Tickets)
            .Where(e => !e.IsDeleted)
            .ToList();

        var eventStats = events.Select(e => new
        {
            e.Title,
            e.Price,
            TicketsSold = e.Tickets.Where(t => t.IsPaid).Sum(t => t.Quantity),
            Revenue = e.Tickets.Where(t => t.IsPaid).Sum(t => t.Quantity * e.Price)
        }).ToList();

        // ✅ Thiết lập license đúng cho EPPlus 8
        ExcelPackage.License.SetNonCommercialPersonal("Nguyen Viet Hung");

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Thống kê sự kiện");

        worksheet.Cells[1, 1].Value = "Tên sự kiện";
        worksheet.Cells[1, 2].Value = "Giá vé";
        worksheet.Cells[1, 3].Value = "Số vé đã bán";
        worksheet.Cells[1, 4].Value = "Doanh thu";

        for (int i = 0; i < eventStats.Count; i++)
        {
            worksheet.Cells[i + 2, 1].Value = eventStats[i].Title;
            worksheet.Cells[i + 2, 2].Value = eventStats[i].Price;
            worksheet.Cells[i + 2, 3].Value = eventStats[i].TicketsSold;
            worksheet.Cells[i + 2, 4].Value = eventStats[i].Revenue;
        }

        worksheet.Cells.AutoFitColumns();
        var stream = new MemoryStream(package.GetAsByteArray());

        return File(
            stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "EventStatistics.xlsx");
    }

}
