using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Model.Dto.IndependentTaskDto;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Services;
using AutoMapper;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/independent-tasks")]
    [Authorize]
    public class IndependentTaskController : ControllerBase
    {
        private readonly IIndependentTaskService _independentTaskService;
        private readonly IMapper _mapper;

        public IndependentTaskController(
            IIndependentTaskService independentTaskService,
            IMapper mapper)
        {
            _independentTaskService = independentTaskService;
            _mapper = mapper;
        }

        [HttpGet("{id}", Name = "GetIndependentTask")]
        public async Task<ActionResult<IndependentTaskReadDto>> GetIndependentTaskById(int id)
        {
            var task = await _independentTaskService.GetIndependentTaskByIdAsync(id);
            if (task == null) return NotFound();

            return Ok(_mapper.Map<IndependentTaskReadDto>(task));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IndependentTaskReadDto>>> GetAllIndependentTasks()
        {
            var tasks = await _independentTaskService.GetAllIndependentTasksAsync();
            return Ok(_mapper.Map<List<IndependentTaskReadDto>>(tasks));
        }

        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<IndependentTaskReadDto>>> GetIndependentTasksByUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var tasks = await _independentTaskService.GetIndependentTasksByUserAsync(userId);
            return Ok(_mapper.Map<List<IndependentTaskReadDto>>(tasks));
        }

        [HttpPost]
        public async Task<ActionResult<IndependentTaskReadDto>> CreateIndependentTask(
            [FromBody] IndependentTaskCreateDto createDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var task = _mapper.Map<IndependentTask>(createDto);
            task.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var createdTask = await _independentTaskService.CreateIndependentTaskAsync(task);
            var readDto = _mapper.Map<IndependentTaskReadDto>(createdTask);

            return CreatedAtRoute(
                "GetIndependentTask",
                new { id = readDto.TaskId },
                readDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIndependentTask(int id, [FromBody] IndependentTaskUpdateDto updateDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != updateDto.TaskId) return BadRequest("ID mismatch");

            // Map DTO to entity
            var taskUpdates = _mapper.Map<IndependentTask>(updateDto);

            // Perform update
            var updatedTask = await _independentTaskService.UpdateIndependentTaskAsync(id, taskUpdates);

            if (updatedTask == null) return NotFound();

            return Ok(_mapper.Map<IndependentTaskReadDto>(updatedTask));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIndependentTask(int id)
        {
            var success = await _independentTaskService.DeleteIndependentTaskAsync(id);
            return success ? NoContent() : NotFound();
        }
    }
}