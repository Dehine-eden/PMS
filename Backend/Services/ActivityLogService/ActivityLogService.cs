using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Services;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using AutoMapper;

namespace ProjectManagementSystem1.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActivityAsync(string userId, string entityType, int entityId, string actionType, string details = null)
        {
            await LogActivityWithChangesAsync(userId, entityType, entityId, actionType, null, null, details);
        }

        public async Task LogUserAccessAsync(
            string userId, 
            string actionType, 
            string details = null,
            string ipAddress = null,
            string userAgent = null)
    {

        // Get IP address and user agent from current request if not provided
        ipAddress ??= _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        userAgent ??= _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
        var activityLog = new ActivityLog
        {
            UserId = userId,
            EntityType = "UserAccess", // Special entity type for access logs
            EntityId = 0, // Not applicable for access logs
            ActionType = actionType,
            Details = details,
            Timestamp = DateTime.UtcNow,
            LogType = "UserAccess", // Mark as access log
        // Store additional access info
            OldValues = JsonSerializer.Serialize(new {
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = DateTime.UtcNow
        }),
        NewValues = "{}" //Provide a default value instead of NULL
    };

    _context.ActivityLogs.Add(activityLog);
    await _context.SaveChangesAsync();
    }

        public async Task LogActivityWithChangesAsync(
            string userId, 
            string entityType, 
            int entityId, 
            string actionType, 
            object oldValues = null, 
            object newValues = null, 
            string details = null)
        {
            var activityLog = new ActivityLog
            {
                UserId = userId,
                EntityType = entityType,
                EntityId = entityId,
                ActionType = actionType,
                Details = details,
                Timestamp = DateTime.UtcNow,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null
            };

            _context.ActivityLogs.Add(activityLog);
            await _context.SaveChangesAsync();
        }
    }
}