using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Model.Dto.ProjectAssignmentDto;
using ProjectManagementSystem1.Services;
using System.Security.Claims;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Manager")]
    public class ProjectAssignmentController : ControllerBase
    {
        private readonly IProjectAssignmentService _assignmentService;

        public ProjectAssignmentController(IProjectAssignmentService assignmentService)
        {
            _assignmentService = assignmentService;
        }

        // GET: Show all members of a project
        [HttpGet("All-members")]
        public async Task<IActionResult> GetByProject(int projectId)
        {
            var assignments = await _assignmentService.GetAllByProjectAsync(projectId);
            return Ok(assignments);
        }

        // GET: Show all projects assigned for a user
        [HttpGet("User-projects")]
        [Authorize] // Optional: Adjust roles as needed
        public async Task<IActionResult> GetProjectsByEmployeeId(string employeeId)
        {
            try
            {
                var result = await _assignmentService.GetProjectsByEmployeeIdAsync(employeeId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // GET: View member using Id
        [HttpGet("member-byId")]
        public async Task<IActionResult> GetById(int id)
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null) return NotFound();
            return Ok(assignment);
        }

        // POST: Add members to the project
        [HttpPost("Add-members")]
        public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name);
            var result = await _assignmentService.CreateAsync(dto, currentUser);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT: Edit role of the project members
        [HttpPut("edit-role")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAssignmentDto dto)
        {
            var currentUser = User.FindFirstValue(ClaimTypes.Name);
            var updated = await _assignmentService.UpdateAsync(id, dto, currentUser);
            if (!updated) return NotFound();
            return Ok("Assignment updated.");
        }

        // DELETE: Delete member from the project
        [HttpDelete("delete-member")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _assignmentService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
