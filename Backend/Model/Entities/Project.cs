﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectManagementSystem1.Model.Entities;

public class Project
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ProjectName { get; set; }

    public string? Description { get; set; }

    public string Department {  get; set; } // assigned when project is created (logged-in manager department)

    public string ProjectOwner { get; set; }

    [Phone]
    public string ProjectOwnerPhone { get; set; }

    [EmailAddress]
    public string ProjectOwnerEmail { get; set; }

    public string Priority { get; set; } 
    public DateTime? DueDate { get; set; }

    public string Status { get; set; }  // "Active", "On Hold", "Completed", "Archived"

    // Audit fields
    public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public string? CreateUser { get; set; }
    public string? UpdateUser { get; set; }

    //Approval fields
    public DateTime? ApprovalDate { get; set; }

    public int? ApprovedById { get; set; }

    [ForeignKey("ApprovedById")]
    public User ApprovedBy { get; set; }

    public string RejectionReason { get; set; }

    public List<ApprovalRequest> ApprovalRequests { get; set; }

    [Timestamp]
    public byte[] Version { get; set; }

    public bool IsAutomateTodo { get; set; } = true; // Default to true
    public bool IsArchived { get; set; } = false; // default to not archived
    public DateTime? ArchiveDate { get; internal set; }
    public ICollection<ProjectAssignment> ProjectAssignments { get; set; }
    public virtual ICollection<Issue> Issues { get; set; } = new List<Issue>(); // Navigation property
}
