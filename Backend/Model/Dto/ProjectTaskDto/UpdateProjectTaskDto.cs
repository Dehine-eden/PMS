using ProjectManagementSystem1.Model.Entities;
using System.ComponentModel.DataAnnotations;
using TaskStatus = ProjectManagementSystem1.Model.Entities.TaskStatus;

namespace ProjectManagementSystem1.Model.Dto.ProjectTaskDto
{
    public class UpdateProjectTaskDto
    {
        [Required]
        public Guid Id { get; set; }

        public Guid? ParentTaskId { get; set; }

        [MaxLength(250)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [Range(0, 100)]
        public decimal? Weight { get; set; }

        public TaskStatus? Status { get; set; }

        public AssignmentStatus? AssignmentStatus { get; set; }

        public DateTime? AssignmentUpdatedDate { get; set; }

        [MaxLength(1000)]
        public string? RejectionReason { get; set; }

        public PriorityLevel? Priority { get; set; }

        public double? EstimatedHours { get; set; }

        public double? ActualHours { get; set; }

        //[Required]
        public string? EmployeeId { get; set; }
        public int? Depth { get; set; }

        public bool? IsLeaf { get; set; }

        public decimal? Progress { get; set; }
    }

}
