using System.Data;
using HotelBooking.Web.Data;
using HotelBooking.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Services;

public record BookingResult(Booking? Booking, string? Error)
{
    public bool Ok => Booking != null;
}

public class BookingService
{
    private readonly ApplicationDbContext _db;
    private readonly AvailabilityService _availability;

    public BookingService(ApplicationDbContext db, AvailabilityService availability)
    {
        _db = db;
        _availability = availability;
    }

    public async Task<BookingResult> CreateAsync(string userId, int roomTypeId, DateOnly checkIn, DateOnly checkOut, int guests, string? notes)
    {
        var dateError = AvailabilityService.ValidateStay(checkIn, checkOut);
        if (dateError != null) return new(null, dateError);

        var type = await _db.RoomTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == roomTypeId);
        if (type == null) return new(null, "Room type not found.");
        if (guests < 1 || guests > type.Capacity) return new(null, $"This room allows 1 to {type.Capacity} guests.");

        // Serializable transaction: the availability check and the insert are atomic, so two
        // simultaneous requests cannot both claim the last free room (the loser gets a deadlock
        // error or sees "no rooms" and is told to retry).
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var room = await _availability.FreeRooms(checkIn, checkOut, roomTypeId)
                .OrderBy(r => r.RoomNumber).FirstOrDefaultAsync();
            if (room == null) return new(null, "Sorry, no rooms of this type are available for those dates.");

            var nights = checkOut.DayNumber - checkIn.DayNumber;
            var booking = new Booking
            {
                Reference = "HB" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                UserId = userId,
                RoomId = room.Id,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Guests = guests,
                PricePerNight = type.BasePrice,          // price snapshot, computed server-side
                TotalPrice = type.BasePrice * nights,
                Status = BookingStatus.Confirmed,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return new(booking, null);
        }
        catch (Exception ex) when (ex is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            _db.ChangeTracker.Clear();
            return new(null, "Another booking was being made at the same time. Please try again.");
        }
    }

    /// <summary>Guest cancellation: own booking only, status Confirmed, and before the check-in date.</summary>
    public async Task<string?> CancelByGuestAsync(int bookingId, string userId)
    {
        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId && x.UserId == userId);
        if (b == null) return "Booking not found.";
        if (b.Status != BookingStatus.Confirmed) return "Only confirmed bookings can be cancelled.";
        if (b.CheckIn <= AvailabilityService.Today) return "Bookings can only be cancelled before the check-in date. Please contact the hotel.";
        b.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync();
        return null;
    }

    /// <summary>Staff status workflow: Confirmed → CheckedIn → Completed, or Confirmed → Cancelled.</summary>
    public async Task<string?> ChangeStatusAsync(int bookingId, BookingStatus next)
    {
        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
        if (b == null) return "Booking not found.";

        var allowed = (b.Status, next) switch
        {
            (BookingStatus.Confirmed, BookingStatus.CheckedIn) => true,
            (BookingStatus.Confirmed, BookingStatus.Cancelled) => true,
            (BookingStatus.CheckedIn, BookingStatus.Completed) => true,
            _ => false
        };
        if (!allowed) return $"A {b.Status} booking cannot be changed to {next}.";
        if (next == BookingStatus.CheckedIn && b.CheckIn > AvailabilityService.Today)
            return "Cannot check in before the arrival date.";

        b.Status = next;
        await _db.SaveChangesAsync();
        return null;
    }
}
