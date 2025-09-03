using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Entities
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // ID of the user who performed the action

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(100)]
        public string EntityType { get; set; } // e.g., "ProjectTask", "TodoItem"

        public int EntityId { get; set; } // ID of the specific entity

        [Required, MaxLength(255)]
        public string ActionType { get; set; } // e.g., "Created", "Updated", "Deleted", "Assigned"

        [MaxLength(2000)]
        public string Details { get; set; } // Optional details about the change (e.g., properties changed)
    
        // New properties for tracking changes
        [MaxLength(4000)]
        public string OldValues { get; set; } // JSON representation of old values

        [MaxLength(4000)]
        public string NewValues { get; set; } // JSON representation of new values

        [MaxLength(50)]
        public string LogType { get; set; } // e.g., "UserAccess", "System", "Audit"
    }
}