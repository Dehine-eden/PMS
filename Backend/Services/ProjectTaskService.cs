using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto;
using ProjectManagementSystem1.Model.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskStatus = ProjectManagementSystem1.Model.Entities.TaskStatus; // Keep this specific using alias

namespace ProjectManagementSystem1.Services
{
    public class ProjectTaskService : IProjectTaskService
    {
        private readonly AppDbContext _context;

        public ProjectTaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProjectTask> GetTaskByIdAsync(int taskId)
        {
            return await _context.ProjectTasks
                .Include(t => t.ProjectAssignment)
                .Include(t => t.ParentTask)
                .Include(t => t.SubTasks)
                .FirstOrDefaultAsync(t => t.Id == taskId);
        }

        public async Task<List<ProjectTask>> GetAllTasksAsync()
        {
            return await _context.ProjectTasks
                .Include(t => t.ProjectAssignment)
                .Include(t => t.ParentTask)
                .Include(t => t.SubTasks)
                .ToListAsync();
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var taskToDelete = await _context.ProjectTasks.FindAsync(taskId);
            if (taskToDelete == null)
            {
                return false;
            }

            _context.ProjectTasks.Remove(taskToDelete);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ValidateParentTaskAsync(int? parentTaskId, int projectAssignmentIdOfCurrentTask)
        {
            if (!parentTaskId.HasValue) return;

            // Get current task's project ID from its assignment
            var currentTaskProjectAssignment = await _context.ProjectAssignments
                .AsNoTracking() // No need to track for validation
                .FirstOrDefaultAsync(pa => pa.Id == projectAssignmentIdOfCurrentTask);

            if (currentTaskProjectAssignment == null)
            {
                throw new InvalidOperationException($"Invalid Project Assignment ID: {projectAssignmentIdOfCurrentTask} for the current task. This assignment must exist.");
            }
            var currentTaskProjectId = currentTaskProjectAssignment.ProjectId;

            var parentTask = await _context.ProjectTasks
                .AsNoTracking() // No need to track for validation
                .Include(t => t.ProjectAssignment)
                .FirstOrDefaultAsync(t => t.Id == parentTaskId.Value);

            if (parentTask == null)
                throw new InvalidOperationException($"Parent task with ID '{parentTaskId.Value}' not found.");

            if (parentTask.ProjectAssignment == null)
                throw new InvalidOperationException($"Project assignment for parent task (ID: {parentTask.Id}) is missing. Cannot validate project consistency.");

            var parentTaskProjectId = parentTask.ProjectAssignment.ProjectId;

            if (parentTaskProjectId != currentTaskProjectId)
                throw new InvalidOperationException($"Parent task (ID: {parentTaskId.Value}, Project: {parentTaskProjectId}) must belong to the same project as the current task (Project: {currentTaskProjectId}).");
        }

        public async Task ValidateMemberAssignmentAsync(string? memberId, int projectAssignmentIdOfTask)
        {
            var trimmedMemberId = memberId?.Trim();
            if (string.IsNullOrEmpty(trimmedMemberId)) return;

            var taskProjectAssignment = await _context.ProjectAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(pa => pa.Id == projectAssignmentIdOfTask);

            if (taskProjectAssignment == null)
            {
                throw new InvalidOperationException($"Invalid Project Assignment ID: {projectAssignmentIdOfTask} for the task. Cannot validate member assignment.");
            }
            var projectIdForTask = taskProjectAssignment.ProjectId;

            bool isMemberAssignedToProject = await _context.ProjectAssignments
                .AsNoTracking()
                .AnyAsync(pa => pa.ProjectId == projectIdForTask && pa.MemberId == trimmedMemberId);

            if (!isMemberAssignedToProject)
                throw new InvalidOperationException($"Member with ID '{trimmedMemberId}' is not assigned to project ID '{projectIdForTask}'.");
        }

        private async Task ValidateCircularReferenceAsync(int taskId, int? parentTaskId)
        {
            if (!parentTaskId.HasValue) return;

            var visitedIds = new HashSet<int> { taskId };
            int? currentAncestorId = parentTaskId;

            while (currentAncestorId != null)
            {
                if (visitedIds.Contains(currentAncestorId.Value))
                    throw new InvalidOperationException($"Circular task hierarchy detected. Task ID '{taskId}' cannot have an ancestor (ID: {currentAncestorId.Value}) that is itself or one of its descendants.");

                visitedIds.Add(currentAncestorId.Value);

                var ancestorTask = await _context.ProjectTasks
                    .AsNoTracking()
                    .Select(t => new { t.Id, t.ParentTaskId }) // Select only what's needed
                    .FirstOrDefaultAsync(t => t.Id == currentAncestorId.Value);

                currentAncestorId = ancestorTask?.ParentTaskId;
            }
        }

        public async Task<ProjectTask> CreateTaskAsync(ProjectTaskCreateDto dto)
        {
            if (!dto.ParentTaskId.HasValue) // This is a root task
            {
                var assignment = await _context.ProjectAssignments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pa => pa.Id == dto.ProjectAssignmentId);
                if (assignment == null)
                    throw new InvalidOperationException($"Invalid Project Assignment ID: {dto.ProjectAssignmentId} for the root task. This assignment must exist.");
            }

            await ValidateParentTaskAsync(dto.ParentTaskId, dto.ProjectAssignmentId);
            await ValidateMemberAssignmentAsync(dto.AssignedMemberId, dto.ProjectAssignmentId);

            var task = new ProjectTask
            {
                Title = dto.Title,
                Description = dto.Description, // Added description mapping
                ProjectAssignmentId = dto.ProjectAssignmentId,
                ParentTaskId = dto.ParentTaskId,
                Weight = dto.weight, // Assuming ProjectTaskCreateDto.weight maps to ProjectTask.Weight
                Priority = dto.Priority,
                DueDate = dto.DueDate,
                EstimatedHours = dto.EstimatedHours,
                AssignedMemberId = dto.AssignedMemberId
                // EF Core by convention will set ProjectAssignment if ProjectAssignmentId is set,
                // and ParentTask if ParentTaskId is set, assuming the related entities are tracked or loaded.
                // Initial Depth (0) and IsLeaf (true) are fine for new tasks. UpdateHierarchy will adjust later if it becomes a parent/subtask.
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync(); // Save to generate task.Id

            // Validate circular reference after task has an ID and ParentTaskId is set
            await ValidateCircularReferenceAsync(task.Id, task.ParentTaskId);

            // If it's a subtask, its hierarchy (Depth, parent's IsLeaf) will be updated by AddSubtaskAsync.
            // If it's a root task, its initial Depth (0) and IsLeaf (true) are correct.
            // If a root task needs its ParentTask explicitly set to null (it is by default for int?), it's fine.
            // No need to call UpdateHierarchy here as its state is initially correct or will be handled by AddSubtaskAsync.

            return task;
        }

        public async Task<ProjectTask> AddSubtaskAsync(int parentTaskId, ProjectTaskCreateDto subtaskSpecificDto)
        {
            var parentTaskEntity = await _context.ProjectTasks
                .Include(t => t.ProjectAssignment) // Needed for subtask's ProjectAssignmentId
                .Include(t => t.SubTasks)         // Needed for parentTask.UpdateHierarchy() and linking
                .FirstOrDefaultAsync(t => t.Id == parentTaskId);

            if (parentTaskEntity == null)
            {
                throw new InvalidOperationException($"Parent task with ID '{parentTaskId}' not found. Cannot add subtask.");
            }

            var dtoForCreateCall = new ProjectTaskCreateDto
            {
                Title = subtaskSpecificDto.Title,
                Description = subtaskSpecificDto.Description,
                ProjectAssignmentId = parentTaskEntity.ProjectAssignmentId, // Inherit from parent's assignment
                AssignedMemberId = subtaskSpecificDto.AssignedMemberId,    // Use specific member for this subtask
                //ParentTaskId = parentTaskId, // Explicitly set the ParentTaskId for the new subtask
                weight = subtaskSpecificDto.weight,
                EstimatedHours = subtaskSpecificDto.EstimatedHours,
                Priority = subtaskSpecificDto.Priority,
                DueDate = subtaskSpecificDto.DueDate
            };

            // CreateTaskAsync will validate, create, add to context, and save the new subtask.
            var subtaskEntity = await CreateTaskAsync(dtoForCreateCall);

            // At this point, subtaskEntity is created and has its ParentTaskId set.
            // We need to ensure the navigation properties are correctly linked for UpdateHierarchy.
            // EF Core *might* link these if both entities are tracked, but explicit linking is safer.

            if (!parentTaskEntity.SubTasks.Contains(subtaskEntity))
            {
                parentTaskEntity.SubTasks.Add(subtaskEntity);
            }
            if (subtaskEntity.ParentTask == null || subtaskEntity.ParentTask.Id != parentTaskEntity.Id)  // Link the navigation property
            {
                subtaskEntity.ParentTask = parentTaskEntity;
            }

            // Now that the subtask is part of the parent's SubTasks collection and
            // subtask.ParentTask is set, UpdateHierarchy can correctly calculate depths and IsLeaf statuses.         

            parentTaskEntity.UpdateHierarchy();

            // Save changes resulting from UpdateHierarchy (e.g., parent's IsLeaf, subtask's Depth).
            await _context.SaveChangesAsync();

            return subtaskEntity;
        }

        public async Task AssignTaskAsync(int taskId, string memberId)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null)
            {
                throw new NotFoundException($"Task with ID '{taskId}' not found.");
            }

