using Maroik.Common.DataAccess.Contracts;
using Maroik.WebAPI.Contracts;
using Maroik.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Maroik.WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AccountController(
    IUserRepository userRepository,
    IJwtAuthManager jwtAuthManager,
    IAccountRepository accountRepository)
    : ControllerBase
{
    //private readonly ILogger<AccountController> _logger;

    //public AccountController(ILogger<AccountController> logger, IUserService userService, IJwtAuthManager jwtAuthManager, IAccountRepository accountRepository)
    //_logger = logger;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        if (!await userRepository.IsValidUserCredentialsAsync(request.Email, request.Password))
        {
            return Unauthorized();
        }

        var tempAccount = await accountRepository.GetAccountByEmailAsync(request.Email);
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, tempAccount.Email),
            new Claim(ClaimTypes.Name, tempAccount.Nickname),
            new Claim(ClaimTypes.Role, tempAccount.Role)
        };

        jwtAuthManager.GenerateTokens(tempAccount.Nickname, claims, DateTime.UtcNow);
        //logger.LogInformation($"User [{tempAccount.Email}] logged in the system.");
        return Ok(new LoginResult());
    }

    [HttpGet("user")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        return Ok(new LoginResult());
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // optionally "revoke" JWT token on the server side --> add the current token to a blocklist
        // https://github.com/auth0/node-jsonwebtoken/issues/375

        string userName = User.Identity?.Name;
        jwtAuthManager.RemoveRefreshTokenByUserName(userName);
        //logger.LogInformation($"User [{userName}] logged out the system.");
        return Ok();
    }

    [HttpPost("refresh-token")]
    [Authorize]
    public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            //logger.LogInformation($"User [{userName}] is trying to refresh JWT token.");

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Unauthorized();
            }

            string accessToken = await HttpContext.GetTokenAsync("Bearer", "access_token");
            jwtAuthManager.Refresh(request.RefreshToken, accessToken, DateTime.UtcNow);
            //logger.LogInformation($"User [{userName}] has refreshed JWT token.");
            return Ok(new LoginResult());
        }
        catch (SecurityTokenException e)
        {
            return Unauthorized(e.Message); // return 401 so that the client side can redirect the user to the login page
        }
    }
}

public class LoginRequest(string email, string password)
{
    [Required]
    [DataType(DataType.EmailAddress)]
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    [JsonPropertyName("Email")]
    public string Email { get; set; } = email;

    [Required]
    [JsonPropertyName("Password")]
    public string Password { get; set; } = password;
}

public class LoginResult;

public class RefreshTokenRequest(string refreshToken)
{
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = refreshToken;
}

public class ImpersonationRequest
{
    [JsonPropertyName("username")]
    public string UserName { get; set; }
}