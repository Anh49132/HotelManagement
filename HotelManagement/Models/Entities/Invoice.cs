using System;
using System.Collections.Generic;

namespace HotelManagement.Models.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public decimal RoomCharge { get; set; }
        public decimal ServiceCharge { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = "Unpaid"; // Unpaid, Partial, Paid
        public ICollection<ServiceUsage> ServiceUsages { get; set; } = new List<ServiceUsage>();
    }
}