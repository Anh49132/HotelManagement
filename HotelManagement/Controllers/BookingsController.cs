using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManagement.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class BookingsController : Controller
    {
        private readonly AppDbContext _context;

        public BookingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Bookings.Include(b => b.Customer);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null) return NotFound();

            return View(booking);
        }
        // ===================== HÀM HỖ TRỢ =====================

        // Private: lấy danh sách phòng trống (đổi tên để không trùng với action)
        private List<Room> GetAvailableRoomsList(DateTime checkIn, DateTime checkOut, int capacity)
        {
            var allRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "Available")
                .ToList();

            var bookedRoomIds = _context.BookingDetails
                .Include(bd => bd.Booking)
                .Where(bd => bd.Booking.Status != "Cancelled" &&
                             bd.Booking.Status != "CheckedOut" &&
                             bd.Booking.CheckInDate < checkOut &&
                             bd.Booking.CheckOutDate > checkIn)
                .Select(bd => bd.RoomId)
                .ToList();

            return allRooms
                .Where(r => !bookedRoomIds.Contains(r.Id) && r.RoomType.Capacity >= capacity)
                .ToList();
        }

        // Private: kiểm tra phòng có trống không
        private bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut)
        {
            return !_context.BookingDetails
                .Include(bd => bd.Booking)
                .Any(bd => bd.RoomId == roomId &&
                           bd.Booking.Status != "Cancelled" &&
                           bd.Booking.Status != "CheckedOut" &&
                           bd.Booking.CheckInDate < checkOut &&
                           bd.Booking.CheckOutDate > checkIn);
        }

        // ===================== AJAX =====================
        [HttpGet]
        public IActionResult GetAvailableRooms(DateTime checkIn, DateTime checkOut, int capacity)
        {
            var rooms = GetAvailableRoomsList(checkIn, checkOut, capacity);
            return PartialView("_RoomCheckboxes", rooms);
        }

        // ===================== CREATE =====================
        // GET: Bookings/Create
        public IActionResult Create()
        {
            var model = new CreateBookingViewModel
            {
                CheckInDate = DateTime.Now.Date,
                CheckOutDate = DateTime.Now.Date.AddDays(1),
                NumberOfAdults = 1,
                AvailableRooms = GetAvailableRoomsList(DateTime.Now.Date, DateTime.Now.Date.AddDays(1), 1)
            };
            ViewBag.Customers = new SelectList(_context.Customers, "Id", "FullName");
            return View(model);
        }
        // POST: Bookings/Cre 
        [HttpPost]
       [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            ModelState.Remove("AvailableRooms");
            ModelState.Remove("TotalPrice");

            if (model.SelectedRoomIds == null || !model.SelectedRoomIds.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một phòng.");
            }

            if (ModelState.IsValid)
          {

                bool allAvailable = true;
                foreach (var roomId in model.SelectedRoomIds)
                {
                    if (!IsRoomAvailable(roomId, model.CheckInDate, model.CheckOutDate))
                    {
                        allAvailable = false;
                        break;
                    }
                }

                if (!allAvailable)
                {
                    ModelState.AddModelError("", "Một số phòng đã được đặt trong khoảng thời gian này.");
                    ViewBag.Customers = new SelectList(_context.Customers, "Id", "FullName", model.CustomerId);
                    model.AvailableRooms = GetAvailableRoomsList(model.CheckInDate, model.CheckOutDate, model.NumberOfAdults);
                    return View(model);
                }

                
                var booking = new Booking
                {
                    CustomerId = model.CustomerId,
                    CheckInDate = model.CheckInDate,
                    CheckOutDate = model.CheckOutDate,
                    NumberOfAdults = model.NumberOfAdults,
                    NumberOfChildren = model.NumberOfChildren,
                    Status = "Confirmed",
                    TotalPrice = 0
                };
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                
                int nights = (model.CheckOutDate - model.CheckInDate).Days;
                decimal totalPrice = 0;
                foreach (var roomId in model.SelectedRoomIds)
                {
                    var room = await _context.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == roomId);
                    var detail = new BookingDetail
                    {
                        BookingId = booking.Id,
                        RoomId = roomId,
                        PriceAtTime = room.RoomType.PricePerNight
                    };
                    _context.BookingDetails.Add(detail);
                    totalPrice += room.RoomType.PricePerNight * nights;
                }
                booking.TotalPrice = totalPrice;
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Customers = new SelectList(_context.Customers, "Id", "FullName", model.CustomerId);
            model.AvailableRooms = GetAvailableRoomsList(model.CheckInDate, model.CheckOutDate, model.NumberOfAdults);
            return View(model);
        }

        // ===================== EDIT =====================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
            return View(booking);
       }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,CheckInDate,CheckOutDate,NumberOfAdults,NumberOfChildren,TotalPrice,Status")] Booking booking)
        {
            if (id != booking.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "FullName", booking.CustomerId);
            return View(booking);
        }

        // ===================== DELETE =====================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null) return NotFound();

            return View(booking);
        }



        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null) _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    
    
    }

}