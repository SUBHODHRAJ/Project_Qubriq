using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public BookingsController(QubriqDbContext db)
    {
        _db = db;
    }

    // GET: api/bookings
    // Optional filters: status, search
    [HttpGet]
    public async Task<IActionResult> GetBookings(
        [FromQuery] string? status,
        [FromQuery] string? search)
    {
        var query = _db.Shipments
            .AsNoTracking()
            .Include(x => x.Customer)
            .AsQueryable();

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
    [HttpGet("customers")]
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
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest request)
    {
        var customer = await _db.Customers
            .FirstOrDefaultAsync(x =>
                x.Id == request.CustomerId && x.IsActive);

        if (customer == null)
        {
            return BadRequest(new
            {
                message = "Select an active customer before creating a booking."
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
            CustomerId = request.CustomerId,
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
