namespace HotelManagement.Models.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; }
        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; }
        public string Status { get; set; } // "Available", "Occupied", "Maintenance"
        public string Floor { get; set; }
        public string Description { get; set; } // Mô tả phòng (tùy chọn)
    }
}