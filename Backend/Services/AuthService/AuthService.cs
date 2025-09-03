using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.UserManagementDto;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Services.JwtService;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ProjectManagementSystem1.Services;

namespace ProjectManagementSystem1.Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;
        private readonly IActivityLogService _activityLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(SignInManager<ApplicationUser> signInManager, 
            UserManager<ApplicationUser> userManager, 
            IConfiguration configuration, 
            IJwtService jwtService, 
            AppDbContext context,
            IActivityLogService activityLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _jwtService = jwtService;
            _context = context;
            _activityLogService = activityLogService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<TokenResponseDto?> LoginAsync(LoginRequestDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.Username);
            if (user == null) 
            {
                // Log failed login attempt (user not found)
                await LogFailedLoginAttempt(null, loginDto.Username, "User not found");
                return null;
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded) 
            {
                // Log failed login attempt (wrong password)
                await LogFailedLoginAttempt(user.Id, loginDto.Username, "Invalid password");
                return null;
            }

             // Check if user account is active
            if (user.Status != "Active" || user.IsArchived)
            {
                await LogFailedLoginAttempt(user.Id, loginDto.Username, "Account is inactive or archived");
                return null;
            }

            var accessToken = await _jwtService.GenerateJwtTokenAsync(user);
            var refreshToken = GenerateRefreshToken(user);

           _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Log successful login
            await LogSuccessfulLogin(user.Id, user.UserName);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                IsFirstLogin = user.IsFirstLogin,
                UserName = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                // Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
            };
        }

        private RefreshToken GenerateRefreshToken(ApplicationUser user)
        {
            return new RefreshToken
            {
                Token = Guid.NewGuid().ToString(), // You can use JWT or secure RNG if preferred
                UserId = user.Id,
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7)
            };
        }

        public async Task<TokenResponseDto?> RefreshTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length < 15)
                return null;

            var existing = await _context.RefreshTokens
                .Include(r => r.User)
                .SingleOrDefaultAsync(r => r.Token == token);

            if (existing == null || existing.IsRevoked || existing.IsUsed || existing.Expires < DateTime.UtcNow)
                return null;

            // Mark old token as used
            existing.IsUsed = true;
            existing.IsRevoked = true;

            // Generate new tokens
            var accessToken = await _jwtService.GenerateJwtTokenAsync(existing.User);
            var newRefreshToken = GenerateRefreshToken(existing.User);

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            // Log token refresh
            await _activityLogService.LogUserAccessAsync(
                userId: existing.UserId,
                actionType: "TokenRefreshed",
                details: "Refresh token used to obtain new access token",
                ipAddress: GetClientIpAddress(),
                userAgent: GetUserAgent()
            );

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                IsFirstLogin = existing.User.IsFirstLogin
            };
        }
        public async Task LogoutAsync(string userId)
        {
            var tokens = _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked && !r.IsUsed);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();

            // Log logout
            await _activityLogService.LogUserAccessAsync(
                userId: userId,
                actionType: "Logout",
                details: "User logged out successfully",
                ipAddress: GetClientIpAddress(),
                userAgent: GetUserAgent()
            );
        }

            public async Task LogFailedLoginAttempt(string userId, string username, string reason)
        {
            await _activityLogService.LogUserAccessAsync(
                userId: userId,
                actionType: "LoginFailed",
                details: $"Failed login attempt for username: {username}. Reason: {reason}",
                ipAddress: GetClientIpAddress(),
                userAgent: GetUserAgent()
            );
        }

        public async Task LogSuccessfulLogin(string userId, string username)
        {
            await _activityLogService.LogUserAccessAsync(
                userId: userId,
                actionType: "LoginSuccess",
                details: $"Successful login for username: {username}",
                ipAddress: GetClientIpAddress(),
                userAgent: GetUserAgent()
            );
        }

        private string GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private string GetUserAgent()
        {
            return _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
        }

        // Additional method to track password change events
        public async Task LogPasswordChange(string userId, bool success, string details = null)
        {
            await _activityLogService.LogUserAccessAsync(
                userId: userId,
                actionType: success ? "PasswordChangeSuccess" : "PasswordChangeFailed",
                details: details ?? (success ? "Password changed successfully" : "Password change failed"),
                ipAddress: GetClientIpAddress(),
                userAgent: GetUserAgent()
            );
        }

        // Method to track account lockout events
        public async Task LogAccountLockout(string userId, string reason)
        {
            await _activityLogService.LogUserAccessAsync(
                userId: userId,
                actionType: "AccountLocked",
                details: $"Account locked. Reason: {reason}",
                ipAddress: GetClientIpAddress(),
                userAgent: GetUserAgent()
            );
        }

        // Method to track account unlock events
        public async Task LogAccountUnlock(string userId, string initiatedBy)
        {
            await _activityLogService.LogUserAccessAsync(
                userId: userId,
                actionType: "AccountUnlocked",
                details: $"Account unlocked by: {initiatedBy}",
                ipAddress: GetClientIpAddress(),
                userAgent: GetUserAgent()
            );
        }

        public async Task LogUserAccessAsync(string userId, string actionType, string details, string ipAddress, string userAgent)
        {
            await _activityLogService.LogUserAccessAsync(userId, actionType, details, ipAddress, userAgent);
        }
    }
}
