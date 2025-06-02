using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.Message;
using ProjectManagementSystem1.Services;
using System.Security.Claims;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/message")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public MessageController(IMessageService messageService, IUserService userService, AppDbContext context, IMapper mapper)
        {
            _messageService = messageService;
            _userService = userService;
            _context = context;
            _mapper = mapper;
        }

        // ------------------ DEPARTMENT MESSAGE ------------------

        [HttpPost("department")]
        public async Task<IActionResult> SendDepartmentMessage([FromBody] string content)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var dept = User.FindFirst("Department")?.Value;

            var dto = new CreateMessageDto
            {
                Content = content,
                MessageType = 2, // Department
                ReceiverId = null,
                ProjectId = null
            };

            var message = await _messageService.SendMessageAsync(dto, senderId);
            return Ok(message);
        }

        [HttpGet("department")]
        public async Task<IActionResult> GetDepartmentMessages()
        {
            var dept = User.FindFirst("Department")?.Value;
            var messages = await _messageService.GetDepartmentMessagesAsync(dept);
            return Ok(messages);
        }

        // ------------------ PROJECT MESSAGE ------------------

        [HttpPost("project")]
        public async Task<IActionResult> SendProjectMessage([FromBody] CreateMessageDto dto)
        {
            if (dto.ProjectId == null)
                return BadRequest("ProjectId is required.");

            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get all member IDs for this project
            var memberIds = await _context.ProjectAssignments
                .Where(pa => pa.ProjectId == dto.ProjectId)
                .Select(pa => pa.MemberId)
                .ToListAsync();

            if (!memberIds.Contains(senderId))
                return Forbid("You are not a member of this project.");

            dto.MessageType = 1;
            dto.ReceiverId = null;

            var message = await _messageService.SendMessageAsync(dto, senderId);
            return Ok(message);
        }


        [HttpGet("project")]
        public async Task<IActionResult> GetProjectMessages()
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get all project IDs the user is assigned to
            var projectIds = await _context.ProjectAssignments
                .Where(pa => pa.MemberId == senderId)
                .Select(pa => pa.ProjectId)
                .ToListAsync();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m =>
                    m.MessageType == 1 &&
                    m.ProjectId.HasValue &&
                    projectIds.Contains(m.ProjectId.Value) &&
                    !m.IsDeleted)
                .OrderBy(m => m.TimeSent)
                .ToListAsync();

            var mapped = _mapper.Map<List<MessageDto>>(messages);
            return Ok(mapped);
        }


        // ------------------ PERSONAL MESSAGE ------------------

        [HttpPost("personal")]
        public async Task<IActionResult> SendPersonalMessage([FromBody] CreateMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ReceiverId))
                return BadRequest("Receiver EmployeeId is required.");

            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Convert EmployeeId to internal user Id
            var receiverUser = await _userService.GetUserByEmployeeIdAsync(dto.ReceiverId);
            if (receiverUser == null)
                return NotFound("Receiver not found.");

            dto.ReceiverId = receiverUser.Id;
            dto.ProjectId = null;
            dto.MessageType = 3;

            var message = await _messageService.SendMessageAsync(dto, senderId);
            return Ok(message);
        }

        [HttpGet("personal")]
        public async Task<IActionResult> GetPersonalMessages()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var messages = await _messageService.GetPersonalMessagesAsync(currentUserId);
            return Ok(messages);
        }
    }
}
