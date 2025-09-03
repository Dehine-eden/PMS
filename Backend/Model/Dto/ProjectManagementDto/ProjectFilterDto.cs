// using Microsoft.AspNetCore.Mvc;
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using ProjectManagementSystem1.Model.Dto.ProjectManagementDto;
// using ProjectManagementSystem1.Services;

// namespace ProjectManagementSystem1.Model.Dto.ProjectManagementDto
// {
// public class ProjectFilterDto
// {
//     // Query parameters
//     public string? Name { get; set; }
//     public string? Status { get; set; }
//     public string? Priority { get; set; }
//     public DateTime? StartDateFrom { get; set; }
//     public DateTime? StartDateTo { get; set; }
//     public DateTime? EndDateFrom { get; set; }
//     public DateTime? EndDateTo { get; set; }
    
//     // Header parameters
//     [FromHeader]
//     public string? Department { get; set; }
    
//     // User-related filters
//     public string? CreatedByUserId { get; set; }
//     public string? AssignedToUserId { get; set; }
//     public List<string>? TeamMemberIds { get; set; }
    
//     // Body parameters
//     public List<string>? Tags { get; set; }
    
//     // Pagination
//     public int Page { get; set; } = 1;
//     public int PageSize { get; set; } = 20;
    
//     // Sorting
//     public string? SortBy { get; set; }
//     public bool SortDescending { get; set; } = false;
    
//     }
// }