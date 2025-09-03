using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace ProjectManagementSystem1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ActivityLogController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ActivityLogController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActivityLog>>> GetActivityLog(
            [FromQuery] string entityType = null,
            [FromQuery] int? entityId = null,
            [FromQuery] string actionType = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            IQueryable<ActivityLog> query = _context.ActivityLogs.OrderByDescending(log => log.Timestamp);

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(log => log.EntityType.ToLower() == entityType.ToLower());
            }

            if (entityId.HasValue)
            {
                query = query.Where(log => log.EntityId == entityId);
            }

            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(log => log.ActionType.ToLower() == actionType.ToLower());
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Deserialize the JSON values for the response
            var result = logs.Select(log => new
            {
                log.Id,
                log.UserId,
                log.Timestamp,
                log.EntityType,
                log.EntityId,
                log.ActionType,
                log.Details,
                OldValues = !string.IsNullOrEmpty(log.OldValues) ? 
                    JsonSerializer.Deserialize<object>(log.OldValues) : null,
                NewValues = !string.IsNullOrEmpty(log.NewValues) ? 
                    JsonSerializer.Deserialize<object>(log.NewValues) : null
            });

            return Ok(new
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Logs = logs
            });
        }

        [HttpGet("{entityType}/{entityId}")]
        public async Task<ActionResult<IEnumerable<ActivityLog>>> GetEntityHistoryAsync(string entityType, int entityId)
        {
            if (string.IsNullOrEmpty(entityType))
            {
                return BadRequest("EntityType is required.");
            }

            var entityLogs = await _context.ActivityLogs
                .Where(log => log.EntityType.ToLower() == entityType.ToLower() && log.EntityId == entityId)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            // Deserialize the JSON values for the response
            var result = entityLogs.Select(log => new
            {
                log.Id,
                log.UserId,
                log.Timestamp,
                log.EntityType,
                log.EntityId,
                log.ActionType,
                log.Details,
                OldValues = !string.IsNullOrEmpty(log.OldValues) ? 
                    JsonSerializer.Deserialize<object>(log.OldValues) : null,
                NewValues = !string.IsNullOrEmpty(log.NewValues) ? 
                    JsonSerializer.Deserialize<object>(log.NewValues) : null
            });

            return Ok(entityLogs);
        }

        // Add a new endpoint to get detailed change history for a specific entity
        [HttpGet("changes/{entityType}/{entityId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetEntityChangeHistoryAsync(string entityType, int entityId)
        {
            if (string.IsNullOrEmpty(entityType))
            {
                return BadRequest("EntityType is required.");
            }

            var changeLogs = await _context.ActivityLogs
                .Where(log => log.EntityType.ToLower() == entityType.ToLower() && 
                             log.EntityId == entityId &&
                             (log.OldValues != null || log.NewValues != null))
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            var result = changeLogs.Select(log => new
            {
                log.Id,
                log.UserId,
                log.Timestamp,
                log.ActionType,
                Changes = GetChangesDescription(log.OldValues, log.NewValues)
            });

            return Ok(result);
        }

        private object GetChangesDescription(string oldValuesJson, string newValuesJson)
        {
            if (string.IsNullOrEmpty(oldValuesJson) && string.IsNullOrEmpty(newValuesJson))
                return null;

            try
            {
                var oldValues = !string.IsNullOrEmpty(oldValuesJson) ? 
                    JsonSerializer.Deserialize<Dictionary<string, object>>(oldValuesJson) : 
                    new Dictionary<string, object>();
                
                var newValues = !string.IsNullOrEmpty(newValuesJson) ? 
                    JsonSerializer.Deserialize<Dictionary<string, object>>(newValuesJson) : 
                    new Dictionary<string, object>();

                var changes = new List<object>();

                // Check for removed properties
                foreach (var key in oldValues.Keys.Except(newValues.Keys))
                {
                    changes.Add(new
                    {
                        Property = key,
                        Change = "Removed",
                        OldValue = oldValues[key],
                        NewValue = (object)null
                    });
                }

                // Check for added properties
                foreach (var key in newValues.Keys.Except(oldValues.Keys))
                {
                    changes.Add(new
                    {
                        Property = key,
                        Change = "Added",
                        OldValue = (object)null,
                        NewValue = newValues[key]
                    });
                }

                // Check for changed properties
                foreach (var key in oldValues.Keys.Intersect(newValues.Keys))
                {
                    if (!Equals(oldValues[key], newValues[key]))
                    {
                        changes.Add(new
                        {
                            Property = key,
                            Change = "Modified",
                            OldValue = oldValues[key],
                            NewValue = newValues[key]
                        });
                    }
                }

                return changes;
            }
            catch
            {
                return "Unable to parse change details";
            }
        }
    }
}