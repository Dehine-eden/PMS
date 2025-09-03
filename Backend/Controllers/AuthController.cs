using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.UserManagementDto;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Services.AuthService;
using ProjectManagementSystem1.Services.UserService;
using System.Security.Claims;

namespace ProjectManagementSystem1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;
       

        public AuthController(IUserService userService, IAuthService authService, UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser> signInManager, ILogger<AuthController> logger)
        {
            _userService = userService;
            _authService = authService;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;

        }

        //[HttpPost("register")]
        //[Authorize(Policy = "AdminOnly")] // Optional: only admins can register users
        //public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        //{
        //    var result = await _userService.RegisterUserAsync(dto);

        //    if (!result.Success)
        //    {
        //        return BadRequest(new { errors = result.Errors });
        //    }

        //    return Ok(new { message = "User registered successfully." });
        //}

        [HttpPost("login")]
    [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var jwtResponse = await _authService.LoginAsync(dto);
        
                if (jwtResponse == null)
                {
                // AuthService already logged the failure, but we can add additional logging if needed
                // For example, we could log the IP address and user agent at the controller level
                    await LogFailedLoginAttempt(dto.Username, "Invalid credentials");
                    return Unauthorized(new { message = "Invalid credentials." });
                }

            // Log successful login
                await LogSuccessfulLogin(jwtResponse.UserName, "Login successful");
        
                if (jwtResponse.IsFirstLogin)
                return Ok(new { message = "First login. Password change required.", jwtResponse });

                return Ok(jwtResponse);
            }
            catch (Exception ex)
            {
            // Log any unexpected errors during login
            await LogFailedLoginAttempt(dto.Username, $"Login error: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred during login." });
            }
        }

        // Helper methods for logging
        private async Task LogSuccessfulLogin(string username, string details)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(username);
                if (user != null)
                {
                    await _authService.LogSuccessfulLogin(user.Id, username);
                }
            }
            catch (Exception ex)
            {
            // Don't let logging errors break the login flow
                _logger.LogError(ex, "Failed to log successful login for user {Username}", username);
            }
        }

        private async Task LogFailedLoginAttempt(string username, string reason)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(username);
                string userId = user?.Id ?? "unknown";
        
                await _authService.LogFailedLoginAttempt(userId, username, reason);
            }
            catch (Exception ex)
            {
            // Don't let logging errors break the login flow
                _logger.LogError(ex, "Failed to log failed login attempt for username {Username}", username);
            }
        }

        // Helper methods to get client information
        private string GetClientIpAddress()
        {
            return HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private string GetUserAgent()
        {
            return HttpContext?.Request?.Headers["User-Agent"].ToString();
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var response = await _authService.RefreshTokenAsync(request.Token);
            if (response == null) return Unauthorized(new { message = "Invalid or expired refresh token." });

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
        {
    // Same implementation as above, but you can use request.DeviceId in your logging
        var userId = _userManager.GetUserId(User) ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Invalid user or session." });
    
    // Get user details for logging
        var user = await _userManager.FindByIdAsync(userId);
        var username = user?.UserName ?? "Unknown";
    
    // Call the auth service to handle token revocation
        await _authService.LogoutAsync(userId);
    
    // Also sign out from any cookie authentication
        await _signInManager.SignOutAsync();
    
    // Log the logout action with additional details
        await _authService.LogUserAccessAsync(
            userId: userId,
            actionType: "Logout",
            details: $"User {username} logged out successfully. Device: {request?.DeviceId ?? "Unknown"}",
            ipAddress: GetClientIpAddress(),
            userAgent: GetUserAgent()
        );

        return Ok(new { message = "Logged out successfully." });
        }
    }
}
