using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelManagement.Data;
using HotelManagement.Models.Entities;
using HotelManagement.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Controllers
{
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
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }
        //***
       

        //****
        private List<Room> GetAvailableRooms(DateTime checkIn, DateTime checkOut, int capacity)
        {
            var allRooms = _context.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.Status == "Available")
                .ToList();

            var bookedRoomIds = _context.BookingDetails
                .Include(bd => bd.Booking)
                .Where(bd => bd.Booking.Status != "Cancelled" &&
                             bd.Booking.CheckInDate < checkOut &&
                             bd.Booking.CheckOutDate > checkIn)
                .Select(bd => bd.RoomId)
                .ToList();

            return allRooms
                .Where(r => !bookedRoomIds.Contains(r.Id) && r.RoomType.Capacity >= capacity)
                .ToList();
        }

        // GET: Bookings/Create
        public IActionResult Create()
        {
            var model = new CreateBookingViewModel
            {
                CheckInDate = DateTime.Now.Date,
                CheckOutDate = DateTime.Now.Date.AddDays(1),
                NumberOfAdults = 1,
                AvailableRooms = GetAvailableRooms(DateTime.Now.Date, DateTime.Now.Date.AddDays(1), 1)
            };
            ViewBag.Customers = new SelectList(_context.Customers, "Id", "FullName");
            return View(model);
        }
        //****
        private bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut)
        {
            return !_context.BookingDetails
                .Include(bd => bd.Booking)
                .Any(bd => bd.RoomId == roomId &&
                           bd.Booking.Status != "Cancelled" &&
                           bd.Booking.CheckInDate < checkOut &&
                           bd.Booking.CheckOutDate > checkIn);
        }
       


        // POST: Bookings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
                // Kiểm tra lại phòng trống
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
                    model.AvailableRooms = GetAvailableRooms(model.CheckInDate, model.CheckOutDate, model.NumberOfAdults);
                    return View(model);
                }

                // Tạo booking
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

                // Thêm chi tiết phòng
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
            model.AvailableRooms = GetAvailableRooms(model.CheckInDate, model.CheckOutDate, model.NumberOfAdults);
            return View(model);
        }
        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Id", booking.CustomerId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,CheckInDate,CheckOutDate,NumberOfAdults,NumberOfChildren,TotalPrice,Status")] Booking booking)
        {
            if (id != booking.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Id", booking.CustomerId);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}
