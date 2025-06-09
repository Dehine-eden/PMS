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
        private readonly INotificationService _notification;
        public ProjectTaskService(AppDbContext context, INotificationService notification)
        {
            _context = context;
            _notification = notification;
        }

        public async Task<ProjectTask> GetTaskByIdAsync(int taskId)
        {
            var task = await _context.ProjectTasks
                .Include(t => t.ProjectAssignment)
                .Include(t => t.ParentTask)
                .Include(t => t.SubTasks)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task != null)
            {
                await LoadSubtasksRecursively(task);
            }

            return task;
        }

        private async Task LoadSubtasksRecursively(ProjectTask task)
        {
            if (task.SubTasks != null && task.SubTasks.Any())
            {
                foreach (var subtask in task.SubTasks)
                {
                    if (!_context.Entry(subtask).Collection(t => t.SubTasks).IsLoaded)
                    {
                        await _context.Entry(subtask).Collection(t => t.SubTasks).LoadAsync();
                    }

                    await LoadSubtasksRecursively(subtask);
                }
            }
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
            var taskToDelete = await _context.ProjectTasks
                .Include(t => t.SubTasks)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (taskToDelete == null)
            {
                return false;
            }

            string assignedMemberId = taskToDelete.AssignedMemberId;
            string taskTitle = taskToDelete.Title;


            if (taskToDelete.SubTasks.Any())
            {
                foreach (var subtask in taskToDelete.SubTasks.ToList())
                {
                    await DeleteTaskAsync(subtask.Id);
                }
            }

            _context.ProjectTasks.Remove(taskToDelete);
            await _context.SaveChangesAsync();

            await _notification.CreateNotificationAsync(
                    recipientUserId: assignedMemberId,
                    subject: "Task Deleted",
                    message: $"Task '{taskTitle}' has been deleted.",
                    relatedEntityType: "ProjectTask",
                    relatedEntityId: taskId,
                    deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                );

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

        public async Task<ProjectTask> CreateTaskAsync(ProjectTaskCreateDto dto, string creatorId)
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

            if (dto.weight < 1 || dto.weight > 100)
            {
                throw new InvalidOperationException("Weight must be between 1 and 100.");
            }

            var task = new ProjectTask
            {
                Title = dto.Title,
                Description = dto.Description, // Added description mapping
                ProjectAssignmentId = dto.ProjectAssignmentId,
                ParentTaskId = dto.ParentTaskId,
                Weight = dto.weight, // Assuming ProjectTaskCreateDto.weight maps to ProjectTask.Weight
                Priority = dto.Priority,
                //ProjectGoal = dto.ProjectGoalId,
                DueDate = dto.DueDate,
                EstimatedHours = dto.EstimatedHours,
                AssignedMemberId = dto.AssignedMemberId
                // EF Core by convention will set ProjectAssignment if ProjectAssignmentId is set,
                // and ParentTask if ParentTaskId is set, assuming the related entities are tracked or loaded.
                // Initial Depth (0) and IsLeaf (true) are fine for new tasks. UpdateHierarchy will adjust later if it becomes a parent/subtask.
            };


            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync(); // Save to generate task.Id

            // Notify the creator
            await _notification.CreateNotificationAsync(
                recipientUserId: creatorId,
                subject: "New Task Created",
                message: $"You have created task: '{task.Title}'.",
                relatedEntityType: "ProjectTask",
                relatedEntityId: task.Id,
                deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
            );
            // Notify the assigned member, if any
            if (!string.IsNullOrEmpty(task.AssignedMemberId) && task.AssignedMemberId != creatorId)
            {
                await _notification.CreateNotificationAsync(
                     recipientUserId: task.AssignedMemberId,
                     subject: "Assigned to Task",
                     message: $"You have been assigned to task: '{task.Title}'.",
                     relatedEntityType: "ProjectTask",
                     relatedEntityId: task.Id,
                     deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                 );
            }
            await UpdateParentTaskEstimatedHoursAsync(task.Id);

            // Validate circular reference after task has an ID and ParentTaskId is set
            await ValidateCircularReferenceAsync(task.Id, task.ParentTaskId);

            // If it's a subtask, its hierarchy (Depth, parent's IsLeaf) will be updated by AddSubtaskAsync.
            // If it's a root task, its initial Depth (0) and IsLeaf (true) are correct.
            // If a root task needs its ParentTask explicitly set to null (it is by default for int?), it's fine.
            // No need to call UpdateHierarchy here as its state is initially correct or will be handled by AddSubtaskAsync.

            return task;
        }
        public async Task<ProjectTask> AddSubtaskAsync(int parentTaskId, ProjectTaskCreateDto subtaskSpecificDto, string creatorId)
        {
            var parentTaskEntity = await _context.ProjectTasks
                .Include(t => t.ProjectAssignment) // Needed for subtask's ProjectAssignmentId
                .Include(t => t.ParentTask)
                .Include(t => t.SubTasks)
                .FirstOrDefaultAsync(t => t.Id == parentTaskId);

            if (parentTaskEntity == null)
            {
                throw new InvalidOperationException($"Parent task with ID '{parentTaskId}' not found. Cannot add subtask.");
            }

            if (parentTaskEntity.SubTasks.Sum(st => st.Weight) + subtaskSpecificDto.weight > 100)
            {
                throw new InvalidCastException($"The total weight of subtasks under parent '{parentTaskEntity.Title}' cannot exceed 100");
            }

            var dtoForCreateCall = new ProjectTaskCreateDto
            {
                Title = subtaskSpecificDto.Title,
                Description = subtaskSpecificDto.Description,
                ProjectAssignmentId = parentTaskEntity.ProjectAssignmentId, // Inherit from parent's assignment
                AssignedMemberId = subtaskSpecificDto.AssignedMemberId,    // Use specific member for this subtask
                ParentTaskId = parentTaskId, // Explicitly set the ParentTaskId for the new subtask
                weight = subtaskSpecificDto.weight,
                EstimatedHours = subtaskSpecificDto.EstimatedHours,
                //Priority = subtaskSpecificDto.Priority,
                DueDate = subtaskSpecificDto.DueDate
            };

            // CreateTaskAsync will validate, create, add to context, and save the new subtask.
            var subtaskEntity = await CreateTaskAsync(dtoForCreateCall, creatorId);

            // Notify the creator
            await _notification.CreateNotificationAsync(
                recipientUserId: creatorId,
                subject: "New Subtask Created",
                message: $"You have created subtask: '{subtaskEntity.Title}' under task '{parentTaskEntity.Title}'.",
                relatedEntityType: "ProjectTask",
                relatedEntityId: subtaskEntity.Id,
                deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
            );

            // Notify the assigned member, if any
            if (!string.IsNullOrEmpty(subtaskEntity.AssignedMemberId) && subtaskEntity.AssignedMemberId != creatorId)
            {
                 await _notification.CreateNotificationAsync(
                    recipientUserId: subtaskEntity.AssignedMemberId,
                    subject: "Assigned to Subtask",
                    message: $"You have been assigned to subtask: '{subtaskEntity.Title}' under task '{parentTaskEntity.Title}'.",
                    relatedEntityType: "ProjectTask",
                    relatedEntityId: subtaskEntity.Id,
                    deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                );
            }

            if (!parentTaskEntity.SubTasks.Contains(subtaskEntity))
            {
                parentTaskEntity.SubTasks.Add(subtaskEntity);
            }
            if (subtaskEntity.ParentTask == null || subtaskEntity.ParentTask.Id != parentTaskEntity.Id)  // Link the navigation property
            {
                subtaskEntity.ParentTask = parentTaskEntity;
            }

            parentTaskEntity.UpdateHierarchy();

            // Save changes resulting from UpdateHierarchy (e.g., parent's IsLeaf, subtask's Depth).
            await _context.SaveChangesAsync();
            await UpdateParentTaskEstimatedHoursAsync(parentTaskEntity.Id);

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

            string originalAssignee = task.AssignedMemberId;
            task.AssignedMemberId = memberId;
            await _context.SaveChangesAsync();

            if (originalAssignee != memberId)
            {
                if (!string.IsNullOrEmpty(memberId))
                {
                    await _notification.CreateNotificationAsync(
                        recipientUserId: memberId,
                        subject: "Assigned to Task",
                        message: $"You have been assigned to task: '{task.Title}'.",
                        relatedEntityType: "ProjectTask",
                        relatedEntityId: taskId,
                        deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                    );
                }
                if (!string.IsNullOrEmpty(originalAssignee))
                {
                    await _notification.CreateNotificationAsync(
                        recipientUserId: originalAssignee,
                        subject: "Unassigned from Task",
                        message: $"You have been unassigned from task: '{task.Title}'.",
                        relatedEntityType: "ProjectTask",
                        relatedEntityId: taskId,
                        deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                    );
                }
            }


            // Enhancement: Automatically create a TodoItem for the assigned task with the same weight as the task
            var mainTodoItem = new TodoItem
            {
                ProjectTaskId = taskId,
                Title = $"Action Item for {task.Title}",
                Weight = task.Weight, // Inherit weight from the task
                Status = TodoItemStatus.Pending
            };
            _context.TodoItems.Add(mainTodoItem);
            await _context.SaveChangesAsync();
            await _notification.CreateNotificationAsync(
                recipientUserId: memberId,
                subject: "New Action Item Created",
                message: $"A new action item has been created for you in task: '{task.Title}'.",
                relatedEntityType: "TodoItem",
                relatedEntityId: mainTodoItem.Id,
                deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
            );

            // Enhancement: Automatically create TodoItems and assign member to subtasks as well with the same weight as the subtask
            if (task.SubTasks != null && task.SubTasks.Any())
            {
                foreach (var subtask in task.SubTasks)
                {
                    string originalSubtaskAssignee = subtask.AssignedMemberId;
                    // Assign the member to the subtask
                    subtask.AssignedMemberId = memberId;
                    _context.ProjectTasks.Update(subtask); // Mark subtask for update

                    var subtaskTodoItem = new TodoItem
                    {
                        ProjectTaskId = subtask.Id,
                        Title = $"Action Item for {subtask.Title}",
                        Weight = subtask.Weight, // Inherit weight from the subtask
                        Status = TodoItemStatus.Pending
                    };
                    _context.TodoItems.Add(subtaskTodoItem);
                    await _context.SaveChangesAsync(); // Save changes for both subtask assignment and todo item creation
                    if (originalSubtaskAssignee != memberId)
                    {
                        if (!string.IsNullOrEmpty(memberId))
                        {
                            await _notification.CreateNotificationAsync(
                                recipientUserId: memberId,
                                subject: "Assigned to Subtask",
                                message: $"You have been assigned to subtask: '{subtask.Title}' under task '{task.Title}'.",
                                relatedEntityType: "ProjectTask",
                                relatedEntityId: subtask.Id,
                                deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                            );
                        }
                        if (!string.IsNullOrEmpty(originalSubtaskAssignee))
                        {
                            await _notification.CreateNotificationAsync(
                                recipientUserId: originalSubtaskAssignee,
                                subject: "Unassigned from Subtask",
                                message: $"You have been unassigned from subtask: '{subtask.Title}' under task '{task.Title}'.",
                                relatedEntityType: "ProjectTask",
                                relatedEntityId: subtask.Id,
                                deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                            );
                        }
                    }
                    await _notification.CreateNotificationAsync(
                        recipientUserId: memberId,
                        subject: "New Action Item Created",
                        message: $"A new action item has been created for you in subtask: '{subtask.Title}' under task '{task.Title}'.",
                        relatedEntityType: "TodoItem",
                        relatedEntityId: subtaskTodoItem.Id,
                        deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                    );
                }
                await _context.SaveChangesAsync(); // Save all subtask assignments
            }
        }

        public async Task<ProjectTask> UpdateTaskAsync(int id, string memberIdFromToken, ProjectTaskUpdateDto dto, bool isSupervisor)
        {
            var task = await _context.ProjectTasks.FindAsync(id);
            if (task == null)
            {
                return null;
            }

            string originalAssignedMemberId = task.AssignedMemberId;
            DateTime? originalDueDate = task.DueDate;
            TaskStatus originalStatus = task.Status;

            // Update properties if provided in the DTO
            if (dto.Title != null)
            {
                task.Title = dto.Title;
            }
            if (dto.Description != null)
            {
                task.Description = dto.Description;
            }
            if (dto.Weight.HasValue)
            {
                if (dto.Weight.HasValue && (dto.Weight.Value < 1 || dto.Weight.Value > 100))
                {
                    throw new InvalidOperationException("Weight must be between 1 and 100.");
                }
                task.Weight = dto.Weight.Value;
                await UpdateParentTaskWeightAsync(id);
            }

            if (dto.DueDate.HasValue && dto.DueDate.Value != originalDueDate)
            {
                // Notify assigned member and potentially supervisor
                if (!string.IsNullOrEmpty(task.AssignedMemberId))
                {
                    await _notification.CreateNotificationAsync(
                        recipientUserId: task.AssignedMemberId,
                        subject: "Task Due Date Updated",
                        message: $"The due date of task '{task.Title}' has been updated to '{dto.DueDate?.ToString("yyyy-MM-dd")}'.",
                        relatedEntityType: "ProjectTask",
                        relatedEntityId: id,
                        deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                    );
                }
                // Optionally notify supervisor
            }
            if (dto.StartDate.HasValue) task.StartDate = dto.StartDate; // Ensure this line is present
            if (dto.EstimatedHours.HasValue)
            {
                task.EstimatedHours = dto.EstimatedHours.Value;
                await UpdateParentTaskEstimatedHoursAsync(id); // Update parent on estimated hours change
            }
            if (dto.ActualHours.HasValue)
            {
                task.ActualHours = dto.ActualHours.Value;
            }
            if (dto.AssignedMemberId != null && dto.AssignedMemberId != originalAssignedMemberId)
            {
                // Notify the newly assigned member
                if (!string.IsNullOrEmpty(dto.AssignedMemberId))
                {
                    await _notification.CreateNotificationAsync(
                        recipientUserId: dto.AssignedMemberId,
                        subject: "Assigned to Task",
                        message: $"You have been assigned to task: '{task.Title}'.",
                        relatedEntityType: "ProjectTask",
                        relatedEntityId: id,
                        deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                    );
                }
                // Optionally notify the previously assigned member
                if (!string.IsNullOrEmpty(originalAssignedMemberId))
                {
                    await _notification.CreateNotificationAsync(
                        recipientUserId: originalAssignedMemberId,
                        subject: "Unassigned from Task",
                        message: $"You have been unassigned from task: '{task.Title}'.",
                        relatedEntityType: "ProjectTask",
                        relatedEntityId: id,
                        deliveryMethod: NotificationDeliveryMethod.InApp // You can choose the delivery method
                    );
                }
                // Optionally notify supervisor
            }
            //if (dto.Status.HasValue) task.Status = dto.Status.Value; // Status removed
            if (dto.Priority.HasValue) task.Priority = dto.Priority.Value;

            task.UpdatedAt = DateTime.UtcNow;
            _context.ProjectTasks.Update(task);
            await _context.SaveChangesAsync();

            return task;
        }

        public async Task AcceptTaskAsync(int taskId, string memberId)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null)
            {
                throw new NotFoundException($"Task with ID '{taskId}' not found.");
            }

            await ValidateMemberAssignmentAsync(memberId, task.ProjectAssignmentId);

            if (task.AssignedMemberId != memberId)
            {
                throw new InvalidOperationException($"Task with ID '{taskId}' is not assigned to member ID '{memberId}'.");
            }

            if (!task.IsLeaf)
            {
                throw new InvalidOperationException($"Task with Id '{taskId}' is not a leaf task and cannot be accepted directly.");
            }
            if (task.Status == TaskStatus.Pending || task.Status == TaskStatus.Rejected)
            {
                task.Status = TaskStatus.Accepted;
                task.RejectionReason = null;
                await _context.SaveChangesAsync();

                await _notification.CreateNotificationAsync(
                   userId: memberId,
                   message: $"You have accepted task: '{task.Title}'.",
                   relatedEntityType: "ProjectTask",
                   relatedEntityId: taskId
               );
            }
            else
            {
                throw new InvalidOperationException($"Task with ID '{taskId}' is not in a Pending state and cannot be accepted.");
            }


        }

        public async Task RejectTaskAsync(int taskId, string memberId, string reason)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null)
            {
                throw new NotFoundException($"Task with ID '{taskId}' not found.");
            }

            await ValidateMemberAssignmentAsync(memberId, task.ProjectAssignmentId);

            if (task.AssignedMemberId != memberId)
            {
                throw new InvalidOperationException($"Task with ID '{taskId}' is not assigned to member ID '{memberId}'.");
            }

            if (!task.IsLeaf)
            {
                throw new InvalidOperationException($"Task with ID '{taskId}' is not a leaf task and cannot be rejected directly.");
            }
            if (task.Status == TaskStatus.Pending || task.Status == TaskStatus.Accepted)
            {
                task.Status = TaskStatus.Rejected;
                task.RejectionReason = reason;
                await _context.SaveChangesAsync();

                await _notification.CreateNotificationAsync(
                    userId: memberId,
                    message: $"You have rejected task: '{task.Title}' with reason.",
                    relatedEntityType: "ProjectTask",
                    relatedEntityId: taskId
                );
            }
            else
            {
                throw new InvalidOperationException($"Task with ID '{taskId}' is not in a Pending state and cannot be rejected.");
            }
        }

        public async Task UpdateTaskActualHoursAsync(int taskId, string memberId, double actualHours)
        {
            var task = await _context.ProjectTasks.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException($"Task with ID '{taskId} not found.");

            await ValidateMemberAssignmentAsync(memberId, task.ProjectAssignmentId);

            if (task.AssignedMemberId != memberId)
            {
                throw new InvalidOperationException($"Task with ID '{taskId}' is not assigned to member ID '{memberId}.");
            }

            task.ActualHours = actualHours;
            task.UpdatedAt = DateTime.UtcNow;
            _context.ProjectTasks.Update(task);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateTaskProgressAsync(int taskId, string memberId, double progress)
        {

            var task = await _context.ProjectTasks
                .Include(t => t.SubTasks)
                .FirstOrDefaultAsync(t => t.Id == taskId);


            if (task == null)
            {
                throw new NotFoundException($"Task with ID '{taskId}' not found.");
            }

            await ValidateMemberAssignmentAsync(memberId, task.ProjectAssignmentId);

            if (task.AssignedMemberId != memberId)
            {
                throw new InvalidOperationException($"Task with ID '{taskId}' is not assigned to member ID '{memberId}'.");
            }

            if (task.Status != TaskStatus.Accepted)
            {
                throw new InvalidOperationException($"Progress can only be updated for task '{task.Title}' if it has been accepted.");
            }

            if (!task.IsLeaf)
            {
                await _context.Entry(task).ReloadAsync();
                if (!task.IsLeaf)
                {
                    throw new InvalidOperationException("Progress can only be updated for leaf tasks.");
                }
            }

            task.Progress = progress;

            if (task.Weight > 0)
            {
                task.Weight = (int)(task.Weight * (1 - (progress / 100)));
                task.Weight = Math.Max(0, Math.Min(100, task.Weight));
            }

            task.UpdatedAt = DateTime.UtcNow;
            _context.ProjectTasks.Update(task);
            await _context.SaveChangesAsync();

            await UpdateParentTaskProgressAsync(task.ParentTaskId); // Update parent progress
            if (task.ParentTaskId.HasValue)
            {
                await UpdateParentTaskWeightAsync(task.ParentTaskId.Value);
            }
        }

        private async Task UpdateParentTaskProgressAsync(int? parentTaskId)
        {

            Console.WriteLine($"UpdateParentTaskProgressAsync called with parentTaskId: {parentTaskId}");

            if (parentTaskId.HasValue)
            {
                var parentTask = await _context.ProjectTasks
                    .Include(p => p.SubTasks)
                    .FirstOrDefaultAsync(p => p.Id == parentTaskId);

                if (parentTask == null)
                {
                    Console.WriteLine($"Parent task with ID {parentTaskId} not found.");
                    return; // Exit the method if the parent task doesn't exist
                }


                _context.Entry(parentTask).Reload();

                if (parentTask != null)
                {
                    if (parentTask.SubTasks.Any())
                    {
                        double totalWeight = parentTask.SubTasks.Sum(st => st.Weight);
                        if (totalWeight > 0)
                        {
                            double weightedProgressSum = parentTask.SubTasks.Sum(st => st.Progress * st.Weight);
                            parentTask.Progress = weightedProgressSum / totalWeight;
                            Console.WriteLine($"Calculated progress for parent task ID {parentTaskId}: {parentTask.Progress}");
                        }
                        else
                        {
                            parentTask.Progress = 0;
                        }

                        //parentTask.Progress = parentTask.SubTasks.Average(st => st.Progress);
                        parentTask.UpdatedAt = DateTime.UtcNow;
                        _context.Entry(parentTask).State = EntityState.Modified;

                        await _context.SaveChangesAsync();

                        Console.WriteLine($"Calling UpdateParentTaskProgressAsync recursively with parentTaskId: {parentTask.ParentTaskId}");
                        await UpdateParentTaskProgressAsync(parentTask.ParentTaskId); // Recursive update
                    }
                }
            }
        }
        private async Task UpdateParentTaskEstimatedHoursAsync(int taskId)
        {
            var task = await _context.ProjectTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task?.ParentTaskId.HasValue == true)
            {
                var parentTask = await _context.ProjectTasks
                    .Include(p => p.SubTasks)
                    .FirstOrDefaultAsync(p => p.Id == task.ParentTaskId);

                if (parentTask != null)
                {
                    double totalEstimatedHours = parentTask.SubTasks.Sum(sub => sub.EstimatedHours);
                    parentTask.EstimatedHours = totalEstimatedHours;
                    _context.ProjectTasks.Update(parentTask);
                    await _context.SaveChangesAsync();

                    // Optionally, you could propagate this update further up the hierarchy
                    // if your requirements dictate immediate consistency across all levels.
                    // However, for better performance, you might defer such full updates.
                }
            }

        }

        private async Task UpdateParentTaskWeightAsync(int? parentTaskId)
        {
            Console.WriteLine($"UpdateParentTaskWeightAsync called with parentTaskId: {parentTaskId}");
            if (!parentTaskId.HasValue)
            {
                Console.WriteLine("ParentTaskId is null, stopping weight update.");
                return;
            }

            var task = await _context.ProjectTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == parentTaskId);


            if (task?.ParentTaskId.HasValue == true)
            {
                var parentTask = await _context.ProjectTasks
                    .Include(p => p.SubTasks)
                    .FirstOrDefaultAsync(p => p.Id == task.ParentTaskId);

                if (parentTask != null)
                {
                    Console.WriteLine($"Found parent task with ID {parentTask.Id}, Title: {parentTask.Title}");
                    double totalWeight = parentTask.SubTasks.Sum(sub => sub.Weight);
                    Console.WriteLine($"Total weight of subtasks for parent {parentTask.Id}: {totalWeight}");
                    parentTask.Weight = Math.Min(100, (int)totalWeight); // Assuming weight is an integer
                    _context.Entry(parentTask).State = EntityState.Modified;
                    //_context.ProjectTasks.Update(parentTask);
                    await _context.SaveChangesAsync();

                    // Continue propagating the update up the hierarchy
                    Console.WriteLine($"Calling UpdateParentTaskWeightAsync recursively with parentTaskId: {parentTask.ParentTaskId}");
                    await UpdateParentTaskWeightAsync(parentTask.ParentTaskId);
                }
                else
                {
                    Console.WriteLine($"Parent task with ID {task.ParentTaskId} not found.");
                }
            }
            else
            {
                Console.WriteLine($"Task with ID {task?.Id} has no ParentTaskId, stopping.");
            }

        }
    }
}

