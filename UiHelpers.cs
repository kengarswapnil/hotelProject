using HotelBooking.Web.Models;

namespace HotelBooking.Web;

public static class Ui
{
    public static string Badge(BookingStatus s) => s switch
    {
        BookingStatus.Confirmed => "text-bg-primary",
        BookingStatus.CheckedIn => "text-bg-success",
        BookingStatus.Completed => "text-bg-secondary",
        _ => "text-bg-danger"
    };

    public static string Money(decimal v) => "₹" + v.ToString("N0");
    public static string D(DateOnly d) => d.ToString("dd MMM yyyy");
    public static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd");
}
