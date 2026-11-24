using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public BookingsController(QubriqDbContext db)
    {
        _db = db;
    }

    private long? GetAuthenticatedUserId()
    {
        var subject =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (long.TryParse(subject, out var userId))
        {
            return userId;
        }

        return null;
    }

    private async Task<bool> HasPermissionAsync(
        long userId,
        string permissionCode)
    {
        return await _db.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(
                _db.RolePermissions,
                userRole => userRole.RoleId,
                rolePermission => rolePermission.RoleId,
                (userRole, rolePermission) => rolePermission)
            .AnyAsync(rolePermission =>
                rolePermission.IsAllowed &&
                rolePermission.Permission.Code == permissionCode);
    }

    // GET: api/bookings
    // Internal users see all permitted bookings.
    // Booking customers see only their own bookings.
    [HttpGet]
    [Authorize(Policy = "BookingsRead")]
    public async Task<IActionResult> GetBookings(
        [FromQuery] string? status,
        [FromQuery] string? search)
    {
        var userId = GetAuthenticatedUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var canReadAll = await HasPermissionAsync(
            userId.Value,
            "bookings.read_all");

        var query = _db.Shipments
            .AsNoTracking()
            .Include(x => x.Customer)
            .AsQueryable();

        if (!canReadAll)
        {
            query = query.Where(x =>
                x.Customer.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(x =>
                x.TrackingNumber.Contains(term) ||
                x.Customer.CompanyName.Contains(term));
        }

        var bookings = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                bookingId = x.TrackingNumber,
                customer = x.Customer.CompanyName,
                pickupLocation = x.Origin,
                deliveryLocation = x.Destination,
                cargoType = x.CargoCategory,
                cargoName = x.CargoName,
                weight = x.TotalWeightKg,
                x.PackageCount,
                x.Priority,
                bookingDate = x.CreatedAtUtc,
                pickupDate = x.PickupDateUtc,
                expectedDeliveryDate = x.ExpectedDeliveryUtc,
                x.Status
            })
            .ToListAsync();

        return Ok(bookings);
    }

    // GET: api/bookings/customers
    // Only users with bookings.read_all can list customer records.
    [HttpGet("customers")]
    [Authorize(Policy = "BookingsReadAll")]
    public async Task<IActionResult> GetCustomers()
    {
        var customers = await _db.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.CompanyName)
            .Select(x => new
            {
                x.Id,
                x.CompanyName,
                x.ContactName,
                x.Email,
                x.Phone,
                x.Address
            })
            .ToListAsync();

        return Ok(customers);
    }

    // POST: api/bookings
    [HttpPost]
    [Authorize(Policy = "BookingsCreate")]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest request)
    {
        var userId = GetAuthenticatedUserId();

        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var canReadAll = await HasPermissionAsync(
            userId.Value,
            "bookings.read_all");

        Customer? customer = null;

        if (canReadAll)
        {
            if (!request.CustomerId.HasValue ||
                request.CustomerId.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "A valid customer must be selected."
                });
            }

            customer = await _db.Customers
                .FirstOrDefaultAsync(x =>
                    x.Id == request.CustomerId.Value &&
                    x.IsActive);
        }
        else
        {
            // Never trust a customer ID submitted by an external customer.
            // Resolve the customer's own profile from the authenticated user.
            customer = await _db.Customers
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId.Value &&
                    x.IsActive);
        }

        if (customer == null)
        {
            return BadRequest(new
            {
                message = canReadAll
                    ? "Select an active customer before creating a booking."
                    : "Your account does not have an active customer profile. Contact Qubriq support."
            });
        }

        if (request.PickupDateUtc == default)
        {
            return BadRequest(new
            {
                message = "A valid pickup date is required."
            });
        }

        if (request.ExpectedDeliveryUtc.HasValue &&
            request.ExpectedDeliveryUtc.Value < request.PickupDateUtc)
        {
            return BadRequest(new
            {
                message = "Expected delivery cannot be earlier than pickup."
            });
        }

        var trackingNumber =
            $"QBR-{DateTime.UtcNow:yyyyMMdd}-" +
            Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var shipment = new Shipment
        {
            TrackingNumber = trackingNumber,
            CustomerId = customer.Id,
            Origin = request.Origin.Trim(),
            Destination = request.Destination.Trim(),
            Status = request.Status,
            CreatedAtUtc = DateTime.UtcNow,
            ExpectedDeliveryUtc = request.ExpectedDeliveryUtc,

            CargoName = request.CargoName.Trim(),
            CargoCategory = request.CargoCategory?.Trim(),
            PackageCount = request.PackageCount,
            TotalWeightKg = request.TotalWeightKg,
            LengthCm = request.LengthCm,
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            IsFragile = request.IsFragile,
            SpecialHandlingRequirements =
                request.SpecialHandlingRequirements?.Trim(),

            Priority = request.Priority,
            PickupDateUtc = request.PickupDateUtc,
            DeliveryTimeWindow = request.DeliveryTimeWindow?.Trim(),
            AdditionalInstructions =
                request.AdditionalInstructions?.Trim()
        };

        _db.Shipments.Add(shipment);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetBookings),
            new { },
            new
            {
                shipment.Id,
                shipment.TrackingNumber,
                shipment.Status,
                message = "Booking created successfully."
            });
    }
}
