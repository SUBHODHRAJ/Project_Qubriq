# Qubriq account registration implementation - PowerShell 5.1
# Source-file changes only. No migration, SQL, MySQL connection, or DB writes.
$ErrorActionPreference = "Stop"
$B = "S:\Project_Qubriq\backend\Qubriq.API"
$F = "S:\Project_Qubriq\frontend"
if (!(Test-Path $B) -or !(Test-Path $F)) { throw "Qubriq backend/frontend path not found." }

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backup = "S:\Project_Qubriq\backend\_Backups\AccountRegistration_$stamp"
$targets = @(
    @{Root=$B; Rel="Data\QubriqDbContext.cs"},
    @{Root=$B; Rel="Program.cs"},
    @{Root=$B; Rel="Controllers\AdminCustomersController.cs"},
    @{Root=$B; Rel="Controllers\AdminRecipientsController.cs"},
    @{Root=$B; Rel="Controllers\AdminUsersController.cs"},
    @{Root=$F; Rel="src\routes\AppRoutes.jsx"},
    @{Root=$F; Rel="src\pages\SignupPage.jsx"}
)
New-Item -ItemType Directory -Path $backup -Force | Out-Null
foreach ($t in $targets) {
    $src = Join-Path $t.Root $t.Rel
    if (Test-Path $src) {
        $dst = Join-Path $backup $t.Rel
        New-Item -ItemType Directory -Path (Split-Path $dst -Parent) -Force | Out-Null
        Copy-Item $src $dst -Force
    }
}
function Put($path, $body) {
    $dir = Split-Path $path -Parent
    if (!(Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Set-Content -Path $path -Value $body -Encoding UTF8
}

Put (Join-Path $B "Models\AccountRegistrationPolicy.cs") @'
namespace Qubriq.API.Models;
public class AccountRegistrationPolicy
{
    public int Id { get; set; } = 1;
    public bool PublicRegistrationEnabled { get; set; } = true;
    public bool BookingCustomerSignupEnabled { get; set; } = true;
    public bool FinalRecipientSignupEnabled { get; set; } = false;
    public bool RequireAdminApproval { get; set; } = false;
    public bool ActivatePublicAccountsImmediately { get; set; } = true;
    public bool AdminAccountCreationEnabled { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public long? UpdatedByUserId { get; set; }
}
'@

Put (Join-Path $B "DTOs\Auth\PublicRegistrationRequest.cs") @'
using System.ComponentModel.DataAnnotations;
namespace Qubriq.API.DTOs.Auth;
public sealed class PublicRegistrationRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = "";
    [Required] public string AccountType { get; set; } = "";
    [StringLength(200)] public string? CompanyName { get; set; }
    [StringLength(150)] public string? ContactName { get; set; }
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(500)] public string? Address { get; set; }
}
'@

Put (Join-Path $B "DTOs\Admin\UpdateRegistrationPolicyRequest.cs") @'
namespace Qubriq.API.DTOs.Admin;
public sealed class UpdateRegistrationPolicyRequest
{
    public bool PublicRegistrationEnabled { get; set; }
    public bool BookingCustomerSignupEnabled { get; set; }
    public bool FinalRecipientSignupEnabled { get; set; }
    public bool RequireAdminApproval { get; set; }
    public bool ActivatePublicAccountsImmediately { get; set; }
    public bool AdminAccountCreationEnabled { get; set; }
}
'@

Put (Join-Path $B "Services\RegistrationPolicyService.cs") @'
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
'@

Put (Join-Path $B "Controllers\RegistrationController.cs") @'
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs.Auth;
using Qubriq.API.Models;
using Qubriq.API.Services;
namespace Qubriq.API.Controllers;
[ApiController]
[Route("api/auth")]
public sealed class RegistrationController : ControllerBase
{
    private readonly QubriqDbContext _db;
    private readonly RegistrationPolicyService _policies;
    private readonly PasswordHasher<User> _hasher = new();
    public RegistrationController(QubriqDbContext db, RegistrationPolicyService policies)
    { _db = db; _policies = policies; }

    [AllowAnonymous, HttpGet("registration-policy")]
    public async Task<IActionResult> PublicPolicy(CancellationToken ct)
    {
        var p = await _policies.GetAsync(ct);
        if (p == null) return StatusCode(503, new { message = "Registration policy is not initialized. Apply the migration first." });
        var types = new List<string>();
        if (p.PublicRegistrationEnabled && p.BookingCustomerSignupEnabled) types.Add("BookingCustomer");
        if (p.PublicRegistrationEnabled && p.FinalRecipientSignupEnabled) types.Add("FinalRecipient");
        return Ok(new { publicRegistrationEnabled = p.PublicRegistrationEnabled, accountTypes = types });
    }

    [AllowAnonymous, HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] PublicRegistrationRequest r, CancellationToken ct)
    {
        var p = await _policies.GetAsync(ct);
        if (p == null) return StatusCode(503, new { message = "Registration policy is not initialized." });
        if (!p.PublicRegistrationEnabled) return StatusCode(403, new { message = "Public registration is disabled." });

        var type = r.AccountType.Trim();
        if (type == "BookingCustomer" && !p.BookingCustomerSignupEnabled ||
            type == "FinalRecipient" && !p.FinalRecipientSignupEnabled)
            return StatusCode(403, new { message = "This account type is not currently available." });
        if (type != "BookingCustomer" && type != "FinalRecipient")
            return BadRequest(new { message = "Unsupported public account type." });

        var name = r.FullName.Trim();
        var email = r.Email.Trim().ToLowerInvariant();
        if (type == "BookingCustomer" && string.IsNullOrWhiteSpace(r.CompanyName))
            return BadRequest(new { message = "Company name is required." });
        try {
            if (!string.Equals(new System.Net.Mail.MailAddress(email).Address, email, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Invalid email address." });
        } catch (FormatException) { return BadRequest(new { message = "Invalid email address." }); }

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (await _db.Users.AnyAsync(x => x.Email == email, ct))
            return Conflict(new { message = "An account with this email already exists." });
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.Name == type, ct);
        if (role == null) return StatusCode(500, new { message = "Required RBAC role is missing." });

        var now = DateTime.UtcNow;
        var pending = p.RequireAdminApproval || !p.ActivatePublicAccountsImmediately;
        var user = new User {
            FullName = name, Email = email, IsActive = !pending, EmailVerified = false,
            RecipientType = type == "FinalRecipient" ? "FinalRecipient" : null,
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
        user.PasswordHash = _hasher.HashPassword(user, r.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAtUtc = now });

        if (type == "BookingCustomer") {
            _db.Customers.Add(new Customer {
                UserId = user.Id, CompanyName = r.CompanyName!.Trim(),
                ContactName = string.IsNullOrWhiteSpace(r.ContactName) ? name : r.ContactName.Trim(),
                Email = email, Phone = r.Phone?.Trim(), Address = r.Address?.Trim(),
                IsActive = !pending, CreatedAtUtc = now
            });
        }
        _db.AuditLogs.Add(new AuditLog {
            UserId = user.Id, Action = "PUBLIC_ACCOUNT_REGISTERED", EntityType = "User",
            EntityId = user.Id.ToString(), Details = "AccountType=" + type + "; PendingApproval=" + pending,
            CreatedAtUtc = now
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Created("/api/auth/login", new {
            message = pending ? "Registration received. Await administrator approval." : "Account created. You can now sign in.",
            userId = user.Id, accountType = type, requiresApproval = pending
        });
    }
}
'@

Put (Join-Path $B "Controllers\AdminRegistrationSettingsController.cs") @'
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.DTOs.Admin;
using Qubriq.API.Models;
namespace Qubriq.API.Controllers;
[ApiController, Route("api/admin/settings/registration"), Authorize(Roles = "Admin")]
public sealed class AdminRegistrationSettingsController : ControllerBase
{
    private readonly QubriqDbContext _db;
    public AdminRegistrationSettingsController(QubriqDbContext db) => _db = db;
    private long? AdminId() {
        var v = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(v, out var id) ? id : null;
    }
    [HttpGet]
    public async Task<IActionResult> Get() {
        var p = await _db.AccountRegistrationPolicies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);
        return p == null ? StatusCode(503, new { message = "Registration policy is not initialized." }) : Ok(p);
    }
    [HttpPut]
    public async Task<IActionResult> Update(UpdateRegistrationPolicyRequest r) {
        var id = AdminId(); if (!id.HasValue) return Unauthorized();
        var p = await _db.AccountRegistrationPolicies.FirstOrDefaultAsync(x => x.Id == 1);
        if (p == null) return StatusCode(503, new { message = "Registration policy is not initialized." });
        p.PublicRegistrationEnabled = r.PublicRegistrationEnabled;
        p.BookingCustomerSignupEnabled = r.BookingCustomerSignupEnabled;
        p.FinalRecipientSignupEnabled = r.FinalRecipientSignupEnabled;
        p.RequireAdminApproval = r.RequireAdminApproval;
        p.ActivatePublicAccountsImmediately = r.ActivatePublicAccountsImmediately;
        p.AdminAccountCreationEnabled = r.AdminAccountCreationEnabled;
        p.UpdatedAtUtc = DateTime.UtcNow; p.UpdatedByUserId = id;
        _db.AuditLogs.Add(new AuditLog {
            UserId = id, Action = "REGISTRATION_POLICY_UPDATED", EntityType = "AccountRegistrationPolicy",
            EntityId = "1", Details = System.Text.Json.JsonSerializer.Serialize(r), CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Registration policy updated.", policy = p });
    }
}
'@

Put (Join-Path $B "Controllers\AdminAccountApprovalsController.cs") @'
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qubriq.API.Data;
using Qubriq.API.Models;
namespace Qubriq.API.Controllers;
[ApiController, Route("api/admin/users"), Authorize(Roles = "Admin")]
public sealed class AdminAccountApprovalsController : ControllerBase
{
    private readonly QubriqDbContext _db;
    public AdminAccountApprovalsController(QubriqDbContext db) => _db = db;
    private long? AdminId() {
        var v = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(v, out var id) ? id : null;
    }
    private async Task<bool> HasUserAdminPermissions() {
        var id = AdminId(); if (!id.HasValue) return false;
        return await _db.UserRoles.Where(x => x.UserId == id.Value)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .AnyAsync(x => x.IsAllowed && x.Permission.Code == "users.create")
            && await _db.UserRoles.Where(x => x.UserId == id.Value)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .AnyAsync(x => x.IsAllowed && x.Permission.Code == "users.assign_role");
    }
    [HttpGet("pending")]
    public async Task<IActionResult> Pending() {
        if (!await HasUserAdminPermissions()) return Forbid();
        var list = await _db.Users.Where(x => !x.IsActive)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.FullName, x.Email, x.RecipientType, x.CreatedAtUtc,
                Roles = x.UserRoles.Select(ur => ur.Role.Name).ToList() }).ToListAsync();
        return Ok(list);
    }
    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id) {
        if (!await HasUserAdminPermissions()) return Forbid();
        var user = await _db.Users.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound();
        if (user.IsActive) return Conflict(new { message = "Account is already active." });
        user.IsActive = true; user.UpdatedAtUtc = DateTime.UtcNow;
        if (user.Customer != null) user.Customer.IsActive = true;
        _db.AuditLogs.Add(new AuditLog { UserId = AdminId(), Action = "USER_ACCOUNT_APPROVED",
            EntityType = "User", EntityId = id.ToString(), Details = "Account approved by Admin.",
            CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Account approved.", userId = id });
    }
}
'@

# Add DbSet + entity mapping + default seed; fail safely if expected anchor absent.
$ctxPath = Join-Path $B "Data\QubriqDbContext.cs"
$ctx = Get-Content $ctxPath -Raw
if ($ctx -notmatch 'DbSet<AccountRegistrationPolicy>') {
    $ctx = $ctx.Replace('public DbSet<User> Users => Set<User>();',
        'public DbSet<User> Users => Set<User>();' + "`r`n    public DbSet<AccountRegistrationPolicy> AccountRegistrationPolicies => Set<AccountRegistrationPolicy>();")
}
if ($ctx -notmatch 'Entity<AccountRegistrationPolicy>') {
    $anchor = '        // ==========================================' + "`r`n        // USERS"
    if (!$ctx.Contains($anchor)) { throw "DbContext mapping anchor not found; inspect backup and restore if necessary." }
    $mapping = @'
        modelBuilder.Entity<AccountRegistrationPolicy>(entity =>
        {
            entity.ToTable("AccountRegistrationPolicies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.HasData(new AccountRegistrationPolicy
            {
                Id = 1,
                PublicRegistrationEnabled = true,
                BookingCustomerSignupEnabled = true,
                FinalRecipientSignupEnabled = false,
                RequireAdminApproval = false,
                ActivatePublicAccountsImmediately = true,
                AdminAccountCreationEnabled = true,
                UpdatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });

'@
    $ctx = $ctx.Replace($anchor, $mapping + $anchor)
}
Set-Content $ctxPath $ctx -Encoding UTF8

$programPath = Join-Path $B "Program.cs"
$program = Get-Content $programPath -Raw
if ($program -notmatch 'AddScoped<RegistrationPolicyService>') {
    $program = $program.Replace('builder.Services.AddScoped<AuthService>();',
        'builder.Services.AddScoped<AuthService>();' + "`r`n    builder.Services.AddScoped<RegistrationPolicyService>();")
}
Set-Content $programPath $program -Encoding UTF8

# Enforce AdminAccountCreationEnabled in all existing Admin provisioning endpoints.
# This ensures the policy is not merely a UI preference.
$adminCustomersPath = Join-Path $B "Controllers\AdminCustomersController.cs"
$adminCustomers = Get-Content $adminCustomersPath -Raw
$customerMethodAnchor = '    {' + "`r`n" + '        var adminUserId = GetAuthenticatedUserId();'
$customerMethodInsert = '    {' + "`r`n" + '        var accountPolicy = await _db.AccountRegistrationPolicies.FirstOrDefaultAsync(x => x.Id == 1);' + "`r`n" +
'        if (accountPolicy == null || !accountPolicy.AdminAccountCreationEnabled)' + "`r`n" +
'            return StatusCode(403, new { message = "Administrator account creation is disabled by policy." });' + "`r`n" +
'        var adminUserId = GetAuthenticatedUserId();'
if ($adminCustomers -notmatch 'Administrator account creation is disabled by policy') {
    if (!$adminCustomers.Contains($customerMethodAnchor)) { throw "Could not patch AdminCustomersController safely." }
    $adminCustomers = $adminCustomers.Replace($customerMethodAnchor, $customerMethodInsert)
    Set-Content $adminCustomersPath $adminCustomers -Encoding UTF8
}

$adminRecipientsPath = Join-Path $B "Controllers\AdminRecipientsController.cs"
$adminRecipients = Get-Content $adminRecipientsPath -Raw
$recipientMethodAnchor = '    {' + "`r`n" + '        if (!await HasPermission("users.create") ||'
$recipientMethodInsert = '    {' + "`r`n" +
'        var accountPolicy = await _db.AccountRegistrationPolicies.FirstOrDefaultAsync(x => x.Id == 1);' + "`r`n" +
'        if (accountPolicy == null || !accountPolicy.AdminAccountCreationEnabled)' + "`r`n" +
'            return StatusCode(403, new { message = "Administrator account creation is disabled by policy." });' + "`r`n" +
'        if (!await HasPermission("users.create") ||'
if ($adminRecipients -notmatch 'Administrator account creation is disabled by policy') {
    if (!$adminRecipients.Contains($recipientMethodAnchor)) { throw "Could not patch AdminRecipientsController safely." }
    $adminRecipients = $adminRecipients.Replace($recipientMethodAnchor, $recipientMethodInsert)
    Set-Content $adminRecipientsPath $adminRecipients -Encoding UTF8
}

$adminUsersPath = Join-Path $B "Controllers\AdminUsersController.cs"
$adminUsers = Get-Content $adminUsersPath -Raw
$dispatcherMethodAnchor = '    {' + "`r`n" + '        if (!await HasPermission("users.create") ||'
$dispatcherMethodInsert = '    {' + "`r`n" +
'        var accountPolicy = await _db.AccountRegistrationPolicies.FirstOrDefaultAsync(x => x.Id == 1);' + "`r`n" +
'        if (accountPolicy == null || !accountPolicy.AdminAccountCreationEnabled)' + "`r`n" +
'            return StatusCode(403, new { message = "Administrator account creation is disabled by policy." });' + "`r`n" +
'        if (!await HasPermission("users.create") ||'
if ($adminUsers -notmatch 'Administrator account creation is disabled by policy') {
    if (!$adminUsers.Contains($dispatcherMethodAnchor)) { throw "Could not patch AdminUsersController safely." }
    $adminUsers = $adminUsers.Replace($dispatcherMethodAnchor, $dispatcherMethodInsert)
    Set-Content $adminUsersPath $adminUsers -Encoding UTF8
}

# Frontend service
Put (Join-Path $F "src\services\registrationService.js") @'
import { apiRequest } from "./apiClient";
export const registrationService = {
    publicPolicy: () => apiRequest("/api/auth/registration-policy"),
    register: payload => apiRequest("/api/auth/register", { method: "POST", body: JSON.stringify(payload) }),
    adminPolicy: () => apiRequest("/api/admin/settings/registration"),
    savePolicy: payload => apiRequest("/api/admin/settings/registration", { method: "PUT", body: JSON.stringify(payload) }),
    pending: () => apiRequest("/api/admin/users/pending"),
    approve: id => apiRequest(`/api/admin/users/${id}/approve`, { method: "POST" })
};
'@

Put (Join-Path $F "src\pages\SignupPage.jsx") @'
import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { registrationService } from "../services/registrationService";
import "../styles/auth.css";
export default function SignupPage() {
    const [types, setTypes] = useState([]);
    const [type, setType] = useState("");
    const [form, setForm] = useState({ fullName:"", email:"", password:"", companyName:"", contactName:"", phone:"", address:"" });
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");
    useEffect(() => {
        registrationService.publicPolicy().then(p => {
            setTypes(p.accountTypes || []);
            setType(p.accountTypes?.[0] || "");
        }).catch(e => setError(e.message)).finally(() => setLoading(false));
    }, []);
    const update = (key, value) => setForm(v => ({ ...v, [key]:value }));
    async function submit(e) {
        e.preventDefault(); setError(""); setMessage(""); setBusy(true);
        try {
            const result = await registrationService.register({ ...form, accountType:type });
            setMessage(result.message);
        } catch (e) { setError(e.message || "Registration failed."); }
        finally { setBusy(false); }
    }
    return <main className="auth-page"><section className="auth-card">
        <Link to="/" className="auth-brand">Qubriq</Link><p className="auth-eyebrow">CREATE YOUR ACCOUNT</p>
        <h1>Join Qubriq</h1><p className="auth-description">Create your Qubriq account.</p>
        {loading && <p>Loading registration options...</p>}
        {!loading && types.length === 0 && <div className="auth-information">Public registration is unavailable. Contact your administrator.</div>}
        {!loading && types.length > 0 && !message && <form className="auth-form" onSubmit={submit}>
            <label>Account type<select value={type} onChange={e=>setType(e.target.value)}>{types.map(t=><option key={t} value={t}>{t==="BookingCustomer"?"Booking Customer":"Final Recipient"}</option>)}</select></label>
            <label>Full name<input required maxLength={150} value={form.fullName} onChange={e=>update("fullName",e.target.value)} /></label>
            <label>Email<input required type="email" maxLength={254} value={form.email} onChange={e=>update("email",e.target.value)} /></label>
            <label>Password<input required type="password" minLength={12} maxLength={128} value={form.password} onChange={e=>update("password",e.target.value)} /></label>
            {type==="BookingCustomer" && <>
                <label>Company name<input required maxLength={200} value={form.companyName} onChange={e=>update("companyName",e.target.value)} /></label>
                <label>Contact name<input maxLength={150} value={form.contactName} onChange={e=>update("contactName",e.target.value)} /></label>
                <label>Phone<input maxLength={30} value={form.phone} onChange={e=>update("phone",e.target.value)} /></label>
                <label>Address<textarea rows={3} maxLength={500} value={form.address} onChange={e=>update("address",e.target.value)} /></label>
            </>}
            {error && <div className="auth-error" role="alert">{error}</div>}
            <button disabled={busy}>{busy?"Creating account...":"Create account"}</button>
        </form>}
        {message && <div className="auth-information" role="status">{message}</div>}
        <p className="auth-footer">Already registered? <Link to="/login">Sign in</Link></p>
        <Link to="/" className="auth-back-link">Back to home</Link>
    </section></main>;
}
'@

Put (Join-Path $F "src\pages\AdminSettingsPage.jsx") @'
import { useEffect, useState } from "react";
import { registrationService } from "../services/registrationService";
const defaults = { publicRegistrationEnabled:true, bookingCustomerSignupEnabled:true, finalRecipientSignupEnabled:false, requireAdminApproval:false, activatePublicAccountsImmediately:true, adminAccountCreationEnabled:true };
const fields = [
 ["publicRegistrationEnabled","Allow public registration"],
 ["bookingCustomerSignupEnabled","Booking Customer signup"],
 ["finalRecipientSignupEnabled","Final Recipient signup"],
 ["requireAdminApproval","Require Admin approval"],
 ["activatePublicAccountsImmediately","Activate public accounts immediately"],
 ["adminAccountCreationEnabled","Allow Admin account creation"]
];
export default function AdminSettingsPage() {
 const [policy,setPolicy]=useState(defaults), [pending,setPending]=useState([]), [error,setError]=useState(""), [message,setMessage]=useState(""), [busy,setBusy]=useState(false);
 async function load() {
  try { const [p,u]=await Promise.all([registrationService.adminPolicy(),registrationService.pending()]); setPolicy(Object.fromEntries(fields.map(([k])=>[k,Boolean(p[k])]))); setPending(u||[]); }
  catch(e){setError(e.message||"Unable to load settings.");}
 }
 useEffect(()=>{load();},[]);
 async function save(e){e.preventDefault();setBusy(true);setError("");setMessage("");try{await registrationService.savePolicy(policy);setMessage("Registration policy saved.");await load();}catch(e){setError(e.message);}finally{setBusy(false);}}
 async function approve(id){setError("");try{await registrationService.approve(id);setMessage("Account approved.");await load();}catch(e){setError(e.message);}}
 return <section className="module-page">
  <div className="page-heading-row"><div><h1>Account & Registration Settings</h1><p>Manage public signup rules and pending account approvals.</p></div></div>
  <form className="dashboard-card" onSubmit={save}><h2>Registration policy</h2><p>Updates are stored in MySQL and apply to subsequent registration requests.</p>
   <div style={{display:"grid",gap:16,margin:"20px 0"}}>{fields.map(([key,label])=><label key={key} style={{display:"flex",gap:12,alignItems:"center"}}><input type="checkbox" checked={policy[key]} onChange={e=>setPolicy(p=>({...p,[key]:e.target.checked}))}/><strong>{label}</strong></label>)}</div>
   <button disabled={busy}>{busy?"Saving...":"Save settings"}</button>
  </form>
  <div className="dashboard-card" style={{marginTop:20}}><h2>Pending approvals</h2>
   {pending.length===0?<p>No inactive accounts found.</p>:<div style={{overflowX:"auto"}}><table style={{width:"100%"}}><thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Created</th><th></th></tr></thead><tbody>{pending.map(u=><tr key={u.id}><td>{u.fullName}</td><td>{u.email}</td><td>{(u.roles||[]).join(", ")}</td><td>{new Date(u.createdAtUtc).toLocaleString()}</td><td><button type="button" onClick={()=>approve(u.id)}>Approve</button></td></tr>)}</tbody></table></div>}
  </div>
  {message&&<p role="status">{message}</p>}{error&&<p role="alert" style={{color:"#b42318"}}>{error}</p>}
 </section>;
}
'@

# Replace Admin Settings placeholder route.
$routePath = Join-Path $F "src\routes\AppRoutes.jsx"
$route = Get-Content $routePath -Raw
if ($route -notmatch 'AdminSettingsPage') {
    $route = $route.Replace('import ModulePage from "../pages/ModulePage";',
        'import ModulePage from "../pages/ModulePage";' + "`r`n" + 'import AdminSettingsPage from "../pages/AdminSettingsPage";')
    $route = $route.Replace('<Route path="settings" element={<ModulePage title="Settings" description="Platform settings." />} />',
        '<Route path="settings" element={<AdminSettingsPage />} />')
    Set-Content $routePath $route -Encoding UTF8
}

Write-Host "`nSource files updated. Backup: $backup" -ForegroundColor Green
Write-Host "NO migration was generated or applied. NO database operation was performed." -ForegroundColor Yellow
Write-Host "`nNext: inspect diff, then build backend and frontend."
Write-Host "Migration generation is intentionally separate and not run by this script:"
Write-Host "  Set-Location '$B'; dotnet ef migrations add AddAccountRegistrationPolicies"
Write-Host "Database update is NOT authorized:"
Write-Host "  dotnet ef database update"
