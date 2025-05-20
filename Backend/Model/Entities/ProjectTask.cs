using ProjectManagementSystem1.Model.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Model.Entities
{
    public enum TaskStatus { New, InProgress, Blocked, Completed }
    public enum AssignmentStatus { Pending, Accepted, Rejected }
    public enum PriorityLevel { Low = 0, Medium = 1, High = 2, Critical = 3 }

    public class ProjectTask
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int ProjectAssignmentId { get; set; }

        [ForeignKey("ProjectAssignmentId")]
        public ProjectAssignment ProjectAssignment { get; set; }  // <-- navigation property

        [ForeignKey("ParentTask")]
        public Guid? ParentTaskId { get; set; }

        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [CustomValidation(typeof(DueDateValidator), nameof(DueDateValidator.ValidateDueDate))]
        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(5,2)"), Range(0, 100)]
        public decimal Weight { get; set; } = 0;

        //[Required]
        public TaskStatus Status { get; set; } = TaskStatus.New;

        public string? AssignedMemberId { get; set; } // Changed from int? to string?

        [ForeignKey("AssignedMemberId")]
        public ApplicationUser? Assignee { get; set; }

        [Required]
        [CustomValidation(typeof(TaskValidator), nameof(TaskValidator.ValidateAssignmentState))]
        public AssignmentStatus AssignmentStatus { get; set; } = AssignmentStatus.Pending;

        public DateTime? AssignmentUpdatedDate { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        [Required, Range(0, 3)]
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        public double? EstimatedHours { get; set; }
        public double? ActualHours { get; set; }

        public int Depth { get; set; } = 0;
        public bool IsLeaf { get; set; } = true;

        //[DatabaseGenerated(DatabaseGeneratedOption.None)]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        //[DatabaseGenerated(DatabaseGeneratedOption.None)]
        public DateTime UpdatedDate { get; set; }

        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        [Timestamp]
        public byte[]? Version { get; set; }

        // Navigation
        public Project? Project { get; set; }

        public bool IsAssignedBySupervisor { get; set; } = false;
        public ProjectTask? ParentTask { get; set; }
        public ICollection<ProjectTask> SubTasks { get; set; } = new List<ProjectTask>();
        //public decimal Progress => SubTasks.Any()
        //? SubTasks.Sum(t => t.Progress * t.Weight / 100)
        //: (Status == TaskStatus.Completed ? 100 : 0);
        [Column(TypeName = "decimal(5,2)")]
        public decimal Progress { get; set; }

    }

    public static class TaskValidator
    {
        public static ValidationResult? ValidateAssignmentState(AssignmentStatus status, ValidationContext context)
        {
            var task = (ProjectTask)context.ObjectInstance;

            if (status == AssignmentStatus.Rejected && string.IsNullOrWhiteSpace(task.RejectionReason))
            {
                return new ValidationResult("Rejection reason is required when a task is rejected.");
            }

            if ((status == AssignmentStatus.Accepted || status == AssignmentStatus.Rejected) && task.AssignedMemberId == null)
            {
                return new ValidationResult("Cannot accept or reject a task without an assignee.");
            }

            if (task.ParentTaskId.HasValue)
            {
                // Just compare ProjectAssignmentId values (both non-nullable ints)
                if (task.ParentTask?.ProjectAssignmentId != task.ProjectAssignmentId)
                {
                    return new ValidationResult("Parent task must belong to the same project assignment.");
                }
            }


            return ValidationResult.Success;
        }

    }


}

    public class DueDateValidator
    {
    public static ValidationResult ValidateDueDate(DateTime dueDate, ValidationContext ctx)
    {
        var task = (ProjectTask)ctx.ObjectInstance;

        if (task.Priority != PriorityLevel.Critical && dueDate < DateTime.UtcNow.Date.AddDays(7))
            return new ValidationResult("Due date must be at least 7 days from now for non-critical tasks.");

        return ValidationResult.Success!;
    }


}

