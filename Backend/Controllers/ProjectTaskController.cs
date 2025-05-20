using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Model.Dto.ProjectTaskDto;
using ProjectManagementSystem1.Services;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Supervisor")]

    public class ProjectTaskController : ControllerBase
    {
        private readonly IProjectTaskService _projectTaskService;

        public ProjectTaskController(IProjectTaskService projectTaskService)
        {
            _projectTaskService = projectTaskService;
        }

        // GET: api/ProjectTask
        [HttpGet("GetAll")]

        public async Task<IActionResult> GetAll()
        {
            var tasks = await _projectTaskService.GetAllAsync();
            return Ok(tasks);
        }

        // GET: api/ProjectTask/{id}
        [HttpGet("GetById")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var task = await _projectTaskService.GetByIdAsync(id);
            if (task == null)
                return NotFound();

            return Ok(task);
        }

        // POST: api/ProjectTask
        [HttpPost("create-task")]
        public async Task<IActionResult> Create([FromBody] CreateProjectTaskDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //var createdTask = await _projectTaskService.CreateAsync(createDto);
            //return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
            try
            {
                var createdTask = await _projectTaskService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = createdTask.Id }, createdTask);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //    // PUT: api/ProjectTask/{id}
        //    [HttpPut("update-task")]
        //    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectTaskDto updateDto)
        //    {
        //        if (id != updateDto.Id)
        //            return BadRequest("ID mismatch.");

        //        if (!ModelState.IsValid)
        //            return BadRequest(ModelState);
        //        try
        //{
        //            var result = await _projectTaskService.UpdateAsync(updateDto);
        //            if (!result)
        //                return NotFound();

        //            return NoContent();
        //        }
        //        catch (InvalidOperationException ex)
        //        {
        //            return BadRequest(ex.Message);
        //        }

        //        //var result = await _projectTaskService.UpdateAsync(updateDto);
        //        //if (!result)
        //        //    return NotFound();

        //        //return NoContent();
        //    }

        // Change the route to include {id}
        [HttpPut("update-task")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectTaskDto updateDto)
        {
            if (id != updateDto.Id)
                return BadRequest("ID mismatch.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _projectTaskService.UpdateAsync(updateDto);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/ProjectTask/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _projectTaskService.DeleteAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }

}
