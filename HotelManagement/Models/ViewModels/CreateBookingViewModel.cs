using HotelManagement.Models.Entities;

namespace HotelManagement.Models.ViewModels
{
    public class CreateBookingViewModel
    {
        public int CustomerId { get; set; }
        public DateTime CheckInDate { get; set; } = DateTime.Now.Date;
        public DateTime CheckOutDate { get; set; } = DateTime.Now.Date.AddDays(1);
        public int NumberOfAdults { get; set; } = 1;
        public int NumberOfChildren { get; set; } = 0;
        public List<int> SelectedRoomIds { get; set; } = new List<int>();
        public List<Room> AvailableRooms { get; set; }
        public decimal TotalPrice { get; set; }
    }
}