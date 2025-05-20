namespace ProjectManagementSystem1.Model.Dto.ProjectTaskDto
{
    public class ProjectTaskDto
    {
        public Guid Id { get; set; }

        public int ProjectAssignmentId { get; set; }

        public string? EmployeeId { get; set; }

        public Guid? ParentTaskId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public decimal Weight { get; set; }

        public string Status { get; set; } = string.Empty; // Can map from enum

        public string AssignmentStatus { get; set; } = string.Empty; // Map from enum

        public DateTime? AssignmentUpdatedDate { get; set; }

        public string? RejectionReason { get; set; }

        public string Priority { get; set; } = string.Empty; // Map from enum

        public double? EstimatedHours { get; set; }

        public double? ActualHours { get; set; }

        public int Depth { get; set; }

        public bool IsLeaf { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }

        public decimal Progress { get; set; }

        public ICollection<ProjectTaskDto>? SubTasks { get; set; } // For nested subtasks
    }

}