            // Optionally validate if the member is assigned to the project
            await ValidateMemberAssignmentAsync(memberId, task.ProjectAssignmentId);

            task.AssignedMemberId = memberId;
            await _context.SaveChangesAsync();
        }
    }
}
//using Microsoft.EntityFrameworkCore;
//using ProjectManagementSystem1.Data;
//using ProjectManagementSystem1.Model.Dto;
//using ProjectManagementSystem1.Model.Entities;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using TaskStatus = ProjectManagementSystem1.Model.Entities.TaskStatus;

//namespace ProjectManagementSystem1.Services
//{
//    public class ProjectTaskService : IProjectTaskService
//    {
//        private readonly AppDbContext _context;

//        public ProjectTaskService(AppDbContext context)
//        {
//            _context = context;
//        }


//        public async Task ValidateParentTaskAsync(int? parentTaskId, int projectAssignmentId)
//        {
//            if (!parentTaskId.HasValue) return;

//            // Get current task's project from its assignment
//            var currentProjectId = await _context.ProjectAssignments
//                .Where(pa => pa.Id == projectAssignmentId)
//                .Select(pa => pa.ProjectId)
//                .FirstOrDefaultAsync();

//            var parentTask = await _context.ProjectTasks
//                .Include(t => t.ProjectAssignment)
//                .FirstOrDefaultAsync(t => t.Id == parentTaskId);

