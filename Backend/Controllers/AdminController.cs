using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Model.Dto.UserManagementDto;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Services;

namespace ProjectManagementSystem1.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IADService _adService; // Service for Active Directory fetch
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, IADService adService, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _adService = adService;
            _roleManager = roleManager;
        }


        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _adService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.EmployeeId))
                return BadRequest(new { message = "Employee ID is required." });

            // Try fetching user from AD
            var adUser = await _adService.GetUserByEmployeeIdAsync(request.EmployeeId);

            ApplicationUser newUser;

            if (adUser == null)
            {
                // No AD record found, require manual entry
                if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
                    return NotFound(new { message = "User not found in Active Directory. Please enter full details manually." });

                newUser = new ApplicationUser
                {
                    FullName = request.FullName,
                    UserName = request.Username,
                    Email = request.Email,
                    Department = request.Department,
                    EmployeeId = request.EmployeeId,
                    PhoneNumber = request.PhoneNumber,
                    Company = request.Company,
                    Title = request.Title,
                    Status = "Active",
                    IsFirstLogin = true
                };
            }
            else
            {
                // AD record found, autofill details
                newUser = new ApplicationUser
                {
                    FullName = adUser.FullName,
                    UserName = adUser.Username,
                    Email = adUser.Email,
                    Department = adUser.Department,
                    EmployeeId = adUser.EmployeeId,
                    PhoneNumber = adUser.PhoneNumber,
                    Company = adUser.Company,
                    Title = adUser.Title,
                    Status = "Active",
                    IsFirstLogin = true
                };
            }

            // Create user with default password
            var result = await _userManager.CreateAsync(newUser, "Welcome2cbe");

            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            // Assign role (Admin manually selects)
            if (!await _roleManager.RoleExistsAsync(request.Role))
                return BadRequest(new { message = "Selected role does not exist." });

            await _userManager.AddToRoleAsync(newUser, request.Role);

            return Ok(new { message = "User created successfully.", userId = newUser.Id });
        }
    }
}
