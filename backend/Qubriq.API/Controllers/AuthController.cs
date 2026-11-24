using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qubriq.API.DTOs.Auth;
using Qubriq.API.Services;

namespace Qubriq.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result == null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password, or the account is inactive."
            });
        }

        return Ok(result);
    }
}
