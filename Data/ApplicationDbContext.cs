using HotelBooking.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<RoomType>(e =>
        {
            e.Property(x => x.BasePrice).HasPrecision(10, 2);
            e.HasIndex(x => x.Name).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_RoomTypes_BasePrice", "[BasePrice] > 0");
                t.HasCheckConstraint("CK_RoomTypes_Capacity", "[Capacity] > 0");
            });
        });

        b.Entity<Room>(e =>
        {
            e.HasIndex(x => x.RoomNumber).IsUnique();
            e.HasOne(x => x.RoomType).WithMany(t => t.Rooms)
             .HasForeignKey(x => x.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Booking>(e =>
        {
            e.Property(x => x.PricePerNight).HasPrecision(10, 2);
            e.Property(x => x.TotalPrice).HasPrecision(12, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Reference).IsUnique();
            e.HasIndex(x => new { x.RoomId, x.CheckIn, x.CheckOut });
            e.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Bookings_Dates", "[CheckOut] > [CheckIn]");
                t.HasCheckConstraint("CK_Bookings_Guests", "[Guests] > 0");
            });
        });
    }
}
