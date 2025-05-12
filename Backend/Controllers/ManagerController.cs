//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using ProjectManagementSystem1.Data;
//using ProjectManagementSystem1.Model.Entities;

//namespace ProjectManagementSystem1.Controllers
//{
//    public class ManagerController : Controllers
//    {
//        [HttpGet("team")]
//        [Authorize(Policy = "ManagerOnly")]
//        public IActionResult GetTeamOverview()
//        {
//            return Ok("👥 Hello Manager, here’s your team overview.");
//        }
//    }
//}
