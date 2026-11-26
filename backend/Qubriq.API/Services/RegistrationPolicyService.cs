using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;
namespace Qubriq.API.Services;
public sealed class RegistrationPolicyService
{
    private readonly QubriqDbContext _db;
    public RegistrationPolicyService(QubriqDbContext db) => _db = db;
    public Task<AccountRegistrationPolicy?> GetAsync(CancellationToken ct = default) =>
        _db.AccountRegistrationPolicies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
}
