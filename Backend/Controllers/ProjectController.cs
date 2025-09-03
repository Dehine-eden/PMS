using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.ProjectManagementDto;
using ProjectManagementSystem1.Services.ProjectService;
using System.Security.Claims;
using ProjectManagementSystem1.Services;
using System;
using System.Threading.Tasks;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // only authenticated users access the endpoints
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly AppDbContext _context;

        public ProjectController(IProjectService projectService, AppDbContext context)
        {
            _projectService = projectService;
            _context = context;
        }

        [HttpGet("All-projects")]
        [Authorize(Roles = "Admin,President,Vice-President,Director,Manager,Supervisor")] // broader visibility controllers; filtering still applied in service
        public async Task<IActionResult> GetAll()
        {
            // var userDept = User.FindFirst("Department")?.Value;
            // if (userDept == null) return Forbid();

            // var projects = await _projectService.GetAllAsync(userDept);
            // return Ok(projects);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Forbid();

            var projects = await _projectService.GetAllVisibleAsync(userId);
            return Ok(projects);
        }

        [HttpGet("project-byId")]
        public async Task<IActionResult> GetById(int id)
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null)
                return NotFound();
            return Ok(project);
        }

        //Get All Projects by Priority
        [HttpGet("project-by-priority/{priority}")]
        public async Task<IActionResult> GetProjectsByPriority(string priority)
        {
            var projects = await _context.Projects
                .Where(p => p.Priority.ToLower() == priority.ToLower())
                .ToListAsync();

            return Ok(projects);
        }

        [HttpPost("create-project")]
        //[Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Unauthorized()
;
            var created = await _projectService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("edit-project")]
        //[Authorize(Roles = "Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var updated = await _projectService.UpdateAsync(id, dto, username);
            if (updated == null)
                return NotFound();
            return Ok(updated);
        }

        [HttpDelete("delete-project")]
        //[Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _projectService.DeleteAsync(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }

        [HttpPost("archive/{id}")]
        public async Task<IActionResult> ArchiveProject(int id)
        {
            var currentUser = User.Identity?.Name; // or get from claims

            await _projectService.ArchiveProjectAsync(id, currentUser);

            return Ok(new { Message = "Project archived successfully" });
        }

        [HttpPost("restore/{id}")]
        public async Task<IActionResult> RestoreProject(int id)
        {
            var currentUser = User.Identity?.Name;

            try
            {
                await _projectService.RestoreProjectAsync(id, currentUser);
                return Ok(new { Message = "Project restored successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
//         [HttpGet]
//         public async Task<IActionResult> GetProjects(
//             [FromQuery] string name, 
//             [FromQuery] string status, 
//             [FromQuery] string priority,
//             [FromQuery] DateTime? startDateFrom,
//             [FromQuery] DateTime? startDateTo,
//             [FromQuery] DateTime? endDateFrom,
//             [FromQuery] DateTime? endDateTo,
//             [FromQuery] string createdByUserId,
//             [FromQuery] string assignedToUserId,
//             [FromQuery] int page = 1,
//             [FromQuery] int pageSize = 10,
//             [FromQuery] string sortBy = "id",
//             [FromQuery] bool sortDescending = false,
//             [FromHeader(Name = "X-Department")] string department = null)
// {
//     var filter = new ProjectFilterDto
//     {
//         Name = name,
//         Status = status,
//         Priority = priority,
//         StartDateFrom = startDateFrom,
//         StartDateTo = startDateTo,
//         EndDateFrom = endDateFrom,
//         EndDateTo = endDateTo,
//         CreatedByUserId = createdByUserId,
//         AssignedToUserId = assignedToUserId,
//         Department = department,
//         Page = page,
//         PageSize = pageSize,
//         SortBy = sortBy,
//         SortDescending = sortDescending
//     };
    
//     var (projects, totalCount) = await _projectService.GetFilteredProjectsAsync(filter);
    
//     Response.Headers.Add("X-Total-Count", totalCount.ToString());
//     Response.Headers.Add("X-Page", page.ToString());
//     Response.Headers.Add("X-Page-Size", pageSize.ToString());
//     Response.Headers.Add("X-Total-Pages", Math.Ceiling((double)totalCount / pageSize).ToString());
    
//     return Ok(projects);
// }
// [HttpPost("search")]
//         public async Task<IActionResult> SearchProjects([FromBody] ProjectFilterDto filter)
// {
//     // Optionally extract header values if needed
//     if (string.IsNullOrEmpty(filter.Department))
//         filter.Department = Request.Headers["X-Department"].FirstOrDefault();
    
//     var (projects, totalCount) = await _projectService.GetFilteredProjectsAsync(filter);
    
//     Response.Headers.Add("X-Total-Count", totalCount.ToString());
//     Response.Headers.Add("X-Page", filter.Page.ToString());
//     Response.Headers.Add("X-Page-Size", filter.PageSize.ToString());
//     Response.Headers.Add("X-Total-Pages", Math.Ceiling((double)totalCount / filter.PageSize).ToString());
    
//     return Ok(projects);
// }


        //[HttpGet("active")]
        //public async Task<IActionResult> GetActiveProjects()
        //{
        //    var projects = await _projectService.GetActiveProjectsAsync();
        //    return Ok(projects);
        //}



    }
}
