using ProjectManagementSystem1.Model.Dto.UserManagementDto;
using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Services.AuthService
{
    public interface IAuthService
    {
        Task<TokenResponseDto?> LoginAsync(LoginRequestDto loginDto);
        Task<TokenResponseDto?> RefreshTokenAsync(string token);

        Task LogoutAsync(string userId);

        // New methods for access logging
    Task LogFailedLoginAttempt(string userId, string username, string reason);
    Task LogSuccessfulLogin(string userId, string username);
    Task LogUserAccessAsync(string userId, string actionType, string details, string ipAddress, string userAgent);
    Task LogPasswordChange(string userId, bool success, string details = null);
    Task LogAccountLockout(string userId, string reason);
    Task LogAccountUnlock(string userId, string initiatedBy);
    }
}
