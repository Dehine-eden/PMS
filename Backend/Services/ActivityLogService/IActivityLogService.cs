using ProjectManagementSystem1.Model.Entities;
using System.Threading.Tasks;

namespace ProjectManagementSystem1.Services
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(string userId, string entityType, int entityId, string actionType, string details = null);
        // New method for logging with old and new values
        Task LogActivityWithChangesAsync(
            string userId, 
            string entityType, 
            int entityId, 
            string actionType, 
            object oldValues = null, 
            object newValues = null, 
            string details = null);

        // New method for user access logging
        Task LogUserAccessAsync(
            string userId, 
            string actionType, 
            string details = null,
            string ipAddress = null,
            string userAgent = null);
    }
}