﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Model.Dto.ProjectManagementDto;
using ProjectManagementSystem1.Services;
using System.Security.Claims;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Ensure only authenticated users access the endpoints
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        // GET: api/project
        [HttpGet("All-projects")]
        public async Task<IActionResult> GetAll()
        {
            var projects = await _projectService.GetAllAsync();
            return Ok(projects);
        }

        // GET: api/project/{id}
        [HttpGet("project-byId")]
        public async Task<IActionResult> GetById(int id)
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null)
                return NotFound();
            return Ok(project);
        }

        // POST: api/project
        [HttpPost("create-project")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var created = await _projectService.CreateAsync(dto, username);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/project/{id}
        [HttpPut("edit-project")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto)
        {
            var username = User.FindFirstValue(ClaimTypes.Name);
            var updated = await _projectService.UpdateAsync(id, dto, username);
            if (updated == null)
                return NotFound();
            return Ok(updated);
        }

        // DELETE: api/project/{id}
        [HttpDelete("delete-project")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _projectService.DeleteAsync(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }
    }
}
