using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using HotelManagement.Data;
using HotelManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Reports/Index (dashboard tổng quan)
        public IActionResult Index()
        {
            return View();
        }

        // API: Doanh thu 7 ngày gần nhất
        [HttpGet]
        public async Task<IActionResult> RevenueLast7Days()
        {
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-6);
            var invoices = await _context.Invoices
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<decimal>();
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                labels.Add(date.ToString("dd/MM"));
                var dailyTotal = invoices.Where(i => i.InvoiceDate.Date == date).Sum(i => i.PaidAmount);
                data.Add(dailyTotal);
            }
            return Json(new { labels, data });
        }

        // API: Doanh thu theo tháng trong năm hiện tại
        [HttpGet]
        public async Task<IActionResult> RevenueByMonth()
        {
            var year = DateTime.Now.Year;
            var invoices = await _context.Invoices
                .Where(i => i.InvoiceDate.Year == year)
                .ToListAsync();

            var labels = Enumerable.Range(1, 12).Select(m => $"Tháng {m}").ToList();
            var data = new List<decimal>();
            for (int m = 1; m <= 12; m++)
            {
                var monthlyTotal = invoices.Where(i => i.InvoiceDate.Month == m).Sum(i => i.PaidAmount);
                data.Add(monthlyTotal);
            }
            return Json(new { labels, data });
        }

        // API: Công suất phòng tháng hiện tại
        [HttpGet]
        public async Task<IActionResult> OccupancyRate()
        {
            var now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = startDate.AddMonths(1);
            var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

            var totalRooms = await _context.Rooms.CountAsync();
            if (totalRooms == 0) return Json(new { rate = 0 });

            var bookings = await _context.BookingDetails
                .Include(bd => bd.Booking)
                .Where(bd => (bd.Booking.Status == "CheckedIn" || bd.Booking.Status == "CheckedOut")
                             && bd.Booking.CheckInDate < endDate
                             && bd.Booking.CheckOutDate > startDate)
                .ToListAsync();

            int totalOccupiedDays = 0;
            foreach (var bd in bookings)
            {
                var checkIn = bd.Booking.CheckInDate < startDate ? startDate : bd.Booking.CheckInDate;
                var checkOut = bd.Booking.CheckOutDate > endDate ? endDate : bd.Booking.CheckOutDate;
                var days = (checkOut - checkIn).Days;
                if (days > 0) totalOccupiedDays += days;
            }

            var maxPossibleDays = totalRooms * daysInMonth;
            var rate = maxPossibleDays > 0 ? (double)totalOccupiedDays / maxPossibleDays * 100 : 0;
            return Json(new { rate = Math.Round(rate, 2) });
        }

        // API: Top 5 khách hàng thân thiết
        [HttpGet]
        public async Task<IActionResult> TopCustomers()
        {
            var topCustomers = await _context.Bookings
                .Include(b => b.Customer)
                .Where(b => b.Customer != null)
                .GroupBy(b => b.Customer.FullName)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var labels = topCustomers.Select(c => c.Name).ToList();
            var data = topCustomers.Select(c => c.Count).ToList();
            return Json(new { labels, data });
        }

        // GET: Báo cáo doanh thu theo tháng (chi tiết)
        public async Task<IActionResult> Revenue(int? year, int? month)
        {
            int reportYear = year ?? DateTime.Now.Year;
            int reportMonth = month ?? DateTime.Now.Month;

            var startDate = new DateTime(reportYear, reportMonth, 1);
            var endDate = startDate.AddMonths(1);

            var invoices = await _context.Invoices
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate < endDate)
                .ToListAsync();

            var totalRevenue = invoices.Sum(i => i.PaidAmount);
            var totalBookings = invoices.Count;
            var avgRevenue = totalBookings > 0 ? totalRevenue / totalBookings : 0;

            var dailyRevenue = invoices
                .GroupBy(i => i.InvoiceDate.Date)
                .Select(g => new RevenueReportViewModel
                {
                    Date = g.Key,
                    TotalRevenue = g.Sum(i => i.PaidAmount),
                    TotalBookings = g.Count(),
                    AverageRevenuePerBooking = g.Sum(i => i.PaidAmount) / g.Count()
                })
                .OrderBy(r => r.Date)
                .ToList();

            ViewBag.Year = reportYear;
            ViewBag.Month = reportMonth;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.AverageRevenue = avgRevenue;

            return View(dailyRevenue);
        }

        // GET: Báo cáo công suất phòng (chi tiết)
        public async Task<IActionResult> Occupancy(int? year, int? month)
        {
            int reportYear = year ?? DateTime.Now.Year;
            int reportMonth = month ?? DateTime.Now.Month;
            var daysInMonth = DateTime.DaysInMonth(reportYear, reportMonth);

            var startDate = new DateTime(reportYear, reportMonth, 1);
            var endDate = startDate.AddMonths(1);

            var bookings = await _context.BookingDetails
                .Include(bd => bd.Booking)
                .Include(bd => bd.Room)
                    .ThenInclude(r => r.RoomType)
                .Where(bd => (bd.Booking.Status == "CheckedIn" || bd.Booking.Status == "CheckedOut")
                             && bd.Booking.CheckInDate < endDate
                             && bd.Booking.CheckOutDate > startDate)
                .ToListAsync();

            var roomOccupancy = bookings
                .GroupBy(bd => bd.Room)
                .Select(g => new OccupancyReportViewModel
                {
                    RoomNumber = g.Key.RoomNumber,
                    RoomType = g.Key.RoomType?.Name ?? "Unknown",
                    TotalDaysOccupied = g.Sum(bd =>
                        Math.Max(0, (bd.Booking.CheckOutDate > endDate ? endDate : bd.Booking.CheckOutDate)
                                  .Subtract(bd.Booking.CheckInDate < startDate ? startDate : bd.Booking.CheckInDate)
                                  .Days)
                    )
                })
                .ToList();

            foreach (var room in roomOccupancy)
            {
                room.OccupancyRate = (decimal)room.TotalDaysOccupied / daysInMonth * 100;
            }

            ViewBag.Year = reportYear;
            ViewBag.Month = reportMonth;
            ViewBag.DaysInMonth = daysInMonth;
            ViewBag.TotalRooms = _context.Rooms.Count();

            return View(roomOccupancy.OrderByDescending(r => r.OccupancyRate));
        }

        // Xuất Excel báo cáo doanh thu
        [HttpGet]
        public async Task<IActionResult> ExportRevenueToExcel(int? year, int? month)
        {
            int reportYear = year ?? DateTime.Now.Year;
            int reportMonth = month ?? DateTime.Now.Month;

            var startDate = new DateTime(reportYear, reportMonth, 1);
            var endDate = startDate.AddMonths(1);

            var invoices = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate < endDate)
                .ToListAsync();

            // Tạo DataTable
            var dt = new DataTable();
            dt.Columns.Add("Ngày", typeof(string));
            dt.Columns.Add("Mã hóa đơn", typeof(int));
            dt.Columns.Add("Khách hàng", typeof(string));
            dt.Columns.Add("Tổng tiền (VNĐ)", typeof(decimal));
            dt.Columns.Add("Đã thanh toán (VNĐ)", typeof(decimal));
            dt.Columns.Add("Còn lại (VNĐ)", typeof(decimal));
            dt.Columns.Add("Trạng thái", typeof(string));

            foreach (var inv in invoices)
            {
                dt.Rows.Add(
                    inv.InvoiceDate.ToString("dd/MM/yyyy"),
                    inv.Id,
                    inv.Booking?.Customer?.FullName ?? "Khách lẻ",
                    inv.TotalAmount,
                    inv.PaidAmount,
                    inv.TotalAmount - inv.PaidAmount,
                    inv.Status == "Paid" ? "Đã thanh toán" : (inv.Status == "Partial" ? "Một phần" : "Chưa thanh toán")
                );
            }

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add(dt, "Doanh thu");

                // Định dạng tiêu đề
                ws.Cell(1, 1).Value = $"BÁO CÁO DOANH THU THÁNG {reportMonth}/{reportYear}";
                ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(16);

                // Tổng cộng
                int lastRow = dt.Rows.Count + 3;
                ws.Cell(lastRow + 1, 3).Value = "TỔNG CỘNG:";
                ws.Cell(lastRow + 1, 4).Value = invoices.Sum(i => i.TotalAmount);
                ws.Cell(lastRow + 1, 5).Value = invoices.Sum(i => i.PaidAmount);
                ws.Cell(lastRow + 1, 6).Value = invoices.Sum(i => i.TotalAmount - i.PaidAmount);
                ws.Range(lastRow + 1, 3, lastRow + 1, 6).Style.Font.SetBold();

                // Tự động co giãn cột
                ws.Columns().AdjustToContents();

                // Tạo stream và trả về file (không dùng using để tránh dispose sớm)
                var stream = new MemoryStream();
                wb.SaveAs(stream);
                stream.Position = 0;
                string fileName = $"DoanhThu_{reportYear}_{reportMonth}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}