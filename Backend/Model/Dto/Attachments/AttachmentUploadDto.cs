using System.ComponentModel.DataAnnotations;

namespace ProjectManagementSystem1.Model.Dto.Attachments
{
    public class AttachmentUploadDto
    {
        [Required]
        public IFormFile File { get; set; }

        [Required]
        public AttachmentCategory Category { get; set; }

        [Required]
        public Guid EntityId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; }

        public int? ProjectTaskId { get; set; }
    }
}
