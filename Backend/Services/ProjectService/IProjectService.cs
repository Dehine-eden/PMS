using ProjectManagementSystem1.Model.Dto.ProjectManagementDto;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Model.Dto;

namespace ProjectManagementSystem1.Services.ProjectService
{
    public interface IProjectService
    {
        Task<List<ProjectDto>> GetAllAsync(string userDept);
        Task<ProjectDto> GetByIdAsync(int id);
        Task<List<ProjectDto>> GetProjectsByUserAsync(int userId);
        Task<List<ProjectDto>> GetPendingApprovalsAsync(int managerId);
        Task<bool> ProcessApprovalAsync(int approvalRequestId, int managerId, ApprovalActionDto action);
        Task<ProjectDto> CreateAsync(CreateProjectDto dto, string currentUser);
        Task<bool> UpdateAsync(int id, UpdateProjectDto dto, string currentUser);
        Task<bool> DeleteAsync(int id);
        Task ArchiveProjectAsync(int projectId, string currentUser);
        Task RestoreProjectAsync(int projectId, string currentUser);
    }

}
