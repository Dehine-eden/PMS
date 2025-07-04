using ProjectManagementSystem1.Model.Dto.Message;

namespace ProjectManagementSystem1.Services.MessageService
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessageAsync(CreateMessageDto dto, string senderId);
        Task<List<MessageDto>> GetProjectMessagesAsync(int projectId);
        Task<List<MessageDto>> GetDepartmentMessagesAsync(string department);
        Task<List<MessageDto>> GetPersonalMessagesAsync(string userId);
        Task<List<MessageDto>> GetProjectMessagesForUserAsync(string userId);

    }

}
