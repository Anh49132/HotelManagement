// Models/Entities/Employee.cs
using Microsoft.AspNetCore.Identity;
namespace HotelManagement.Models.Entities
{
    public class Employee : IdentityUser
    {
        public string FullName { get; set; }
        public string Address { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; }
    }
}