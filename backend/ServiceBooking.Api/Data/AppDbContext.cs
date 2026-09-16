using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Entities;

namespace ServiceBooking.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<User> Users => Set<User>();

  public DbSet<Service> Services => Set<Service>();

  public DbSet<Staff> Staffs => Set<Staff>();

  public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();

  public DbSet<Booking> Bookings => Set<Booking>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    ConfigureUser(modelBuilder);
    ConfigureService(modelBuilder);
    ConfigureStaff(modelBuilder);
    ConfigureWorkSchedule(modelBuilder);
    ConfigureBooking(modelBuilder);
  }

  private static void ConfigureUser(ModelBuilder modelBuilder)
  {
    var entity = modelBuilder.Entity<User>();

    entity.HasKey(x => x.Id);

    entity.Property(x => x.Email)
        .HasMaxLength(255)
        .IsRequired();

    entity.Property(x => x.PasswordHash)
        .IsRequired();

    entity.Property(x => x.Role)
        .HasConversion<string>()
        .HasMaxLength(20)
        .IsRequired();

    entity.HasIndex(x => x.Email)
        .IsUnique();
  }

  private static void ConfigureService(ModelBuilder modelBuilder)
  {
    var entity = modelBuilder.Entity<Service>();

    entity.HasKey(x => x.Id);

    entity.Property(x => x.Name)
        .HasMaxLength(200)
        .IsRequired();

    entity.Property(x => x.Description)
        .HasMaxLength(1000);

    entity.Property(x => x.Price)
        .HasPrecision(18, 2)
        .IsRequired();

    entity.Property(x => x.DurationMinutes)
        .IsRequired();

    entity.Property(x => x.IsActive)
        .IsRequired();
  }

  private static void ConfigureStaff(ModelBuilder modelBuilder)
  {
    var entity = modelBuilder.Entity<Staff>();

    entity.HasKey(x => x.Id);

    entity.Property(x => x.FullName)
        .HasMaxLength(200)
        .IsRequired();

    entity.Property(x => x.Email)
        .HasMaxLength(255)
        .IsRequired();

    entity.HasIndex(x => x.Email)
        .IsUnique();
  }

  private static void ConfigureWorkSchedule(ModelBuilder modelBuilder)
  {
    var entity = modelBuilder.Entity<WorkSchedule>();

    entity.HasKey(x => x.Id);

    entity.Property(x => x.WorkDate)
        .IsRequired();

    entity.Property(x => x.StartTime)
        .IsRequired();

    entity.Property(x => x.EndTime)
        .IsRequired();

    entity.HasOne(x => x.Staff)
        .WithMany(x => x.WorkSchedules)
        .HasForeignKey(x => x.StaffId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasIndex(x => new
    {
      x.StaffId,
      x.WorkDate
    });
  }

  private static void ConfigureBooking(ModelBuilder modelBuilder)
  {
    var entity = modelBuilder.Entity<Booking>();

    entity.HasKey(x => x.Id);

    entity.Property(x => x.BookingCode)
        .HasMaxLength(50)
        .IsRequired();

    entity.HasIndex(x => x.BookingCode)
        .IsUnique();

    entity.Property(x => x.Status)
        .HasConversion<string>()
        .HasMaxLength(20)
        .IsRequired();

    entity.Property(x => x.CustomerNote)
        .HasMaxLength(1000);

    entity.Property(x => x.CancellationReason)
        .HasMaxLength(500);

    entity.HasOne(x => x.Customer)
        .WithMany(x => x.Bookings)
        .HasForeignKey(x => x.CustomerId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(x => x.Service)
        .WithMany(x => x.Bookings)
        .HasForeignKey(x => x.ServiceId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasOne(x => x.Staff)
        .WithMany(x => x.Bookings)
        .HasForeignKey(x => x.StaffId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.HasIndex(x => new
    {
      x.StaffId,
      x.StartTime,
      x.EndTime
    });

    entity.HasIndex(x => new
    {
      x.CustomerId,
      x.StartTime
    });

    entity.HasIndex(x => new
    {
      x.Status,
      x.StartTime
    });
  }
}