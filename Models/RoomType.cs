using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Web.Models;

public class RoomType
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = "";

    [Required, StringLength(500)]
    public string Description { get; set; } = "";

    [Display(Name = "Price per night (₹)"), Range(1, 1000000)]
    public decimal BasePrice { get; set; }

    [Display(Name = "Max guests"), Range(1, 10)]
    public int Capacity { get; set; } = 2;

    [StringLength(200), Display(Name = "Amenities (comma separated)")]
    public string? Amenities { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();

    public IEnumerable<string> AmenityList =>
        (Amenities ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
