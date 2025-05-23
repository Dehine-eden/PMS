using ProjectManagementSystem1.Model.Dto;
using ProjectManagementSystem1.Model.Entities;
using System.Threading.Tasks;

namespace ProjectManagementSystem1.Services
{
    public interface IProjectTaskService
    {
        Task<ProjectTask> CreateTaskAsync(ProjectTaskCreateDto dto);
        Task<List<ProjectTask>> GetAllTasksAsync();
        Task<ProjectTask> GetTaskByIdAsync(int taskId);
        Task ValidateParentTaskAsync(int? parentTaskId, int projectAssignmentId);
        Task ValidateMemberAssignmentAsync(string? memberId, int projectAssignmentId);
        Task<ProjectTask> AddSubtaskAsync(int parentTaskId, ProjectTaskCreateDto dto);
        Task AssignTaskAsync(int taskId, string memberId);
        Task<bool> DeleteTaskAsync(int taskId);


        //Task ValidateCircularReferenceAsync(int taskId, int? parentTaskId);
        //Task<ProjectTaskReadDto> GetTaskByIdAsync(int taskId);
        //Task<IEnumerable<ProjectTaskReadDto>> GetAllTasksAsync();
        //Task<ProjectTaskReadDto> UpdateTaskAsync(int id, ProjectTaskUpdateDto dto);
        //Task<bool> DeleteTaskAsync(int taskId);
    }
}

