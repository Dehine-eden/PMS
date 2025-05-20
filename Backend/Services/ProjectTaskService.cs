using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.ProjectTaskDto;
using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Services
{
    public class ProjectTaskService : IProjectTaskService
    {
        private readonly AppDbContext _context; // Replace with your actual DbContext
        private readonly IMapper _mapper; // If using AutoMapper

        public ProjectTaskService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProjectTaskDto>> GetAllAsync()
        {

            var tasks = await _context.ProjectTasks
                .Include(t => t.Assignee) // For EmployeeId
                .Include(t => t.ParentTask) // If needed
                .ToListAsync();

            return _mapper.Map<IEnumerable<ProjectTaskDto>>(tasks);
        }

        public async Task<ProjectTaskDto?> GetByIdAsync(Guid id)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.Assignee) // Load Assignee
                .FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return null;
            return _mapper.Map<ProjectTaskDto>(task);
        }

        public async Task<ProjectTaskDto> CreateAsync(CreateProjectTaskDto createDto)
        {
            //// Handle default GUID for ParentTaskId
            //if (createDto.ParentTaskId == default || createDto.ParentTaskId == Guid.Empty)
            //{
            //    createDto.ParentTaskId = null;
            //}
            if (createDto.ParentTaskId == default || createDto.ParentTaskId == Guid.Empty)
            {
                createDto.ParentTaskId = null;
            }

            // Find user by EmployeeId (e.g., "010102")
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == createDto.EmployeeId);
            if (user == null) throw new InvalidOperationException("User not found.");

            // Find project assignment for the user and project
            var projectAssignment = await _context.ProjectAssignments
                .FirstOrDefaultAsync(pa =>
                    pa.ProjectId == createDto.ProjectId &&
                    pa.MemberId == user.Id
                );
            if (projectAssignment == null)
                throw new InvalidOperationException("Member is not assigned to this project.");

            // Validate parent task (if provided)
            if (createDto.ParentTaskId.HasValue)
            {
                var parentTask = await _context.ProjectTasks
                    .Include(t => t.ProjectAssignment)
                    .FirstOrDefaultAsync(t => t.Id == createDto.ParentTaskId.Value);
                if (parentTask == null)
                    throw new InvalidOperationException("Parent task not found.");
                if (parentTask.ProjectAssignment.ProjectId != createDto.ProjectId)
                    throw new InvalidOperationException("Parent task belongs to a different project.");
            }

            // Create the task
            var entity = _mapper.Map<ProjectTask>(createDto);
            entity.Id = Guid.NewGuid();
            entity.AssignedMemberId = user.Id; // Identity ID, not EmployeeId
            entity.ProjectAssignmentId = projectAssignment.Id;
            entity.CreatedDate = DateTime.UtcNow;
            entity.UpdatedDate = DateTime.UtcNow;
            

            _context.ProjectTasks.Add(entity);
            await _context.SaveChangesAsync();

            // Reload the task with Assignee to populate EmployeeId in the response
            var createdTask = await _context.ProjectTasks
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(t => t.Id == entity.Id);

            return _mapper.Map<ProjectTaskDto>(createdTask);
        }

        //public async Task<bool> AssignTaskToUserAsync(Guid taskId, int assigneeId, string supervisorId)
        //{
        //    var task = await _context.ProjectTasks.FindAsync(taskId);
        //    if (task == null) return false;

        //    // Example validation: Ensure assignee is part of the project
        //    var isValidAssignee = await _context.ProjectAssignments
        //        .AnyAsync(pa => pa.ProjectId == task.ProjectAssignmentId && pa.UserId == assigneeId);

        //    if (!isValidAssignee) return false;

        //    task.AssignedMemberId = assigneeId;
        //    task.IsAssignedBySupervisor = true;
        //    task.AssignmentStatus = AssignmentStatus.Pending;
        //    task.AssignmentUpdatedDate = DateTime.UtcNow;
        //    task.UpdatedBy = supervisorId;

        //    await _context.SaveChangesAsync();
        //    return true;
        //}
        public async Task<bool> UpdateAsync(UpdateProjectTaskDto updateDto)
        {
            // Handle default GUID for ParentTaskId
            if (updateDto.ParentTaskId == default || updateDto.ParentTaskId == Guid.Empty)
            {
                updateDto.ParentTaskId = null;
            }

            // Include Assignee to map EmployeeId later
            var existingTask = await _context.ProjectTasks
                .Include(t => t.ProjectAssignment)
                .Include(t => t.Assignee) // Add this line
                .FirstOrDefaultAsync(t => t.Id == updateDto.Id);

            if (existingTask == null)
            {
                return false;
            }


            // Validate ParentTaskId only if provided
            if (updateDto.ParentTaskId.HasValue)
            {
                if (updateDto.ParentTaskId == updateDto.Id)
                    throw new InvalidOperationException("Task cannot be its own parent.");

                var parentTask = await _context.ProjectTasks
                    .Include(t => t.ProjectAssignment)
                    .FirstOrDefaultAsync(t => t.Id == updateDto.ParentTaskId.Value);

                if (parentTask == null)
                    throw new InvalidOperationException("Parent task not found.");

                if (parentTask.ProjectAssignment.ProjectId != existingTask.ProjectAssignment.ProjectId)
                    throw new InvalidOperationException("Parent task belongs to a different project.");

                existingTask.ParentTaskId = updateDto.ParentTaskId.Value; // Set valid parent
            }
            else
            {
                existingTask.ParentTaskId = null; // Explicitly set to null
            }


            // Update hierarchy properties (Depth, IsLeaf)
            await UpdateTaskHierarchyAsync(existingTask);

            // Rest of your existing code for handling member reassignment and mapping...

            // If reassigning member
            if (!string.IsNullOrEmpty(updateDto.EmployeeId))
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == updateDto.EmployeeId);
                if (user == null) throw new InvalidOperationException("User not found.");

                var newAssignment = await _context.ProjectAssignments
                    .FirstOrDefaultAsync(pa => pa.ProjectId == existingTask.ProjectAssignment.ProjectId && pa.MemberId == user.Id);

                if (newAssignment == null)
                    throw new InvalidOperationException("Member is not assigned to this project.");

                existingTask.AssignedMemberId = user.Id;
                existingTask.ProjectAssignmentId = newAssignment.Id;
            }

            // Map other fields from DTO
            _mapper.Map(updateDto, existingTask);
            existingTask.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update hierarchy again in case parent/child relationships changed
            await UpdateTaskHierarchyAsync(existingTask);

            return true;
        }
        private async Task UpdateTaskHierarchyAsync(ProjectTask task)
        {
            // Compute Depth
            task.Depth = task.ParentTaskId.HasValue
                ? (await _context.ProjectTasks.FindAsync(task.ParentTaskId.Value))?.Depth + 1 ?? 0
                : 0;

            // Update IsLeaf for the previous parent (if any)
            var previousParentId = _context.Entry(task).Property(x => x.ParentTaskId).OriginalValue;
            if (previousParentId.HasValue)
            {
                var previousParent = await _context.ProjectTasks.FindAsync(previousParentId.Value);
                if (previousParent != null)
                {
                    var hasSiblings = await _context.ProjectTasks
                        .AnyAsync(t => t.ParentTaskId == previousParentId && t.Id != task.Id);
                    previousParent.IsLeaf = !hasSiblings;
                }
            }

            // Update IsLeaf for the new parent (if any)
            if (task.ParentTaskId.HasValue)
            {
                var newParent = await _context.ProjectTasks.FindAsync(task.ParentTaskId.Value);
                if (newParent != null)
                    newParent.IsLeaf = false;
            }

            // Check if current task is a leaf
            task.IsLeaf = !await _context.ProjectTasks.AnyAsync(t => t.ParentTaskId == task.Id);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _context.ProjectTasks.FindAsync(id);
            if (existing == null) return false;

            _context.ProjectTasks.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        
    }

}