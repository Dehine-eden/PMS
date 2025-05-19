using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectManagementSystem1.Model.Enums;

namespace ProjectManagementSystem1.Model.Entities
{
    public class ProjectTask
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Project")]
        public int ProjectId { get; set; }

        [ForeignKey("User")]
        public int MemberId { get; set; }

        [Required]
        public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.ToDo;
        [Required]
        public ProjectTaskPriority Priority { get; set; } = ProjectTaskPriority.Medium;

        public required string Title { get; set; }

        [MaxLength(1000)]
        public required string Descriptions { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(0, 100)]
        public double? Weight { get; set; } // percentage

        // Navigation Properties
        public Project? Project { get; set; }
        public User? Member { get; set; }

        // Common audit fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public required string AssignUser { get; set; }
        public required string ReAssignUser { get; set; }

        [Timestamp]
        public byte[]? Version { get; set; }
    }
}
