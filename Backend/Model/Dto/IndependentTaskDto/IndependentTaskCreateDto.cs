using ProjectManagementSystem1.Model.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.IndependentTaskDto
{
    public class IndependentTaskCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "NotStarted"; // Use enum

        public DateTime? ApprovalDate { get; set; }

        public string ApprovedBy { get; set; }
        
        public string RejectionReason { get; set; }

        [Range(0, 100)]
        public double Progress { get; set; }

        [Range(1, 100)]
        public double Weight { get; set; }
        [Required]
        public string AssignedToUserId { get; set; }
    }
}