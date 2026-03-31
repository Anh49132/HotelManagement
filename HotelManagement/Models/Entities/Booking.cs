using System;
using System.Collections.Generic;

namespace HotelManagement.Models.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfAdults { get; set; }
        public int NumberOfChildren { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public ICollection<BookingDetail> BookingDetails { get; set; }
    }
}