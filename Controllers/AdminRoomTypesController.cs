using HotelBooking.Web.Data;
using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Controllers;

[Authorize(Roles = DbSeeder.AdminRole)]
[Route("admin/room-types")]
public class AdminRoomTypesController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminRoomTypesController(ApplicationDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index() =>
        View(await _db.RoomTypes.AsNoTracking().Include(t => t.Rooms).OrderBy(t => t.BasePrice).ToListAsync());

    [HttpGet("create")]
    public IActionResult Create() => View("Form", new RoomType());

    [HttpPost("create")]
    public async Task<IActionResult> Create([Bind("Name,Description,BasePrice,Capacity,Amenities")] RoomType type)
    {
        type.Name = type.Name?.Trim() ?? "";
        if (await _db.RoomTypes.AnyAsync(t => t.Name == type.Name))
            ModelState.AddModelError(nameof(RoomType.Name), "A room type with this name already exists.");
        if (!ModelState.IsValid) return View("Form", type);

        _db.RoomTypes.Add(type);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"{type.Name} created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var t = await _db.RoomTypes.FindAsync(id);
        return t == null ? NotFound() : View("Form", t);
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, [Bind("Name,Description,BasePrice,Capacity,Amenities")] RoomType input)
    {
        var t = await _db.RoomTypes.FindAsync(id);
        if (t == null) return NotFound();

        input.Name = input.Name?.Trim() ?? "";
        if (await _db.RoomTypes.AnyAsync(x => x.Name == input.Name && x.Id != id))
            ModelState.AddModelError(nameof(RoomType.Name), "A room type with this name already exists.");
        if (!ModelState.IsValid) { input.Id = id; return View("Form", input); }

        // Price changes only affect future bookings; existing bookings keep their price snapshot.
        t.Name = input.Name; t.Description = input.Description; t.BasePrice = input.BasePrice;
        t.Capacity = input.Capacity; t.Amenities = input.Amenities;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"{t.Name} updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.RoomTypes.Include(x => x.Rooms).FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        if (t.Rooms.Any()) TempData["Error"] = "Remove or reassign this type's rooms before deleting it.";
        else
        {
            _db.RoomTypes.Remove(t);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{t.Name} deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
