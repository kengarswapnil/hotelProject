using HotelBooking.Web.Data;
using HotelBooking.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Services;

public class RoomTypeAvailability
{
    public RoomType Type { get; set; } = null!;
    public int Available { get; set; }
    public int Nights { get; set; }
    public decimal Total => Type.BasePrice * Nights;
}

public class AvailabilityService
{
    public const int MaxNights = 30;
    private readonly ApplicationDbContext _db;

    public AvailabilityService(ApplicationDbContext db) => _db = db;

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Returns an error message if the stay dates are invalid, otherwise null.</summary>
    public static string? ValidateStay(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkIn < Today) return "Check-in date cannot be in the past.";
        if (checkOut <= checkIn) return "Check-out must be after check-in.";
        if (checkOut.DayNumber - checkIn.DayNumber > MaxNights) return $"Maximum stay is {MaxNights} nights.";
        return null;
    }

    /// <summary>
    /// Active rooms with no Confirmed/CheckedIn booking overlapping [checkIn, checkOut).
    /// Overlap rule: existing.CheckIn &lt; new.CheckOut AND existing.CheckOut &gt; new.CheckIn
    /// (so same-day turnover is allowed: one guest leaves the morning another arrives).
    /// </summary>
    public IQueryable<Room> FreeRooms(DateOnly checkIn, DateOnly checkOut, int? roomTypeId = null) =>
        _db.Rooms.Where(r => r.IsActive
            && (roomTypeId == null || r.RoomTypeId == roomTypeId)
            && !_db.Bookings.Any(b => b.RoomId == r.Id
                && (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
                && b.CheckIn < checkOut && b.CheckOut > checkIn));

    public async Task<List<RoomTypeAvailability>> SearchAsync(DateOnly checkIn, DateOnly checkOut, int guests, int? roomTypeId)
    {
        var counts = await FreeRooms(checkIn, checkOut, roomTypeId)
            .GroupBy(r => r.RoomTypeId)
            .Select(g => new { TypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TypeId, x => x.Count);

        var types = await _db.RoomTypes
            .Where(t => t.Capacity >= guests && (roomTypeId == null || t.Id == roomTypeId))
            .OrderBy(t => t.BasePrice)
            .ToListAsync();

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        return types.Select(t => new RoomTypeAvailability
        {
            Type = t,
            Nights = nights,
            Available = counts.TryGetValue(t.Id, out var c) ? c : 0
        }).ToList();
    }
}
