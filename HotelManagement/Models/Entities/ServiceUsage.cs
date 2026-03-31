using System;

namespace HotelManagement.Models.Entities
{
    public class ServiceUsage
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public int? BookingId { get; set; }               // booking hiện tại (khi chưa có hóa đơn)
        public Booking? Booking { get; set; }
        public int? InvoiceId { get; set; }               // hóa đơn sau khi check-out
        public Invoice? Invoice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime UsageDate { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
    }
}