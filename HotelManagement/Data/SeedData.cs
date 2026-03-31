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
            // Tạo dịch vụ mẫu nếu chưa có
            if (!dbContext.Services.Any())
            {
                dbContext.Services.AddRange(
                    new Service { Name = "Ăn sáng", Unit = "Suất", Price = 50000, Description = "Bữa sáng buffet" },
                    new Service { Name = "Massage", Unit = "Giờ", Price = 200000, Description = "Massage thư giãn" },
                    new Service { Name = "Giặt ủi", Unit = "Kg", Price = 30000, Description = "Giặt và ủi quần áo" },
                    new Service { Name = "Phí hỏng nội thất", Unit = "Lần", Price = 100000, Description = "Phí bồi thường thiệt hại" }
                );
                await dbContext.SaveChangesAsync();
            }
            // ========== THÊM DỮ LIỆU MẪU ==========

            // Tạo khách hàng mẫu
            if (!dbContext.Customers.Any())
            {
                dbContext.Customers.AddRange(
                    new Customer { FullName = "Nguyễn Văn A", Phone = "0901234567", Email = "nguyenvana@email.com", IdentityNumber = "123456789", Address = "Hà Nội" },
                    new Customer { FullName = "Trần Thị B", Phone = "0987654321", Email = "tranthib@email.com", IdentityNumber = "987654321", Address = "TP.HCM" },
                    new Customer { FullName = "Lê Văn C", Phone = "0912345678", Email = "levanc@email.com", IdentityNumber = "456789123", Address = "Đà Nẵng" },
                    new Customer { FullName = "Phạm Thị D", Phone = "0934567890", Email = "phamthid@email.com", IdentityNumber = "789123456", Address = "Cần Thơ" }
                );
                await dbContext.SaveChangesAsync();
            }

            // Tạo thêm phòng nếu thiếu (giả sử đã có loại phòng từ trước)
            if (dbContext.Rooms.Count() < 5)
            {
                var roomTypes = dbContext.RoomTypes.ToList();
                if (roomTypes.Any())
                {
                    var standard = roomTypes.FirstOrDefault(r => r.Name == "Standard");
                    var deluxe = roomTypes.FirstOrDefault(r => r.Name == "Deluxe");
                    var suite = roomTypes.FirstOrDefault(r => r.Name == "Suite");
                    dbContext.Rooms.AddRange(
                        new Room { RoomNumber = "103", RoomTypeId = standard?.Id ?? 1, Status = "Available", Floor = "1", Description = "Phòng 103" },
                        new Room { RoomNumber = "104", RoomTypeId = standard?.Id ?? 1, Status = "Available", Floor = "1", Description = "Phòng 104" },
                        new Room { RoomNumber = "202", RoomTypeId = deluxe?.Id ?? 2, Status = "Available", Floor = "2", Description = "Phòng 202" },
                        new Room { RoomNumber = "301", RoomTypeId = suite?.Id ?? 3, Status = "Available", Floor = "3", Description = "Phòng 301" }
                    );
                    await dbContext.SaveChangesAsync();
                }
            }

            // Tạo booking mẫu (đã xác nhận, chưa check-in)
            if (!dbContext.Bookings.Any())
            {
                var customers = dbContext.Customers.ToList();
                var rooms = dbContext.Rooms.ToList();
                if (customers.Any() && rooms.Any())
                {
                    // Booking 1: khách A, phòng 101, 3 ngày từ hôm nay
                    var booking1 = new Booking
                    {
                        CustomerId = customers[0].Id,
                        CheckInDate = DateTime.Now.Date,
                        CheckOutDate = DateTime.Now.Date.AddDays(3),
                        NumberOfAdults = 2,
                        NumberOfChildren = 0,
                        Status = "Confirmed",
                        TotalPrice = 0
                    };
                    dbContext.Bookings.Add(booking1);
                    await dbContext.SaveChangesAsync();

                    // Thêm chi tiết phòng cho booking1
                    var room101 = rooms.FirstOrDefault(r => r.RoomNumber == "101");
                    if (room101 != null)
                    {
                        var price = room101.RoomType?.PricePerNight ?? 500000;
                        dbContext.BookingDetails.Add(new BookingDetail
                        {
                            BookingId = booking1.Id,
                            RoomId = room101.Id,
                            PriceAtTime = price
                        });
                        booking1.TotalPrice = price * 3;
                    }

                    // Booking 2: khách B, phòng 102, 2 ngày bắt đầu từ ngày mai
                    var booking2 = new Booking
                    {
                        CustomerId = customers[1].Id,
                        CheckInDate = DateTime.Now.Date.AddDays(1),
                        CheckOutDate = DateTime.Now.Date.AddDays(3),
                        NumberOfAdults = 1,
                        NumberOfChildren = 1,
                        Status = "Confirmed",
                        TotalPrice = 0
                    };
                    dbContext.Bookings.Add(booking2);
                    await dbContext.SaveChangesAsync();

                    var room102 = rooms.FirstOrDefault(r => r.RoomNumber == "102");
                    if (room102 != null)
                    {
                        var price = room102.RoomType?.PricePerNight ?? 500000;
                        dbContext.BookingDetails.Add(new BookingDetail
                        {
                            BookingId = booking2.Id,
                            RoomId = room102.Id,
                            PriceAtTime = price
                        });
                        booking2.TotalPrice = price * 2;
                    }

                    await dbContext.SaveChangesAsync();
                }
            }

            // Tạo booking đã check-in (để có thể check-out)
            if (!dbContext.Bookings.Any(b => b.Status == "CheckedIn"))
            {
                var customers = dbContext.Customers.ToList();
                var rooms = dbContext.Rooms.ToList();
                if (customers.Any() && rooms.Any())
                {
                    // Booking 3: khách C, phòng 201, đã check-in hôm qua
                    var booking3 = new Booking
                    {
                        CustomerId = customers[2].Id,
                        CheckInDate = DateTime.Now.Date.AddDays(-1),
                        CheckOutDate = DateTime.Now.Date.AddDays(2),
                        NumberOfAdults = 2,
                        NumberOfChildren = 0,
                        Status = "CheckedIn",
                        TotalPrice = 0
                    };
                    dbContext.Bookings.Add(booking3);
                    await dbContext.SaveChangesAsync();

                    var room201 = rooms.FirstOrDefault(r => r.RoomNumber == "201");
                    if (room201 != null)
                    {
                        var price = room201.RoomType?.PricePerNight ?? 800000;
                        dbContext.BookingDetails.Add(new BookingDetail
                        {
                            BookingId = booking3.Id,
                            RoomId = room201.Id,
                            PriceAtTime = price
                        });
                        booking3.TotalPrice = price * 3;
                        // Cập nhật trạng thái phòng
                        room201.Status = "Occupied";
                        dbContext.Update(room201);
                    }
                    await dbContext.SaveChangesAsync();
                }
            }

            // Tạo hóa đơn mẫu (cho booking đã check-out)
            if (!dbContext.Invoices.Any())
            {
                var customers = dbContext.Customers.ToList();
                var rooms = dbContext.Rooms.ToList();
                if (customers.Any() && rooms.Any())
                {
                    // Tạo một booking đã check-out
                    var booking4 = new Booking
                    {
                        CustomerId = customers[3].Id,
                        CheckInDate = DateTime.Now.Date.AddDays(-5),
                        CheckOutDate = DateTime.Now.Date.AddDays(-2),
                        NumberOfAdults = 1,
                        NumberOfChildren = 0,
                        Status = "CheckedOut",
                        TotalPrice = 0
                    };
                    dbContext.Bookings.Add(booking4);
                    await dbContext.SaveChangesAsync();

                    var room103 = rooms.FirstOrDefault(r => r.RoomNumber == "103");
                    if (room103 != null)
                    {
                        var price = room103.RoomType?.PricePerNight ?? 500000;
                        dbContext.BookingDetails.Add(new BookingDetail
                        {
                            BookingId = booking4.Id,
                            RoomId = room103.Id,
                            PriceAtTime = price
                        });
                        booking4.TotalPrice = price * 3;
                        await dbContext.SaveChangesAsync();

                        // Tạo hóa đơn
                        var invoice = new Invoice
                        {
                            BookingId = booking4.Id,
                            InvoiceDate = DateTime.Now.Date.AddDays(-2),
                            RoomCharge = price * 3,
                            ServiceCharge = 0,
                            TotalAmount = price * 3,
                            PaidAmount = price * 3,
                            Status = "Paid",
                            PaymentMethod = "Cash"
                        };
                        dbContext.Invoices.Add(invoice);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
        }
    }
}