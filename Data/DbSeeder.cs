using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Data;

public static class DbSeeder
{
    public const string AdminRole = "Admin";
    public const string GuestRole = "Guest";

    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();

        // Creates the schema (tables, FKs, indexes, check constraints) on first run.
        await db.Database.EnsureCreatedAsync();

        foreach (var r in new[] { AdminRole, GuestRole })
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        await EnsureUser(users, "admin@hotel.com", "Hotel Manager", "Admin@123", AdminRole);
        var guest = await EnsureUser(users, "guest@hotel.com", "Demo Guest", "Guest@123", GuestRole);

        if (await db.RoomTypes.AnyAsync()) return;

        var types = new[]
        {
            new RoomType { Name = "Standard Room", BasePrice = 2500, Capacity = 2,
                Description = "Comfortable room with a queen bed, work desk and city views.",
                Amenities = "Wi-Fi, AC, TV, Breakfast" },
            new RoomType { Name = "Deluxe Room", BasePrice = 4000, Capacity = 3,
                Description = "Spacious room with a king bed, sitting area and a private balcony.",
                Amenities = "Wi-Fi, AC, Smart TV, Mini bar, Balcony" },
            new RoomType { Name = "Family Suite", BasePrice = 6500, Capacity = 5,
                Description = "Two-bedroom suite designed for families, with a living room and kitchenette.",
                Amenities = "Wi-Fi, AC, 2 bedrooms, Kitchenette, Breakfast" },
            new RoomType { Name = "Executive Suite", BasePrice = 9000, Capacity = 2,
                Description = "Top-floor suite with a lounge, jacuzzi and complimentary airport pickup.",
                Amenities = "Wi-Fi, AC, Jacuzzi, Lounge, Airport pickup" },
        };
        db.RoomTypes.AddRange(types);
        await db.SaveChangesAsync();

        var rooms = new List<Room>();
        void Add(RoomType t, int floor, params string[] nums) =>
            rooms.AddRange(nums.Select(n => new Room { RoomNumber = n, Floor = floor, RoomTypeId = t.Id }));
        Add(types[0], 1, "101", "102", "103", "104", "105");
        Add(types[1], 2, "201", "202", "203", "204");
        Add(types[2], 3, "301", "302", "303");
        Add(types[3], 4, "401", "402");
        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        Booking Make(string no, int offset, int nights, int guests, BookingStatus s)
        {
            var room = rooms.First(r => r.RoomNumber == no);
            var type = types.First(t => t.Id == room.RoomTypeId);
            var ci = today.AddDays(offset);
            return new Booking
            {
                Reference = "HB" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                UserId = guest.Id, RoomId = room.Id, CheckIn = ci, CheckOut = ci.AddDays(nights),
                Guests = guests, PricePerNight = type.BasePrice, TotalPrice = type.BasePrice * nights, Status = s
            };
        }
        db.Bookings.AddRange(
            Make("101", 0, 2, 2, BookingStatus.Confirmed),
            Make("201", -1, 2, 2, BookingStatus.CheckedIn),
            Make("301", 3, 4, 4, BookingStatus.Confirmed),
            Make("102", -10, 3, 1, BookingStatus.Completed),
            Make("401", 7, 2, 2, BookingStatus.Cancelled));
        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUser(UserManager<ApplicationUser> um, string email, string name, string pwd, string role)
    {
        var u = await um.FindByEmailAsync(email);
        if (u != null) return u;
        u = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
        var res = await um.CreateAsync(u, pwd);
        if (!res.Succeeded) throw new InvalidOperationException(string.Join("; ", res.Errors.Select(e => e.Description)));
        await um.AddToRoleAsync(u, role);
        return u;
    }
}
