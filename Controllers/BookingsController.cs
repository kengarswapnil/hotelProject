using HotelBooking.Web.Data;
using HotelBooking.Web.Models;
using HotelBooking.Web.Services;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly BookingService _bookings;
    private readonly AvailabilityService _availability;

    public BookingsController(ApplicationDbContext db, UserManager<ApplicationUser> users,
        BookingService bookings, AvailabilityService availability)
    {
        _db = db;
        _users = users;
        _bookings = bookings;
        _availability = availability;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int roomTypeId, DateOnly checkIn, DateOnly checkOut, int guests = 1)
    {
        var type = await _db.RoomTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == roomTypeId);
        if (type == null) return NotFound();

        var err = AvailabilityService.ValidateStay(checkIn, checkOut);
        if (err == null && !await _availability.FreeRooms(checkIn, checkOut, roomTypeId).AnyAsync())
            err = "Sorry, this room type is no longer available for those dates.";
        if (err != null)
        {
            TempData["Error"] = err;
            return RedirectToAction("Search", "Rooms");
        }

        return View(new BookingCreateVm
        {
            RoomTypeId = roomTypeId, CheckIn = checkIn, CheckOut = checkOut,
            Guests = Math.Clamp(guests, 1, type.Capacity), RoomType = type
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookingCreateVm vm)
    {
        vm.RoomType = await _db.RoomTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == vm.RoomTypeId);
        if (vm.RoomType == null) return NotFound();
        if (!ModelState.IsValid) return View(vm);

        var result = await _bookings.CreateAsync(_users.GetUserId(User)!, vm.RoomTypeId, vm.CheckIn, vm.CheckOut, vm.Guests, vm.Notes);
        if (!result.Ok)
        {
            ModelState.AddModelError("", result.Error!);
            return View(vm);
        }
        TempData["Success"] = "Your booking is confirmed!";
        return RedirectToAction(nameof(Details), new { id = result.Booking!.Id });
    }

    public async Task<IActionResult> Mine()
    {
        var uid = _users.GetUserId(User);
        var list = await _db.Bookings.AsNoTracking()
            .Include(b => b.Room!).ThenInclude(r => r.RoomType)
            .Where(b => b.UserId == uid)
            .OrderByDescending(b => b.CheckIn).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var b = await _db.Bookings.AsNoTracking()
            .Include(x => x.Room!).ThenInclude(r => r.RoomType)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        // Guests may only view their own bookings; staff may view any.
        if (b == null || (b.UserId != _users.GetUserId(User) && !User.IsInRole(DbSeeder.AdminRole)))
            return NotFound();
        return View(b);
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var err = await _bookings.CancelByGuestAsync(id, _users.GetUserId(User)!);
        if (err == null) TempData["Success"] = "Booking cancelled.";
        else TempData["Error"] = err;
        return RedirectToAction(nameof(Mine));
    }
}
