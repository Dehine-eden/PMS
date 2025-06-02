using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.Message;
using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Services
{
    public class MessageService : IMessageService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public MessageService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<MessageDto> SendMessageAsync(CreateMessageDto dto, string senderUserId)
        {
            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderUserId);
            if (sender == null)
                throw new Exception("Sender not found.");

            // Validation
            if (dto.MessageType < 1 || dto.MessageType > 3)
                throw new ArgumentException("MessageType must be 1, 2, or 3.");

            if (dto.MessageType == 1 && dto.ProjectId == null)
                throw new ArgumentException("ProjectId is required for project messages.");
            if (dto.MessageType == 3 && string.IsNullOrWhiteSpace(dto.ReceiverId))
                throw new ArgumentException("ReceiverId is required for personal messages.");
            if (dto.MessageType == 2)
            {
                dto.ProjectId = null;
                dto.ReceiverId = null;
            }

            var message = new Message
            {
                Content = dto.Content,
                SenderId = sender.Id, // keep user Id
                ReceiverId = dto.ReceiverId,
                ProjectId = dto.ProjectId,
                MessageType = dto.MessageType,
                AttachmentId = dto.AttachmentId,
                TimeSent = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            message = await _context.Messages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.MessageId == message.MessageId);

            return _mapper.Map<MessageDto>(message);
        }


        public async Task<List<MessageDto>> GetProjectMessagesAsync(int projectId)
        {
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ProjectId == projectId && m.MessageType == 1 && !m.IsDeleted)
                .OrderBy(m => m.TimeSent)
                .ToListAsync();

            return _mapper.Map<List<MessageDto>>(messages);
        }

        public async Task<List<MessageDto>> GetDepartmentMessagesAsync(string department)
        {
            var senderIds = await _context.Users
                .Where(u => u.Department == department)
                .Select(u => u.Id)
                .ToListAsync();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.MessageType == 2 && senderIds.Contains(m.SenderId) && !m.IsDeleted)
                .OrderBy(m => m.TimeSent)
                .ToListAsync();

            return _mapper.Map<List<MessageDto>>(messages);
        }

        public async Task<List<MessageDto>> GetPersonalMessagesAsync(string userId)
        {
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m =>
                    m.MessageType == 3 &&
                    !m.IsDeleted &&
                    (m.SenderId == userId || m.ReceiverId == userId))
                .OrderBy(m => m.TimeSent)
                .ToListAsync();

            return _mapper.Map<List<MessageDto>>(messages);
        }



        public async Task<List<MessageDto>> GetProjectMessagesForUserAsync(string userId)
        {
            // Get all project IDs where the user is a member
            var projectIds = await _context.ProjectAssignments
                .Where(pa => pa.MemberId == userId)
                .Select(pa => pa.ProjectId)
                .ToListAsync();

            // Get messages only from those projects
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m =>
                    m.MessageType == 1 &&
                    m.ProjectId.HasValue &&
                    projectIds.Contains(m.ProjectId.Value) &&
                    !m.IsDeleted)
                .OrderBy(m => m.TimeSent)
                .ToListAsync();

            return _mapper.Map<List<MessageDto>>(messages);
        }

    }

}
