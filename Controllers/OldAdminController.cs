//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.DotNet.Scaffolding.Shared.Messaging;
//using Microsoft.EntityFrameworkCore;
//using ProjectManagementSystem1.Data;
//using ProjectManagementSystem1.Model.Dto;
//using ProjectManagementSystem1.Model.Entities;
//using ProjectManagementSystem1.Services;

//namespace ProjectManagementSystem1.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class AdminController : Controller
//    {
//        private readonly IAuthService _authService;
//        private readonly IADService _adService;

//        public AdminController(IAuthService authService, IADService adService)
//        {
//            _authService = authService;
//            _adService = adService;
//        }

//        [HttpGet("dashboard")]
//        [Authorize(Policy = "AdminOnly")]
//        public IActionResult GetAdminDashboard()
//        {
//            return Ok("🔐 Welcome, Admin! Here's the dahsboard");
//        }

//        //[HttpPost("create-user")]
//        //[Authorize(Policy = "AdminOnly")]
//        //public async Task<IActionResult> CreateUser(UserCreateRequest request)
//        //{
//        //    if (string.IsNullOrWhiteSpace(request?.Identifier))
//        //        return BadRequest("Identifier is required.");
//        //    // Try to fetch user from AD
//        //    var adUser = await _adService.GetUserAsync(request.Identifier);
//        //    if (adUser == null)
//        //        return NotFound(new { Message = "User not found in AD. Please enter details manually." });
//        //    // Use AD info if available, otherwise fall back to manual input
//        //    else
//        //    {
//        //        var userDto = new RegisterUserDto
//        //        {
//        //            FullName = adUser?.FullName ?? request.FullName ?? "",
//        //            Username = adUser?.Username ?? request.Username ?? request.Identifier,
//        //            Email = adUser?.Email ?? request.Email ?? "",
//        //            Department = adUser?.Department ?? request.Department ?? "",
//        //            Role = adUser?.Role ?? request.Role ?? "",
//        //        };

//        //        var user = await _authService.RegisterAsync(userDto);
//        //        return Ok(user);
//        //    }
//        //}
        

//        [HttpGet("all-users")]
//        [Authorize(Policy = "AdminOrManager")]
//        public IActionResult GetAllUsers()
//        {
//            return Ok("📋 Admin or Manager can access this endpoint.");
//        }



//    }
//}
