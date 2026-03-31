using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Data;
using HotelManagement.Models.Entities;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class InvoicesController : Controller
    {
        private readonly AppDbContext _context;

        public InvoicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Invoices
        public async Task<IActionResult> Index()
        {
            var invoices = _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                .OrderByDescending(i => i.InvoiceDate);
            return View(await invoices.ToListAsync());
        }

        // GET: Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(i => i.Booking)
                    .ThenInclude(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Room)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // GET: Invoices/Pay/5
        public async Task<IActionResult> Pay(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Booking)
                    .ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (invoice == null) return NotFound();

            ViewBag.PaymentMethods = new SelectList(new[] { "Cash", "Card", "BankTransfer" });
            return View(invoice);
        }

        // POST: Invoices/Pay/5
        [HttpPost, ActionName("Pay")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayConfirmed(int id, string paymentMethod, decimal paidAmount)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            decimal remaining = invoice.TotalAmount - invoice.PaidAmount;

            if (paidAmount <= 0)
            {
                TempData["Error"] = "Số tiền thanh toán phải lớn hơn 0.";
                return RedirectToAction(nameof(Pay), new { id });
            }

            if (paidAmount > remaining)
            {
                TempData["Error"] = $"Số tiền thanh toán không được vượt quá số tiền còn lại ({remaining.ToString("N0")} VNĐ).";
                return RedirectToAction(nameof(Pay), new { id });
            }

            invoice.PaidAmount += paidAmount;
            invoice.PaymentMethod = paymentMethod;

            if (invoice.PaidAmount >= invoice.TotalAmount)
                invoice.Status = "Paid";
            else
                invoice.Status = "Partial";

            _context.Update(invoice);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Thanh toán thành công {paidAmount.ToString("N0")} VNĐ.";
            return RedirectToAction(nameof(Index));
        }
    }
}