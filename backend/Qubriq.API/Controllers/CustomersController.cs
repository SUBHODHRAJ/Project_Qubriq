using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs;
using Qubriq.API.Models;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly QubriqDbContext _db;

    public CustomersController(QubriqDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.CompanyName.Contains(term) ||
                (x.ContactName != null && x.ContactName.Contains(term)) ||
                (x.Email != null && x.Email.Contains(term)) ||
                (x.Phone != null && x.Phone.Contains(term)));
        }

        var customers = await query
            .OrderBy(x => x.CompanyName)
            .Select(x => new
            {
                x.Id,
                x.CompanyName,
                x.ContactName,
                x.Email,
                x.Phone,
                x.Address,
                x.IsActive,
                x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetCustomer(long id)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.CompanyName,
                x.ContactName,
                x.Email,
                x.Phone,
                x.Address,
                x.IsActive,
                x.CreatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (customer == null)
            return NotFound(new { message = "Customer not found." });

        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request)
    {
        var email = NormalizeEmail(request.Email);

        if (email != null && await EmailExists(email))
            return Conflict(new { message = "A customer with this email already exists." });

        var customer = new Customer
        {
            CompanyName = request.CompanyName.Trim(),
            ContactName = NormalizeText(request.ContactName),
            Email = email,
            Phone = NormalizeText(request.Phone),
            Address = NormalizeText(request.Address),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = customer.Id },
            new
            {
                customer.Id,
                customer.CompanyName,
                customer.ContactName,
                customer.Email,
                customer.Phone,
                customer.Address,
                customer.IsActive,
                customer.CreatedAtUtc,
                message = "Customer created successfully."
            });
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateCustomer(
        long id,
        [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
            return NotFound(new { message = "Customer not found." });

        var email = NormalizeEmail(request.Email);

        if (email != null && await EmailExists(email, id))
            return Conflict(new { message = "Another customer already uses this email." });

        customer.CompanyName = request.CompanyName.Trim();
        customer.ContactName = NormalizeText(request.ContactName);
        customer.Email = email;
        customer.Phone = NormalizeText(request.Phone);
        customer.Address = NormalizeText(request.Address);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            customer.Id,
            customer.CompanyName,
            customer.ContactName,
            customer.Email,
            customer.Phone,
            customer.Address,
            customer.IsActive,
            customer.CreatedAtUtc,
            message = "Customer updated successfully."
        });
    }

    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> UpdateCustomerStatus(
        long id,
        [FromBody] UpdateCustomerStatusRequest request)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id);

        if (customer == null)
            return NotFound(new { message = "Customer not found." });

        customer.IsActive = request.IsActive;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            customer.Id,
            customer.CompanyName,
            customer.IsActive,
            message = request.IsActive
                ? "Customer activated successfully."
                : "Customer deactivated successfully."
        });
    }

    private async Task<bool> EmailExists(
        string email,
        long? excludeCustomerId = null)
    {
        var normalizedEmail = email.ToLowerInvariant();

        var query = _db.Customers
            .AsNoTracking()
            .Where(x => x.Email != null &&
                        x.Email.ToLower() == normalizedEmail);

        if (excludeCustomerId.HasValue)
            query = query.Where(x => x.Id != excludeCustomerId.Value);

        return await query.AnyAsync();
    }

    private static string? NormalizeEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToLowerInvariant();
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
