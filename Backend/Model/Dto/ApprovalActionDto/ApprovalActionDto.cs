using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto
{
    public class ApprovalActionDto
    {
        [Required]
        public string Action { get; set; } // "approve" or "reject"

        public string Comments { get; set; }
    }
}