//            if (parentTask == null)
//                throw new InvalidOperationException("Parent task not found.");
//            // Get parent task's project
//            //var parentProjectId = await _context.ProjectTasks
//            //    .Where(t => t.Id == parentTaskId)
//            //    .Select(t => t.ProjectAssignment.ProjectId)
//            //    .FirstOrDefaultAsync();

//            var parentProjectId = parentTask.ProjectAssignment?.ProjectId;

//            if (parentProjectId != currentProjectId)
//                throw new InvalidOperationException("Parent task must belong to the same project");
//        }

//        public async Task ValidateMemberAssignmentAsync(string? memberId, int projectAssignmentId)
//        {
//            if (string.IsNullOrEmpty(memberId)) return;

//            // Get project ID from the task's assignment
//            var projectId = await _context.ProjectAssignments
//                .Where(pa => pa.Id == projectAssignmentId)
//                .Select(pa => pa.ProjectId)
//                .FirstOrDefaultAsync();

//            // Check if member has any assignment to this project
//            bool isValid = await _context.ProjectAssignments
//                .AnyAsync(pa => pa.ProjectId == projectId && pa.MemberId == memberId);

//            if (!isValid)
//                throw new InvalidOperationException("Member not assigned to project");
//        }

//        private async Task ValidateCircularReferenceAsync(int taskId, int? parentTaskId)
//        {
//            if (!parentTaskId.HasValue) return;

//            var visitedIds = new HashSet<int> { taskId };
//            int? currentParentId = parentTaskId;

//            while (currentParentId != null)
//            {
//                if (visitedIds.Contains(currentParentId.Value))
//                    throw new InvalidOperationException("Circular task hierarchy detected");

