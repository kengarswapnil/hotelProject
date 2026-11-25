using HotelBooking.Web.Data;
using HotelBooking.Web.Models;
using HotelBooking.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Controllers;

[Authorize(Roles = DbSeeder.AdminRole)]
[Route("admin/rooms")]
public class AdminRoomsController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminRoomsController(ApplicationDbContext db) => _db = db;

    private async Task LoadTypes(int? selected = null) =>
        ViewBag.Types = new SelectList(await _db.RoomTypes.OrderBy(t => t.Name).ToListAsync(), "Id", "Name", selected);

    [HttpGet("")]
    public async Task<IActionResult> Index() =>
        View(await _db.Rooms.AsNoTracking().Include(r => r.RoomType).OrderBy(r => r.RoomNumber).ToListAsync());

    [HttpGet("create")]
    public async Task<IActionResult> Create() { await LoadTypes(); return View("Form", new Room()); }

    [HttpPost("create")]
    public async Task<IActionResult> Create([Bind("RoomNumber,Floor,RoomTypeId,IsActive")] Room room)
    {
        room.RoomNumber = (room.RoomNumber ?? "").Trim();
        if (await _db.Rooms.AnyAsync(r => r.RoomNumber == room.RoomNumber))
            ModelState.AddModelError(nameof(Room.RoomNumber), "A room with this number already exists.");
        if (!await _db.RoomTypes.AnyAsync(t => t.Id == room.RoomTypeId))
            ModelState.AddModelError(nameof(Room.RoomTypeId), "Select a valid room type.");
        if (!ModelState.IsValid) { await LoadTypes(room.RoomTypeId); return View("Form", room); }

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Room {room.RoomNumber} added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();
        await LoadTypes(room.RoomTypeId);
        return View("Form", room);
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, [Bind("RoomNumber,Floor,RoomTypeId,IsActive")] Room input)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();

        input.RoomNumber = (input.RoomNumber ?? "").Trim();
        if (await _db.Rooms.AnyAsync(r => r.RoomNumber == input.RoomNumber && r.Id != id))
            ModelState.AddModelError(nameof(Room.RoomNumber), "A room with this number already exists.");
        if (!input.IsActive && room.IsActive && await HasFutureBookings(id))
            ModelState.AddModelError(nameof(Room.IsActive), "This room has upcoming bookings. Cancel or move them before deactivating.");
        if (!ModelState.IsValid) { input.Id = id; await LoadTypes(input.RoomTypeId); return View("Form", input); }

        room.RoomNumber = input.RoomNumber;
        room.Floor = input.Floor;
        room.RoomTypeId = input.RoomTypeId;
        room.IsActive = input.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Room {room.RoomNumber} updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null) return NotFound();
        // Rooms with any booking history are kept (deactivate instead) so booking records stay intact.
        if (await _db.Bookings.AnyAsync(b => b.RoomId == id))
            TempData["Error"] = $"Room {room.RoomNumber} has booking history and cannot be deleted. Deactivate it instead.";
        else
        {
            _db.Rooms.Remove(room);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Room {room.RoomNumber} deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    private Task<bool> HasFutureBookings(int roomId)
    {
        var today = AvailabilityService.Today;
        return _db.Bookings.AnyAsync(b => b.RoomId == roomId && b.CheckOut > today
            && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn));
    }
}
