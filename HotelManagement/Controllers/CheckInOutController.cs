using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManagement.Data;
using HotelManagement.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models.Entities;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class CheckInOutController : Controller
    {
        private readonly AppDbContext _context;

        public CheckInOutController(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách booking chờ check-in (Status = "Confirmed")
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                .Where(b => b.Status == "Confirmed")
                .OrderBy(b => b.CheckInDate)
                .ToListAsync();
            return View(bookings);
        }

        // GET: Xác nhận check-in
        public async Task<IActionResult> CheckIn(int? id)
        {
            if (id == null) return NotFound();
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();
            if (booking.Status != "Confirmed")
            {
                TempData["Error"] = "Booking này không thể check-in.";
                return RedirectToAction(nameof(Index));
            }
            return View(booking);
        }

        // POST: Xác nhận check-in
        [HttpPost, ActionName("CheckIn")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckInConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            if (booking.Status != "Confirmed")
            {
                TempData["Error"] = "Booking này không thể check-in.";
                return RedirectToAction(nameof(Index));
            }

            // Cập nhật trạng thái booking
            booking.Status = "CheckedIn";

            // Cập nhật trạng thái phòng thành Occupied
            foreach (var detail in booking.BookingDetails)
            {
                var room = await _context.Rooms.FindAsync(detail.RoomId);
                if (room != null)
                {
                    room.Status = "Occupied";
                    _context.Update(room);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Check-in thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Danh sách booking đang check-in (Status = "CheckedIn")
        public async Task<IActionResult> CheckOutList()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                .Where(b => b.Status == "CheckedIn")
                .OrderBy(b => b.CheckOutDate)
                .ToListAsync();
            return View(bookings);
        }

        // GET: Xác nhận check-out
        public async Task<IActionResult> CheckOut(int? id)
        {
            if (id == null) return NotFound();
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();
            if (booking.Status != "CheckedIn")
            {
                TempData["Error"] = "Booking này không thể check-out.";
                return RedirectToAction(nameof(CheckOutList));
            }
            return View(booking);
        }

        // POST: Xác nhận check-out
        [HttpPost, ActionName("CheckOut")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOutConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            if (booking.Status != "CheckedIn")
            {
                TempData["Error"] = "Booking này không thể check-out.";
                return RedirectToAction(nameof(CheckOutList));
            }

            // Tính tiền phòng thực tế
            int actualNights = (DateTime.Now.Date - booking.CheckInDate).Days;
            if (actualNights <= 0) actualNights = 1;

            decimal roomCharge = booking.BookingDetails.Sum(bd => bd.PriceAtTime) * actualNights;

            // Lấy các service usage chưa có invoice của booking này
            var serviceUsages = await _context.ServiceUsages
                .Where(su => su.BookingId == booking.Id && su.InvoiceId == null)
                .Include(su => su.Service)
                .ToListAsync();

            decimal serviceCharge = serviceUsages.Sum(su => su.TotalPrice);

            // Tạo hóa đơn
            var invoice = new Invoice
            {
                BookingId = booking.Id,
                InvoiceDate = DateTime.Now,
                RoomCharge = roomCharge,
                ServiceCharge = serviceCharge,
                TotalAmount = roomCharge + serviceCharge,
                PaidAmount = 0,
                Status = "Unpaid",
                PaymentMethod = "Cash"
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Gán InvoiceId cho các service usage
            foreach (var usage in serviceUsages)
            {
                usage.InvoiceId = invoice.Id;
                _context.Update(usage);
            }
            await _context.SaveChangesAsync();

            // Cập nhật trạng thái booking
            booking.Status = "CheckedOut";

            // Cập nhật phòng thành Available
            foreach (var detail in booking.BookingDetails)
            {
                var room = detail.Room;
                if (room != null)
                {
                    room.Status = "Available";
                    _context.Update(room);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Check-out thành công! Hóa đơn đã được tạo.";
            return RedirectToAction(nameof(CheckOutList));
        }

        // ================= THÊM DỊCH VỤ =================

        // GET: Hiển thị form thêm dịch vụ
        public async Task<IActionResult> AddService(int? bookingId)
        {
            if (bookingId == null) return NotFound();
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null || booking.Status != "CheckedIn")
            {
                TempData["Error"] = "Chỉ có thể thêm dịch vụ cho booking đang ở.";
                return RedirectToAction(nameof(CheckOutList));
            }
            ViewBag.BookingId = bookingId;
            ViewBag.Services = new SelectList(_context.Services, "Id", "Name");
            return View();
        }

        // POST: Lưu dịch vụ đã chọn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(int bookingId, int serviceId, int quantity, string notes)
        {
            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0.";
                return RedirectToAction(nameof(AddService), new { bookingId });
            }

            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return NotFound();

            var usage = new ServiceUsage
            {
                ServiceId = serviceId,
                BookingId = bookingId,
                Quantity = quantity,
                TotalPrice = service.Price * quantity,
                UsageDate = DateTime.Now,
                Notes = notes
            };
            _context.ServiceUsages.Add(usage);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã thêm {quantity} {service.Name} vào booking.";
            return RedirectToAction(nameof(CheckOutList));
        }
    }
}