//                visitedIds.Add(currentParentId.Value);

//                currentParentId = await _context.ProjectTasks
//                    .Where(t => t.Id == currentParentId.Value)
//                    .Select(t => t.ParentTaskId)
//                    .FirstOrDefaultAsync();
//            }
//        }

//        public async Task<ProjectTask> CreateTaskAsync(ProjectTaskCreateDto dto)
//        {

//            if (!dto.ParentTaskId.HasValue)
//            {
//                var assignment = await _context.ProjectAssignments
//                    .FirstOrDefaultAsync(pa => pa.Id == dto.ProjectAssignmentId);

//                if (assignment == null)
//                    throw new InvalidOperationException("Invalid Project Assignment");
//            }
//            // Validate parent task
//            await ValidateParentTaskAsync(dto.ParentTaskId, dto.ProjectAssignmentId);



//            // Validate member assignment
//            await ValidateMemberAssignmentAsync(dto.AssignedMemberId, dto.ProjectAssignmentId);

//            // Create task
//            var task = new ProjectTask
//            {
//                Title = dto.Title,
//                ProjectAssignmentId = dto.ProjectAssignmentId,
//                ParentTaskId = dto.ParentTaskId,
//                Weight = dto.weight,
//                Priority = dto.Priority,
//                DueDate = dto.DueDate,
//                EstimatedHours = dto.EstimatedHours
//            };

//            // Validate circular reference (after task has an ID)
//            _context.ProjectTasks.Add(task);
//            await _context.SaveChangesAsync(); // Generate task.ID

//            await ValidateCircularReferenceAsync(task.Id, dto.ParentTaskId);

//            return task;
//        }


//        public async Task<ProjectTask> AddSubtaskAsync(int parentTaskId, ProjectTaskCreateDto dto)
//        {
//            // 1. Get parent task with its ProjectAssignment
//            var parentTask = await _context.ProjectTasks
//                .Include(t => t.ProjectAssignment)
//                .Include(t => t.SubTasks)// Load ProjectAssignment
//                .FirstOrDefaultAsync(t => t.Id == parentTaskId);

//            if (parentTask == null)
//                throw new InvalidOperationException("Parent task not found");

//            // Prepare a DTO specifically for creating this subtask
//            var dtoForCreateCall = new ProjectTaskCreateDto
//            {
//                Title = dto.Title,
//                Description = dto.Description,
//                ProjectAssignmentId = parentTask.ProjectAssignmentId, // Inherit from parent
//                AssignedMemberId = dto.AssignedMemberId,    // Use new DTO's assigned member
//                ParentTaskId = parentTaskId, // <<< CRITICAL: Set the correct ParentTaskId for CreateTaskAsync
//                weight = dto.weight,
//                EstimatedHours = dto.EstimatedHours,
//                Priority = dto.Priority,
//                DueDate = dto.DueDate
//            };

//            // 3. Create subtask (now guaranteed same project)
//            var subtask = await CreateTaskAsync(dtoForCreateCall);

//            if (!parentTask.SubTasks.Contains(subtask))
//            {
//                parentTask.SubTasks.Add(subtask);
//            }
//            if (subtask.ParentTask == null)
//            {
//                subtask.ParentTask = parentTask;
//            }

//            // 5. Update hierarchy
//            parentTask.UpdateHierarchy();
//            await _context.SaveChangesAsync();

//            return subtask;
//        }


//    }

//}





////public async Task<ProjectTaskReadDto> CreateTaskAsync(ProjectTaskCreateDto dto)
////{
////    var projectAssignment = await _context.ProjectAssignments
////        .Include(pa => pa.Project)
////        .FirstOrDefaultAsync(pa => pa.Id == dto.ProjectAssignmentId);

////    if (projectAssignment == null)
////        throw new ArgumentException("Invalid ProjectAssignmentId");

////    if (dto.AssignedMemberId != null)
////    {
////        var isValidMember = await _context.ProjectAssignments
////            .AnyAsync(pa => pa.Id == dto.ProjectAssignmentId
////            && pa.MemberId == dto.AssignedMemberId);

////        if (!isValidMember)
////            throw new ArgumentException("Invalid AssignedMemberId");
////    }


