using ProjectManagementSystem1.Model.Dto.ProjectTaskDto;
using ProjectManagementSystem1.Model.Entities;
using System.Threading.Tasks;

namespace ProjectManagementSystem1.Services
{
    public interface IProjectTaskService
    {
        Task<IEnumerable<ProjectTaskDto>> GetAllAsync();

        Task<ProjectTaskDto?> GetByIdAsync(Guid id);

        Task<ProjectTaskDto> CreateAsync(CreateProjectTaskDto createDto);
        //Task<bool> AssignTaskToUserAsync(Guid taskId, int assigneeId, string supervisorId)

        Task<bool> UpdateAsync(UpdateProjectTaskDto updateDto);
        Task<bool> DeleteAsync(Guid id);
    }

}
