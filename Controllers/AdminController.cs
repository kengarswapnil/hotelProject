using HotelBooking.Web.Data;
using HotelBooking.Web.Models;
using HotelBooking.Web.Services;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Controllers;

[Authorize(Roles = DbSeeder.AdminRole)]
[Route("admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var today = AvailabilityService.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var active = new[] { BookingStatus.Confirmed, BookingStatus.CheckedIn };

        var vm = new DashboardVm
        {
            TotalRooms = await _db.Rooms.CountAsync(r => r.IsActive),
            OccupiedToday = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.CheckedIn
                                                              && b.CheckIn <= today && b.CheckOut > today),
            UpcomingBookings = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed && b.CheckIn >= today),
            MonthRevenue = await _db.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled && b.CheckIn >= monthStart && b.CheckIn < monthStart.AddMonths(1))
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0,
            Arrivals = await _db.Bookings.Include(b => b.Room).Include(b => b.User)
                .Where(b => b.Status == BookingStatus.Confirmed && b.CheckIn == today).ToListAsync(),
            Departures = await _db.Bookings.Include(b => b.Room).Include(b => b.User)
                .Where(b => b.Status == BookingStatus.CheckedIn && b.CheckOut == today).ToListAsync()
        };
        return View(vm);
    }
}