////    var task = new ProjectTask
////    {
////        Title = dto.Title,
////        Description = dto.Description,
////        ProjectAssignmentId = dto.ProjectAssignmentId,
////        AssignedMemberId = dto.AssignedMemberId,
////        ParentTaskId = dto.ParentTaskId,
////        Depth = dto.Depth,
////        IsLeaf = dto.IsLeaf,
////        Weight = dto.weight,
////        EstimatedHours = dto.EstimatedHours,
////        Priority = dto.Priority,
////        DueDate = dto.DueDate,
////        Status = TaskStatus.Pending,
////        CreatedAt = DateTime.UtcNow,
////        CreatedBy = "System" // you’ll later pull from authenticated user
////    };

////    if (dto.ParentTaskId.HasValue)
////    {
////        var parentTask = await _context.ProjectTasks
////            .FirstOrDefaultAsync(t => t.Id == dto.ParentTaskId);

////        if (parentTask == null)
////            throw new ArgumentException("Invalid ParentTaskId");

////        task.Depth = parentTask.Depth + 1;

////        parentTask.IsLeaf = false;
////        _context.ProjectTasks.Update(parentTask);
////    }
////    else
////    {
////        task.Depth = 0;
////    }

////    task.IsLeaf = true;

////    _context.ProjectTasks.Add(task);
////    await _context.SaveChangesAsync();

////    return MapToReadDto(task);
////}


////public async Task<ProjectTaskReadDto> GetTaskByIdAsync(int taskId)
////{
////    var task = await _context.ProjectTasks.FindAsync(taskId);

////    if (task == null) return null;

////    return new ProjectTaskReadDto
////    {
////        Id = task.Id,
////        Title = task.Title,
////        Status = task.Status,
////        Priority = task.Priority,
////        Progress = task.Progress,
////        AssignedMemberId = task.AssignedMemberId
////    };
////}

////public async Task<IEnumerable<ProjectTaskReadDto>> GetAllTasksAsync()
////{
////    return await _context.ProjectTasks
////        .Select(task => new ProjectTaskReadDto
////        {
////            Id = task.Id,
////            Title = task.Title,
////            Status = task.Status,
////            Priority = task.Priority,
////            Progress = task.Progress,
////            AssignedMemberId = task.AssignedMemberId
////        })
////        .ToListAsync();
////}

////public async Task<ProjectTaskReadDto> UpdateTaskAsync(int id, ProjectTaskUpdateDto dto)
////{
////    var task = await _context.ProjectTasks
////        .Include(t => t.SubTasks)
////        .FirstOrDefaultAsync(t => t.Id == id);

////    if (task == null) return null;

////    // Only update fields that are not null
////    if (!string.IsNullOrEmpty(dto.Title)) task.Title = dto.Title;
////    if (!string.IsNullOrEmpty(dto.Description)) task.Description = dto.Description;
////    if (dto.Status.HasValue)
////    {
////        if (dto.Status.Value == TaskStatus.Rejected && string.IsNullOrWhiteSpace(dto.RejectionReason))
////            throw new ArgumentException("Rejection reason must be provided when status is rejected");

////        task.Status = dto.Status.Value;
////    }
////    if (dto.Priority.HasValue) task.Priority = dto.Priority.Value;
////    if (dto.Progress.HasValue) task.Progress = dto.Progress.Value;
////    if (!string.IsNullOrEmpty(dto.AssignedMemberId)) task.AssignedMemberId = dto.AssignedMemberId;
////    if (!string.IsNullOrWhiteSpace(dto.RejectionReason)) task.RejectionReason = dto.RejectionReason;

////    task.UpdatedAt = DateTime.UtcNow;
////    task.UpdatedBy = "System";

////    await _context.SaveChangesAsync();

////    return new ProjectTaskReadDto
////    {
////        Id = task.Id,
////        Title = task.Title,
////        Status = task.Status,
////        Priority = task.Priority,
////        Progress = task.Progress,
////        AssignedMemberId = task.AssignedMemberId
////    };
////}

////public async Task<bool> DeleteTaskAsync(int taskId)
////{
////    var task = await _context.ProjectTasks.FindAsync(taskId);
////    if (task == null) return false;

////    _context.ProjectTasks.Remove(task);
////    await _context.SaveChangesAsync();
////    return true;
////}

