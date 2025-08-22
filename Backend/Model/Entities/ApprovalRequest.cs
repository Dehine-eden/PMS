using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Model.Entities
    public class ApprovalRequest
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public Project Project { get; set; }

        [Required]
        public int IndependentTaskId { get; set; }

        [ForeignKey("IndependentTaskId")]
        public IndependentTask IndependentTask { get; set; }

        [Required]
        public int ManagerId { get; set; }

        [ForeignKey("ManagerId")]
        public User Manager { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        public DateTime? ResponseDate { get; set; }

        public string Status { get; set; } // "Pending", "Approved", "Rejected"

        public string Comments { get; set; }
    }
}