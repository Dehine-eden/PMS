using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Model.Entities;

namespace ProjectManagementSystem1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("users-by-department/{departmentName}")]
        public async Task<IActionResult> GetUsersByDepartment(string departmentName)
        {
            var users = await _userManager.Users
                .Where(u => u.Department.ToLower() == departmentName.ToLower())
                .Select(u => new
                {
                    u.FullName,
                    u.EmployeeId,
                    u.Email,
                    u.Department,        
                    u.PhoneNumber
                }).ToListAsync();

            return Ok(users);
        }

        [HttpGet("users-by-manager/{manager}")]
        public async Task<IActionResult> GetUsersByManager(string manager)
        {
            var users = await _userManager.Users
                .Where(u => u.Manager.ToLower() == manager.ToLower())
                .Select(u => new
                {
                    u.FullName,
                    u.EmployeeId,
                    u.Email,
                    u.Department,
                    u.PhoneNumber
                }).ToListAsync();

            return Ok(users);
        }


    }
}
