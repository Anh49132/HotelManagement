using HotelManagement.Models.Entities;

namespace HotelManagement.Models.ViewModels
{
    public class CheckInViewModel
    {
        public int BookingId { get; set; }
        public Booking Booking { get; set; }

    }
}