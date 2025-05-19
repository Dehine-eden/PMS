using ProjectManagementSystem1.Model.Enums;

namespace ProjectManagementSystem1.Model.Dtos
{
    public class UpdateProjectTaskDto
    {
        internal readonly int Id;
        public ProjectTaskStatus LastUsedStatus { get; set; }
        public required string Descriptions { get; set; }
        public ProjectTaskPriority LastUsedPriority { get; set;}
        public required string Title { get; set; }
        public DateTime? DueDate { get; set; }
        public double? Weight { get; set; }
        public required string UpdateUser { get; set; }
          public byte[]? Version { get; set; } 
    }
}
