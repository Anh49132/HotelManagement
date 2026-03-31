using HotelManagement.Models.Entities;

namespace HotelManagement.Models.ViewModels
{
    public class CheckOutViewModel
    {
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public decimal RoomCharge { get; set; }

        public decimal TotalAmount { get; set; }
        public Invoice Invoice { get; set; }
    }
}