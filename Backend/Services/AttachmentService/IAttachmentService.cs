using ProjectManagementSystem1.Model.Dto.Attachments;

namespace ProjectManagementSystem1.Services.AttachmentService
{
    public interface IAttachmentService
    {
        Task<Attachment> UploadAttachmentAsync(AttachmentUploadDto uploadDto, string uploadedByUserId);
        Task<Attachment> GetAttachmentByIdAsync(Guid id);
        Task<IEnumerable<Attachment>> GetAttachmentByEntityAsync(string entityType, Guid entityId);
        Task SoftDeleteAttachmentAsync(Guid id);
    }
}
