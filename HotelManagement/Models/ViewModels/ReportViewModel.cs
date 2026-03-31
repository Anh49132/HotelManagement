using System;

namespace HotelManagement.Models.ViewModels
{
    public class RevenueReportViewModel
    {
        public DateTime Date { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public decimal AverageRevenuePerBooking { get; set; }
    }

    public class OccupancyReportViewModel
    {
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }
        public int TotalDaysOccupied { get; set; }
        public decimal OccupancyRate { get; set; }
    }
}