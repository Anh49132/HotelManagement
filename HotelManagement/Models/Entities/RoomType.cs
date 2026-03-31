
namespace HotelManagement.Models.Entities
{
    public class RoomType
    {
        public int Id { get; set; }
        public string Name { get; set; }           // "Standard", "Deluxe", "Suite"
        public string Description { get; set; }
        public decimal PricePerNight { get; set; } // Giá mỗi đêm
        public int Capacity { get; set; }          // Số người tối đa
        public ICollection<Room> Rooms { get; set; }
    }
}