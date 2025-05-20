using ProjectManagementSystem1.Model.Entities;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.ProjectTaskDto
{
    public class CreateProjectTaskDto
    {
        [Required]
        public int ProjectId { get; set; }
        //public int ProjectId { get; set; }

        [Display(Name = "Optional. Omit or set to null for no parent task.")]
        public Guid? ParentTaskId { get; set; } 

        [Required, MaxLength(250)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Range(0, 100)]
        public decimal Weight { get; set; } = 0;

        //[Required]
        public Entities.TaskStatus Status { get; set; } = Entities.TaskStatus.New;

        [Required]
        public AssignmentStatus AssignmentStatus { get; set; } = AssignmentStatus.Pending;

        public DateTime? AssignmentUpdatedDate { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        [Required]
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        public double? EstimatedHours { get; set; }

        public double? ActualHours { get; set; }

        [Required]
        public string EmployeeId { get; set; }
        //public string AssignedMemberId { get; set; }
        //public bool IsAssignedBySupervisor { get; set; }

        public int Depth { get; set; } = 0;

        public bool IsLeaf { get; set; } = true;

        public decimal Progress { get; set; }
    }

}
