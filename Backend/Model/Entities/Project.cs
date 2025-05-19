using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ProjectName { get; set; }

    public string ProjectOwner { get; set; }
    public string ProjectOwnerPhone { get; set; }
    public string ProjectOwnerEmail { get; set; }

    public ProjectPriority Priority { get; set; } = ProjectPriority.Medium;
    public DateTime? DueDate { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.ToDo;

    // Audit fields
    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public string? CreateUser { get; set; }
    public string? UpdateUser { get; set; }

    [Timestamp]
    public byte[] Version { get; set; }
}
