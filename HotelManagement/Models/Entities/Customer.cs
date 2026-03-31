
namespace HotelManagement.Models.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string IdentityNumber { get; set; } // CMND/CCCD
        public string Address { get; set; }
        public ICollection<Booking> Bookings { get; set; }
    }
}
