using HotelBooking.Web.Data;
using HotelBooking.Web.Services;
using HotelBooking.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Controllers;

public class RoomsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly AvailabilityService _availability;

    public RoomsController(ApplicationDbContext db, AvailabilityService availability)
    {
        _db = db;
        _availability = availability;
    }

    [HttpGet]
    public async Task<IActionResult> Search(DateOnly? checkIn, DateOnly? checkOut, int guests = 2, int? roomTypeId = null)
    {
        var vm = new SearchViewModel
        {
            CheckIn = checkIn ?? AvailabilityService.Today.AddDays(1),
            CheckOut = checkOut ?? AvailabilityService.Today.AddDays(2),
            Guests = Math.Clamp(guests, 1, 10),
            RoomTypeId = roomTypeId,
            AllTypes = await _db.RoomTypes.AsNoTracking().OrderBy(t => t.BasePrice).ToListAsync()
        };

        // Only run the search when dates were actually submitted
        if (checkIn.HasValue && checkOut.HasValue)
        {
            vm.Error = AvailabilityService.ValidateStay(vm.CheckIn, vm.CheckOut);
            if (vm.Error == null)
                vm.Results = await _availability.SearchAsync(vm.CheckIn, vm.CheckOut, vm.Guests, vm.RoomTypeId);
        }
        return View(vm);
    }
}
