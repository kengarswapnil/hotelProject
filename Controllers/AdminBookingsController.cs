using HotelBooking.Web.Data;
using HotelBooking.Web.Models;
using HotelBooking.Web.Services;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Controllers;

[Authorize(Roles = DbSeeder.AdminRole)]
[Route("admin/bookings")]
public class AdminBookingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly BookingService _bookings;

    public AdminBookingsController(ApplicationDbContext db, BookingService bookings)
    {
        _db = db;
        _bookings = bookings;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, BookingStatus? status)
    {
        var query = _db.Bookings.AsNoTracking()
            .Include(b => b.Room!).ThenInclude(r => r.RoomType)
            .Include(b => b.User).AsQueryable();

        if (status.HasValue) query = query.Where(b => b.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(b => b.Reference.Contains(q) || b.User!.FullName.Contains(q)
                                  || b.User!.Email!.Contains(q) || b.Room!.RoomNumber.Contains(q));
        }
        return View(new BookingFilterVm { Q = q, Status = status, Bookings = await query.OrderByDescending(b => b.CheckIn).Take(200).ToListAsync() });
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, BookingStatus newStatus, string? q, BookingStatus? status)
    {
        var err = await _bookings.ChangeStatusAsync(id, newStatus);
        if (err == null) TempData["Success"] = $"Booking updated to {newStatus}.";
        else TempData["Error"] = err;
        return RedirectToAction(nameof(Index), new { q, status });
    }
}
