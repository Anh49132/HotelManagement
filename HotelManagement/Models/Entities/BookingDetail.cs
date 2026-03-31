using HotelManagement.Models.Entities;

public class BookingDetail
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; }
    public decimal PriceAtTime { get; set; }
}