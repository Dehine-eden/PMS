﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using ProjectManagementSystem1.Model.Dto;
using ProjectManagementSystem1.Model.Dto.ProjectTaskDto;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Services;
using System.Security.Claims;
using System.Threading.Tasks;


namespace ProjectManagementSystem1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication
    public class ProjectTaskController : ControllerBase
    {
        private readonly IProjectTaskService _projectTaskService;
        private readonly ICommentService _commentService;
        private readonly INotificationService _notification;
        public ProjectTaskController(IProjectTaskService projectTaskService, ICommentService commentService)
        {
            _projectTaskService = projectTaskService;
            _commentService = commentService;
        }

        /// GET: api/ProjectTask/notifications
        [HttpGet("notifications")]
        public async Task<IActionResult> GetUserNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var notifications = await _notification.GetNotificationsForUserAsync(userId);
            return Ok(notifications);
        }
        [Authorize(Policy = "SupervisorOnly")]
        [HttpPost("create-task")]
        public async Task<IActionResult> CreateTask([FromBody] ProjectTaskCreateDto dto)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var task = await _projectTaskService.CreateTaskAsync(dto, memberId);
                return Ok(MapToResponseDto(task));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/ProjectTask/{parentTaskId}/add-subtask
        [Authorize(Policy = "SupervisorOnly")] // Example: Specific policy for creation
        [HttpPost("{parentTaskId}/add-subtask")]
        public async Task<IActionResult> AddSubtask(int parentTaskId, [FromBody] ProjectTaskCreateDto dto)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var subtask = await _projectTaskService.AddSubtaskAsync(parentTaskId, dto, memberId);
                return Ok(MapToResponseDto(subtask)); // Or CreatedAtAction
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            // catch (Exception ex) { /* Log error */ return StatusCode(500, "An unexpected error occurred."); }
        }

        private ProjectTaskReadDto MapToResponseDto(ProjectTask task)
        {
            if (task == null) return null;

            return new ProjectTaskReadDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                ProjectAssignmentId = task.ProjectAssignmentId,
                AssignedMemberId = task.AssignedMemberId,
                ParentTaskId = task.ParentTaskId,
                Depth = task.Depth,
                IsLeaf = task.IsLeaf,
                Progress = task.Progress,
                Weight = task.Weight,
                //Priority = task.Priority,
                Status = task.Status,
                EstimatedHours = task.EstimatedHours,
                ActualHours = task.ActualHours,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                SubTasks = task.SubTasks.Select(MapToResponseDto).ToList()
            };
        }

        [HttpPut("{taskId}/assign/{memberId}")]
        public async Task<IActionResult> AssignTask(int taskId, string memberId)
        {
            try
            {
                await _projectTaskService.AssignTaskAsync(taskId, memberId);
                return Ok(); // Or a more descriptive response
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (NotFoundException) // Assuming you might have a custom NotFoundException
            {
                return NotFound();
            }
        }

        // GET: api/ProjectTask/{id}
        [HttpGet("Get-task-by-id/{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _projectTaskService.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            return Ok(MapToResponseDto(task));
        }

        // GET: api/ProjectTask
        [HttpGet("Get-all-tasks")]
        public async Task<IActionResult> GetAllTasks()
        {
            var tasks = await _projectTaskService.GetAllTasksAsync();
            return Ok(tasks.Select(MapToResponseDto));
        }

        [HttpPut("update-task/{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] ProjectTaskUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(memberId))
                return Unauthorized();

            bool isSupervisor = User.IsInRole("Supervisor");

            var updatedTask = await _projectTaskService.UpdateTaskAsync(id, memberId, dto, isSupervisor);
            if (updatedTask == null)
            {
                return NotFound();
            }

            return Ok(MapToResponseDto(updatedTask));
        }

        [HttpPut("{taskId}/accept")]
        public async Task<IActionResult> AcceptTask(int taskId)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(memberId))
            {
                return Unauthorized();
            }

            try
            {
                await _projectTaskService.AcceptTaskAsync(taskId, memberId);
                return Ok();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{taskId}/reject")]
        public async Task<IActionResult> RejectTask(int taskId, [FromBody] RejectTaskDto rejectDto)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(rejectDto?.Reason))
            {
                return BadRequest("Rejection reason is required.");
            }

            try
            {
                await _projectTaskService.RejectTaskAsync(taskId, memberId, rejectDto.Reason);
                return Ok();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{taskId}/comments")]
        public async Task<IActionResult> AddComment(int taskId, [FromBody] AddCommentDto commentDto)
        {
            var memeberId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(memeberId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _commentService.AddCommentAsync(taskId, memeberId, commentDto.Content);
                return Ok();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
        }
        [HttpPut("{taskId}/progress")]
        public async Task<IActionResult> UpdateTaskProgress(int taskId, [FromBody] UpdateTaskProgressDto progressDto)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(memberId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _projectTaskService.UpdateTaskProgressAsync(taskId, memberId, progressDto.Progress);
                return Ok();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{taskId}/comments")]
        public async Task<IActionResult> GetComments(int taskId)
        {
            var comments = await _commentService.GetCommentsByTaskIdAsync(taskId);
            return Ok(comments);
        }

        [HttpPut("{taskId}/actual-hours")]
        public async Task<IActionResult> UpdateTaskActualHours(int taskId, [FromBody] UpdateTaskActualHoursDto actualHoursDto)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(memberId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _projectTaskService.UpdateTaskActualHoursAsync(taskId, memberId, actualHoursDto.ActualHours);
                return Ok();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // DELETE: api/ProjectTask/{id}
        [HttpDelete("Delete-task")]
        [Authorize(Policy = "SupervisorOnly")] // Example policy for deletion
        public async Task<IActionResult> DeleteTask(int id)
        {
            var deleted = await _projectTaskService.DeleteTaskAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent(); // Or Ok() with a success message
        }

    }
}

