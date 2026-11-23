using Microsoft.EntityFrameworkCore;
using Qubriq.API.Models;

namespace Qubriq.API.Data;

public class QubriqDbContext : DbContext
{
    public QubriqDbContext(DbContextOptions<QubriqDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<ShipmentAssignment> ShipmentAssignments => Set<ShipmentAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(250);

            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(x => new { x.UserId, x.RoleId });

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ContactName).HasMaxLength(150);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Address).HasMaxLength(500);
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("Shipments");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TrackingNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Origin).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Destination).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();

            // Booking details
            entity.Property(x => x.CargoName).HasMaxLength(200).IsRequired(false);
            entity.Property(x => x.CargoCategory).HasMaxLength(100);
            entity.Property(x => x.PackageCount).IsRequired(false);
            entity.Property(x => x.TotalWeightKg).HasPrecision(12, 2).IsRequired(false);
            entity.Property(x => x.LengthCm).HasPrecision(12, 2);
            entity.Property(x => x.WidthCm).HasPrecision(12, 2);
            entity.Property(x => x.HeightCm).HasPrecision(12, 2);
            entity.Property(x => x.IsFragile).IsRequired(false);
            entity.Property(x => x.SpecialHandlingRequirements).HasMaxLength(500);
            entity.Property(x => x.Priority).HasMaxLength(20).IsRequired(false);
            entity.Property(x => x.PickupDateUtc).IsRequired(false);
            entity.Property(x => x.DeliveryTimeWindow).HasMaxLength(100);
            entity.Property(x => x.AdditionalInstructions).HasMaxLength(1000);
            entity.HasIndex(x => x.TrackingNumber).IsUnique();

            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Shipments)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.RegistrationNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.VehicleType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CapacityKg).HasColumnType("decimal(12,2)");
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();

            entity.HasIndex(x => x.RegistrationNumber).IsUnique();
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.ToTable("Drivers");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.LicenseNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();

            entity.HasIndex(x => x.LicenseNumber).IsUnique();
        });

        modelBuilder.Entity<ShipmentAssignment>(entity =>
        {
            entity.ToTable("ShipmentAssignments");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();

            entity.HasOne(x => x.Shipment)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Vehicle)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Driver)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.ShipmentId);
            entity.HasIndex(x => x.VehicleId);
            entity.HasIndex(x => x.DriverId);
        });
    }
}
