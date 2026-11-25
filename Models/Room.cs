using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Web.Models;

public class Room
{
    public int Id { get; set; }

    [Required, StringLength(10), Display(Name = "Room number")]
    public string RoomNumber { get; set; } = "";

    [Range(0, 100)]
    public int Floor { get; set; }

    [Display(Name = "Room type")]
    public int RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }

    [Display(Name = "Active (bookable)")]
    public bool IsActive { get; set; } = true;
}
