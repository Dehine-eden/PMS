using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.Attachments;

namespace ProjectManagementSystem1.Services.AttachmentService
{
    public class AttachmentService : IAttachmentService
    {
        private readonly AppDbContext _context;

        public AttachmentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Attachment> UploadAttachmentAsync(AttachmentUploadDto uploadDto, string uploadedByUserId)
        {
            var file = uploadDto.File;
            if (file == null || file.Length == 0)
            {
                throw new Exception("No file uploaded.");
            }

            var uploadFolderPath = @"C:\ProjectManagementSystem\Attachments"; // Use your desired local path
            if (!Directory.Exists(uploadFolderPath))
            {
                Directory.CreateDirectory(uploadFolderPath);
            }

            var fileName = Path.GetFileName(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}-{fileName}"; // Ensure unique filenames
            var filePath = Path.Combine(uploadFolderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                FileName = fileName,
                ContentType = file.ContentType,
                Category = uploadDto.Category,
                FileSize = file.Length,
                FilePhysicalPath = filePath,
                UploadedByUserId = uploadedByUserId,
                EntityId = uploadDto.EntityId,
                EntityType = uploadDto.EntityType,
                ProjectTaskId = uploadDto.ProjectTaskId,
                Checksum = CalculateChecksum(filePath) // Implement this method
            };

            await _context.Attachments.AddAsync(attachment);
            await _context.SaveChangesAsync();

            return attachment;

        }

        private string CalculateChecksum(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] hashbytes = sha256.ComputeHash(stream);

            return BitConverter.ToString(hashbytes).Replace("-", "").ToLowerInvariant();
        }

        public async Task<Attachment> GetAttachmentByIdAsync(Guid id)
        {

            return await _context.Attachments.FindAsync(id);
        }

        public async Task<IEnumerable<Attachment>> GetAttachmentByEntityAsync(string entityType, Guid entityId)
        {
            return await _context.Attachments
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .ToListAsync();
        }

        public async Task SoftDeleteAttachmentAsync(Guid id)
        {
            var attachment = await _context.Attachments.FindAsync(id);
            if (attachment != null && !attachment.IsDeleted)
            {
                attachment.IsDeleted = true;
                attachment.UpdatedAt = DateTime.UtcNow;
                _context.Attachments.Update(attachment);
                await _context.SaveChangesAsync();
            }
        }
    }
}
