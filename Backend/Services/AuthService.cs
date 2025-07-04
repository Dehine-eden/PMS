using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.UserManagementDto;
using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Services
{
    public class AuthService : IAuthService
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext context;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;

        public AuthService(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IConfiguration _configuration, IJwtService jwtService, AppDbContext _context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _jwtService = jwtService;
            context = _context;
        }

        public async Task<TokenResponseDto?> LoginAsync(LoginRequestDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.Username);
            if (user == null) return null;

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded) return null;

            var accessToken = await _jwtService.GenerateJwtTokenAsync(user);
            var refreshToken = GenerateRefreshToken(user);

            context.RefreshTokens.Add(refreshToken);
            await context.SaveChangesAsync();

            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
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

            var existing = await context.RefreshTokens
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

            context.RefreshTokens.Add(newRefreshToken);
            await context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token
            };
        }
        public async Task LogoutAsync(string userId)
        {
            var tokens = context.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked && !r.IsUsed);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await context.SaveChangesAsync();
        }

    }
}
