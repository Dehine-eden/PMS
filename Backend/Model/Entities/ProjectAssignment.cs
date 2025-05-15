using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectManagementSystem1.Model.Entities;

public class ProjectAssignment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey("ProjectId")]
    public Project Project { get; set; }

    [Required]
    public string MemberId { get; set; } // FK to ApplicationUser

    [ForeignKey("MemberId")]
    public ApplicationUser Member { get; set; }

    [Required]
    public string MemberRole { get; set; } // Supervisor or User

    public double Status { get; set; } = 0; // Calculated from sub-tasks

    // Audit fields
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public string? CreateUser { get; set; }
    public string? UpdateUser { get; set; }

    [Timestamp]
    public byte[] Version { get; set; }
}
