using Microsoft.AspNetCore.Mvc;
using ProjectManagementSystem1.Model.Dto.ADDto;
using ProjectManagementSystem1.Services.ADService;

namespace ProjectManagementSystem1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ADAuthController : ControllerBase
    {
        private readonly IADAuthService _adAuthService;

        public ADAuthController(IADAuthService adAuthService)
        {
            _adAuthService = adAuthService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequestDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
                return BadRequest("Username and password are required.");

            var result = _adAuthService.Authenticate(loginDto.Username, loginDto.Password);
            if (!result)
                return Unauthorized("Invalid credentials.");

            var userInfo = _adAuthService.GetEmployee(loginDto.Username);
            return Ok(userInfo);
        }

        [HttpGet("employee/{empId}")]
        public ActionResult<EmployeeADDto> GetEmployee(string empId)
        {
            try
            {
                var employee = _adAuthService.GetEmployee(empId);
                return Ok(employee);
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }
    }
}
