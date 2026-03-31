using System.Collections.Generic;

namespace HotelManagement.Models.Entities
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;      // tên dịch vụ: "Ăn sáng", "Massage", "Giặt ủi",...
        public string Unit { get; set; } = string.Empty;      // đơn vị: "suất", "giờ", "kg"
        public decimal Price { get; set; }                    // giá
        public string Description { get; set; } = string.Empty;
        public ICollection<ServiceUsage> ServiceUsages { get; set; } = new List<ServiceUsage>();
    }
}