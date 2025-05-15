using ProjectManagementSystem1.Model.Dto.ProjectAssignmentDto;

public interface IProjectAssignmentService
{
    Task<List<AssignmentDto>> GetAllByProjectAsync(int projectId);
    Task<List<UserProjectDto>> GetProjectsByEmployeeIdAsync(string employeeId);
    Task<AssignmentDto?> GetByIdAsync(int id);
    Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto, string currentUser);
    Task<bool> UpdateAsync(int id, UpdateAssignmentDto dto, string currentUser);
    Task<bool> DeleteAsync(int id);
}
