using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HotelManagement.Models.Entities;
using HotelManagement.Data;

namespace HotelManagement.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Employee>>();
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

            // Tạo các role
            string[] roleNames = { "Admin", "Receptionist", "Housekeeping" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Tạo admin nếu chưa tồn tại
            var adminEmail = "admin@hotel.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new Employee
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator",
                    HireDate = DateTime.Now,
                    IsActive = true,
                    Address = ""
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
            else
            {
                // Đã tồn tại, kiểm tra role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Tạo loại phòng mẫu nếu chưa có
            if (!dbContext.RoomTypes.Any())
            {
                dbContext.RoomTypes.AddRange(
                    new RoomType { Name = "Standard", PricePerNight = 500000, Capacity = 2, Description = "Phòng tiêu chuẩn" },
                    new RoomType { Name = "Deluxe", PricePerNight = 800000, Capacity = 2, Description = "Phòng cao cấp" },
                    new RoomType { Name = "Suite", PricePerNight = 1500000, Capacity = 4, Description = "Phòng suite sang trọng" }
                );
                await dbContext.SaveChangesAsync();
            }
            // tao room
            if (!dbContext.Rooms.Any())
            {
                var roomTypes = dbContext.RoomTypes.ToList();
                dbContext.Rooms.AddRange(
                    new Room { RoomNumber = "101", RoomTypeId = roomTypes.First(r => r.Name == "Standard").Id, Status = "Available", Floor = "1", Description = "" },
                    new Room { RoomNumber = "102", RoomTypeId = roomTypes.First(r => r.Name == "Standard").Id, Status = "Available", Floor = "1", Description = "" },
                    new Room { RoomNumber = "201", RoomTypeId = roomTypes.First(r => r.Name == "Deluxe").Id, Status = "Available", Floor = "2", Description = "" }
                );
                await dbContext.SaveChangesAsync();
            }
        }
    }
}