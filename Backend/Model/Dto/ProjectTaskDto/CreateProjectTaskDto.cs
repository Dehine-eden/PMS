using System.ComponentModel.DataAnnotations;
using System;
using ProjectManagementSystem1.Model.Enums;

namespace ProjectManagementSystem1.Model.Dtos
{
public class CreateProjectTaskDto
{
    public int? ProjectId { get; set; }
    public int? MemberId { get; set; }
    public double? Weight { get; set; }
    [Required]
    [StringLength(100, ErrorMessage = "Task title can't be longer than 100 characters.")]
    public required string Title { get; set; }

    [StringLength(1000, ErrorMessage = "Task description can't be longer than 1000 characters.")]
        public string? Description { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }
        
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Assigned user ID must be valid.")]
    public int AssignedUserId { get; set; }

    [Required]
    [EnumDataType(typeof(ProjectTaskPriority))]
    public ProjectTaskPriority Priority { get; set; } = ProjectTaskPriority.Medium;

    [EnumDataType(typeof(ProjectTaskStatus))]
    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.ToDo;
}
}
// This class is used to create a new project in the system. It contains properties for the project name, owner, priority, due date, and status. The properties are used to capture the necessary information when creating a new project. The class is typically used in conjunction with a controller action that handles the creation of a new project in the database.
