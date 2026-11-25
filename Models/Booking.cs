using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBooking.Web.Models;

public enum BookingStatus { Confirmed, CheckedIn, Completed, Cancelled }

public class Booking
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string Reference { get; set; } = "";

    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Guests { get; set; }

    /// <summary>Snapshot of the nightly rate at booking time, so later price changes don't alter old bookings.</summary>
    public decimal PricePerNight { get; set; }
    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [NotMapped]
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;
}
