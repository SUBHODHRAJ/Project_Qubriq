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
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<ShipmentAssignment> ShipmentAssignments => Set<ShipmentAssignment>();
    public DbSet<ShipmentRecipient> ShipmentRecipients => Set<ShipmentRecipient>();
    public DbSet<DeliveryProof> DeliveryProofs => Set<DeliveryProof>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ShipmentPackage> ShipmentPackages => Set<ShipmentPackage>();
    public DbSet<OptimizationRun> OptimizationRuns => Set<OptimizationRun>();
    public DbSet<PackagePlacement> PackagePlacements => Set<PackagePlacement>();
    public DbSet<LoadingPlanApproval> LoadingPlanApprovals => Set<LoadingPlanApproval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================
        // USERS
        // ==========================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(254)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .HasMaxLength(500)
                .IsRequired(false);

            entity.Property(x => x.RecipientType)
                .HasMaxLength(30);

            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasOne(x => x.Customer)
                .WithOne(x => x.User)
                .HasForeignKey<Customer>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Driver)
                .WithOne(x => x.User)
                .HasForeignKey<Driver>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================
        // ROLES
        // ==========================================

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(250);

            entity.HasIndex(x => x.Name).IsUnique();
        });

        // ==========================================
        // USER ROLES
        // ==========================================

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

        // ==========================================
        // PERMISSIONS
        // ==========================================

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(x => x.Module)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.Action)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(300);

            entity.HasIndex(x => x.Code).IsUnique();
        });

        // ==========================================
        // ROLE PERMISSIONS
        // ==========================================

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(x => new { x.RoleId, x.PermissionId });

            entity.HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================
        // EXTERNAL LOGINS
        // ==========================================

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.ToTable("ExternalLogins");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Provider)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ProviderSubject)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.Provider,
                x.ProviderSubject
            }).IsUnique();

            entity.HasIndex(x => x.UserId);

            entity.HasOne(x => x.User)
                .WithMany(x => x.ExternalLogins)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================
        // CUSTOMERS
        // ==========================================

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CompanyName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.ContactName).HasMaxLength(150);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Address).HasMaxLength(500);

            entity.HasIndex(x => x.UserId).IsUnique();
        });

        // ==========================================
        // SHIPMENTS
        // ==========================================

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("Shipments");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TrackingNumber)
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(x => x.Origin)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.Destination)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(40)
                .IsRequired();

            // Existing booking details
            entity.Property(x => x.CargoName)
                .HasMaxLength(200)
                .IsRequired(false);

            entity.Property(x => x.CargoCategory).HasMaxLength(100);
            entity.Property(x => x.PackageCount).IsRequired(false);

            entity.Property(x => x.TotalWeightKg)
                .HasPrecision(12, 2)
                .IsRequired(false);

            entity.Property(x => x.LengthCm).HasPrecision(12, 2);
            entity.Property(x => x.WidthCm).HasPrecision(12, 2);
            entity.Property(x => x.HeightCm).HasPrecision(12, 2);

            entity.Property(x => x.IsFragile).IsRequired(false);

            entity.Property(x => x.SpecialHandlingRequirements)
                .HasMaxLength(500);

            entity.Property(x => x.Priority)
                .HasMaxLength(20)
                .IsRequired(false);

            entity.Property(x => x.PickupDateUtc).IsRequired(false);
            entity.Property(x => x.DeliveryTimeWindow).HasMaxLength(100);
            entity.Property(x => x.AdditionalInstructions).HasMaxLength(1000);

            entity.HasIndex(x => x.TrackingNumber).IsUnique();

            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Shipments)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================
        // VEHICLES
        // ==========================================

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.RegistrationNumber)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.VehicleType)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.CapacityKg)
                .HasColumnType("decimal(12,2)");

            entity.Property(x => x.CargoLengthCm)
                .HasPrecision(12, 2)
                .IsRequired(false);

            entity.Property(x => x.CargoWidthCm)
                .HasPrecision(12, 2)
                .IsRequired(false);

            entity.Property(x => x.CargoHeightCm)
                .HasPrecision(12, 2)
                .IsRequired(false);

            entity.Property(x => x.Status)
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(x => x.RegistrationNumber).IsUnique();
        });

        // ==========================================
        // SHIPMENT PACKAGES
        // ==========================================

        modelBuilder.Entity<ShipmentPackage>(entity =>
        {
            entity.ToTable("ShipmentPackages");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Description)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.PackageCode).HasMaxLength(80);
            entity.Property(x => x.Category).HasMaxLength(100);
            entity.Property(x => x.Quantity).IsRequired();

            entity.Property(x => x.LengthCm).HasPrecision(12, 2);
            entity.Property(x => x.WidthCm).HasPrecision(12, 2);
            entity.Property(x => x.HeightCm).HasPrecision(12, 2);
            entity.Property(x => x.WeightKg).HasPrecision(12, 2);

            entity.Property(x => x.SpecialHandlingRequirements)
                .HasMaxLength(500);

            entity.HasOne(x => x.Shipment)
                .WithMany(x => x.Packages)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.ShipmentId);
        });

        // ==========================================
        // OPTIMIZATION RUNS
        // ==========================================

        modelBuilder.Entity<OptimizationRun>(entity =>
        {
            entity.ToTable("OptimizationRuns");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Algorithm)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(x => x.VehicleLengthCm).HasPrecision(12, 2);
            entity.Property(x => x.VehicleWidthCm).HasPrecision(12, 2);
            entity.Property(x => x.VehicleHeightCm).HasPrecision(12, 2);
            entity.Property(x => x.VehicleCapacityKg).HasPrecision(12, 2);

            entity.Property(x => x.TotalWeightKg).HasPrecision(12, 2);
            entity.Property(x => x.UsedWeightKg).HasPrecision(12, 2);
            entity.Property(x => x.WeightUtilizationPercent).HasPrecision(8, 2);
            entity.Property(x => x.VolumeUtilizationPercent).HasPrecision(8, 2);

            entity.Property(x => x.Warnings)
                .HasColumnType("longtext");

            entity.HasOne(x => x.Shipment)
                .WithMany(x => x.OptimizationRuns)
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Vehicle)
                .WithMany(x => x.OptimizationRuns)
                .HasForeignKey(x => x.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.ShipmentId);
            entity.HasIndex(x => x.VehicleId);
            entity.HasIndex(x => x.CreatedByUserId);
            entity.HasIndex(x => x.CreatedAtUtc);
        });

        // ==========================================
        // PACKAGE PLACEMENTS
        // ==========================================

        modelBuilder.Entity<PackagePlacement>(entity =>
        {
            entity.ToTable("PackagePlacements");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PackageDescription)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.LengthCm).HasPrecision(12, 2);
            entity.Property(x => x.WidthCm).HasPrecision(12, 2);
            entity.Property(x => x.HeightCm).HasPrecision(12, 2);
            entity.Property(x => x.WeightKg).HasPrecision(12, 2);

            entity.Property(x => x.PlacementStatus)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.X).HasPrecision(12, 2);
            entity.Property(x => x.Y).HasPrecision(12, 2);
            entity.Property(x => x.Z).HasPrecision(12, 2);

            entity.Property(x => x.Orientation).HasMaxLength(60);
            entity.Property(x => x.UnplacedReason).HasMaxLength(500);

            entity.HasOne(x => x.OptimizationRun)
                .WithMany(x => x.Placements)
                .HasForeignKey(x => x.OptimizationRunId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ShipmentPackage)
                .WithMany(x => x.Placements)
                .HasForeignKey(x => x.ShipmentPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.OptimizationRunId);
            entity.HasIndex(x => x.ShipmentPackageId);

            entity.HasIndex(x => new
            {
                x.OptimizationRunId,
                x.ShipmentPackageId,
                x.UnitNumber
            }).IsUnique();
        });

        // ==========================================
        // LOADING PLAN APPROVALS
        // ==========================================

        modelBuilder.Entity<LoadingPlanApproval>(entity =>
        {
            entity.ToTable("LoadingPlanApprovals");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Decision)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.Comments).HasMaxLength(1000);

            entity.HasOne(x => x.OptimizationRun)
                .WithMany(x => x.Approvals)
                .HasForeignKey(x => x.OptimizationRunId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ReviewedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.OptimizationRunId);
            entity.HasIndex(x => x.ReviewedByUserId);
            entity.HasIndex(x => x.ReviewedAtUtc);
        });
        // ==========================================
        // DRIVERS
        // ==========================================

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.ToTable("Drivers");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FullName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Phone).HasMaxLength(30);

            entity.Property(x => x.LicenseNumber)
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(x => x.LicenseNumber).IsUnique();
            entity.HasIndex(x => x.UserId).IsUnique();
        });

        // ==========================================
        // SHIPMENT ASSIGNMENTS
        // ==========================================

        modelBuilder.Entity<ShipmentAssignment>(entity =>
        {
            entity.ToTable("ShipmentAssignments");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Status)
                .HasMaxLength(40)
                .IsRequired();

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

        // ==========================================
        // SHIPMENT RECIPIENTS
        // ==========================================

        modelBuilder.Entity<ShipmentRecipient>(entity =>
        {
            entity.ToTable("ShipmentRecipients");
            entity.HasKey(x => x.Id);

            entity.HasOne(x => x.Shipment)
                .WithMany()
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.User)
                .WithMany(x => x.ShipmentRecipients)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.ShipmentId,
                x.UserId
            }).IsUnique();
        });

        // ==========================================
        // DELIVERY PROOFS
        // ==========================================

        modelBuilder.Entity<DeliveryProof>(entity =>
        {
            entity.ToTable("DeliveryProofs");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ProofType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.FileReference)
                .HasMaxLength(500);

            entity.Property(x => x.Remarks)
                .HasMaxLength(1000);

            entity.HasOne(x => x.Shipment)
                .WithMany()
                .HasForeignKey(x => x.ShipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.SubmittedByUser)
                .WithMany(x => x.DeliveryProofs)
                .HasForeignKey(x => x.SubmittedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.ShipmentId);
            entity.HasIndex(x => x.SubmittedByUserId);
        });

        // ==========================================
        // AUDIT LOGS
        // ==========================================

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Action)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(x => x.EntityType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.EntityId)
                .HasMaxLength(100);

            entity.Property(x => x.Details)
                .HasColumnType("longtext");

            entity.HasOne(x => x.User)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => new
            {
                x.EntityType,
                x.EntityId
            });
        });
    }
}

