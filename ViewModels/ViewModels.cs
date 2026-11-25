using System.ComponentModel.DataAnnotations;
using HotelBooking.Web.Models;
using HotelBooking.Web.Services;

namespace HotelBooking.Web.ViewModels;

public class SearchViewModel
{
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Guests { get; set; } = 2;
    public int? RoomTypeId { get; set; }
    public List<RoomType> AllTypes { get; set; } = new();
    public List<RoomTypeAvailability>? Results { get; set; }
    public string? Error { get; set; }
}

public class BookingCreateVm
{
    public int RoomTypeId { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    [Range(1, 10)]
    public int Guests { get; set; } = 1;

    [StringLength(500), Display(Name = "Special requests (optional)")]
    public string? Notes { get; set; }

    // Display only. Prices are always recomputed on the server.
    public RoomType? RoomType { get; set; }
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;
    public decimal Total => (RoomType?.BasePrice ?? 0) * Nights;
}

public class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    [Display(Name = "Remember me")] public bool RememberMe { get; set; }
}

public class RegisterVm
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress] public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
}

public class DashboardVm
{
    public int TotalRooms { get; set; }
    public int OccupiedToday { get; set; }
    public int UpcomingBookings { get; set; }
    public decimal MonthRevenue { get; set; }
    public List<Booking> Arrivals { get; set; } = new();
    public List<Booking> Departures { get; set; } = new();
    public int OccupancyPercent => TotalRooms == 0 ? 0 : OccupiedToday * 100 / TotalRooms;
}

public class BookingFilterVm
{
    public string? Q { get; set; }
    public BookingStatus? Status { get; set; }
    public List<Booking> Bookings { get; set; } = new();
}
