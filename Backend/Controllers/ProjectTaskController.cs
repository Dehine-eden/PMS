using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using ProjectManagementSystem1.Model.Dto;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Services;
using System.Threading.Tasks;


namespace ProjectManagementSystem1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require authentication
    public class ProjectTaskController : ControllerBase
    {
        private readonly IProjectTaskService _projectTaskService;

        public ProjectTaskController(IProjectTaskService projectTaskService)
        {
            _projectTaskService = projectTaskService;
        }

        [Authorize(Policy = "SupervisorOnly")]
        [HttpPost("create-task")]
        public async Task<IActionResult> CreateTask([FromBody] ProjectTaskCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var task = await _projectTaskService.CreateTaskAsync(dto);
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var subtask = await _projectTaskService.AddSubtaskAsync(parentTaskId, dto);
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
                Priority = task.Priority,
                Status = task.Status,
                EstimatedHours = task.EstimatedHours,
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
        [HttpGet("{id}")]
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
        [HttpGet]
        public async Task<IActionResult> GetAllTasks()
        {
            var tasks = await _projectTaskService.GetAllTasksAsync();
            return Ok(tasks.Select(MapToResponseDto));
        }

        // DELETE: api/ProjectTask/{id}
        [HttpDelete("{id}")]
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









//[HttpPost("create-task")]
//public async Task<IActionResult> CreateTask([FromBody] ProjectTaskCreateDto dto)
//{
//    if (!ModelState.IsValid)
//        return BadRequest(ModelState);

//    try
//    {
//        var result = await _projectTaskService.CreateTaskAsync(dto);
//        return CreatedAtAction(nameof(GetByTaskId), new { id = result.Id }, result);
//    }
//    catch (ArgumentException ex)
//    {
//        return BadRequest(new { error = ex.Message });
//    }

//}

//// GET: api/ProjectTask/{id}
//[HttpGet("get-by-id")]
//public async Task<IActionResult> GetByTaskId(int id)
//{
//    var task = await _projectTaskService.GetTaskByIdAsync(id);
//    if (task == null)
//        return NotFound();

//    return Ok(task);
//}

//// GET: api/ProjectTask
//[HttpGet("get-all")]
//public async Task<IActionResult> GetAllTasks()
//{
//    var tasks = await _projectTaskService.GetAllTasksAsync();
//    return Ok(tasks);
//}

//// PUT: api/ProjectTask/{id}
//[HttpPut("update-task")]
//public async Task<IActionResult> UpdateTask(int id, [FromBody] ProjectTaskUpdateDto dto)
//{
//    if (!ModelState.IsValid)
//        return BadRequest(ModelState);

//    try
//    {
//        var result = await _projectTaskService.UpdateTaskAsync(id, dto);
//        if (result == null)
//            return NotFound();

//        return Ok(result);
//    }
//    catch (ArgumentException ex)
//    {
//        return BadRequest(new { error = ex.Message });
//    }



//    // 204 No Content
//}

//// DELETE: api/ProjectTask/{id}
//[HttpDelete("delete-task")]
//public async Task<IActionResult> Delete(int id)
//{
//    var result = await _projectTaskService.DeleteTaskAsync(id);
//    if (!result)
//        return NotFound();

//    return NoContent();
//}