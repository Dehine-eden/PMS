using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem1.Data;
using ProjectManagementSystem1.Model.Dto.ProjectManagementDto;
using ProjectManagementSystem1.Model.Entities;
using ProjectManagementSystem1.Services.AccessControl;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProjectManagementSystem1.Services;

namespace ProjectManagementSystem1.Services.ProjectService
{
    public class ProjectService : IProjectService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IProjectAssignmentService _projectAssignmentService;
        private readonly IAccessControlService _accessControlService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityLogService _activityLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        //public ProjectService(AppDbContext context, IMapper mapper, IProjectAssignmentService projectAssignmentService, IAccessControlService accessControlService, UserManager<ApplicationUser> userManager)
        private readonly IProjectApprovalService _projectApprovalService;

        public ProjectService(
            AppDbContext context, 
            IMapper mapper, 
            IProjectAssignmentService projectAssignmentService, 
            IAccessControlService accessControlService, 
            UserManager<ApplicationUser> userManager, 
            IProjectApprovalService projectApprovalService,
            IActivityLogService activityLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _projectAssignmentService = projectAssignmentService;
            _accessControlService = accessControlService;
            _userManager = userManager;
            _projectApprovalService = projectApprovalService;
            _activityLogService = activityLogService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<ProjectDto>> GetAllAsync(string department)
        {
            var projects = await _context.Projects.Where(p => p.Department == department).ToListAsync();
            return _mapper.Map<List<ProjectDto>>(projects);
        }

        public async Task<ProjectDto> GetByIdAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, string currentUser)
        {
            var project = _mapper.Map<Project>(dto);
            var now = DateTime.UtcNow;

            // Set project metadata
            project.CreateUser = currentUser;
            project.CreatedByUserId = currentUser;
            project.CreatedDate = now;
            project.UpdateUser = currentUser;
            project.UpdatedDate = now;

            // Phase 2A: Handle approval workflow
            if (await _projectApprovalService.IsUserManagerOrAboveAsync(currentUser))
            {
                // Manager or above - auto approve
                project.ApprovalStatus = ProjectApprovalStatus.AutoApproved;
                project.Status = "Active";
            }
            else
            {
                // Regular user - needs approval
                project.ApprovalStatus = ProjectApprovalStatus.Pending;
                project.Status = "Pending Approval";
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Log the creation activity
            await _activityLogService.LogActivityWithChangesAsync(
                userId: currentUser,
                entityType: "Project",
                entityId: project.Id,
                actionType: "Created",
                oldValues: null,
                newValues: new {
                    project.ProjectName,
                    project.Description,
                    project.Status,
                    project.Priority,
                    project.Department,
                    project.CreatedDate,
                    project.DueDate
                }
            );

            // Resolve current user object
            var currentUserEntity = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUser);
            if (currentUserEntity == null)
                throw new ArgumentException("Creator user not found.");

            ProjectAssignment projectAssignment;

            // 🧠 Logic for assignment
            if (!string.IsNullOrWhiteSpace(dto.AssignedEmployeeId) && !string.IsNullOrWhiteSpace(dto.AssignedRole))
            {
                var assignedUser = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == dto.AssignedEmployeeId);
                if (assignedUser == null)
                    throw new ArgumentException("Assigned employee not found.");

                // Check if role is ScrumMaster and already exists
                //if (dto.AssignedRole == "ScrumMaster")
                //{
                //    var exists = await _context.ProjectAssignments
                //        .AnyAsync(a => a.ProjectId == project.Id && a.MemberRole == "ScrumMaster");
                //    if (exists)
                //        throw new InvalidOperationException("Project already has a ScrumMaster.");
                //}

                projectAssignment = new ProjectAssignment
                {
                    ProjectId = project.Id,
                    MemberId = assignedUser.Id,
                    MemberRole = dto.AssignedRole,
                    CreateUser = currentUser,
                    CreatedDate = now
                };
            }
            else
            {
                // 👤 Default: assign creator as ScrumMaster
                projectAssignment = new ProjectAssignment
                {
                    ProjectId = project.Id,
                    MemberId = currentUserEntity.Id,
                    MemberRole = "ScrumMaster",
                    CreateUser = currentUser,
                    CreatedDate = now
                };
            }

            _context.ProjectAssignments.Add(projectAssignment);
            await _context.SaveChangesAsync();

            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<bool> UpdateAsync(int id, UpdateProjectDto dto, string currentUser)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return false;

        // Capture old values before updating
            var oldValues = new 
            {
                ProjectName = project.ProjectName,
                Description = project.Description,
                Status = project.Status,
                Priority = project.Priority,
                Department = project.Department,
                CreatedDate = project.CreatedDate,
                DueDate = project.DueDate,
                ProjectOwner = project.ProjectOwner,
                IsArchived = project.IsArchived,
                ArchiveDate = project.ArchiveDate,
                ApprovalStatus = project.ApprovalStatus 
            };

            _mapper.Map(dto, project);
            project.UpdateUser = currentUser;
            project.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Capture new values after updating
            var newValues = new 
            {
                ProjectName = project.ProjectName,
                Description = project.Description,
                Status = project.Status,
                Priority = project.Priority,
                Department = project.Department,
                CreatedDate = project.CreatedDate,
                DueDate = project.DueDate,
                ProjectOwner = project.ProjectOwner,
                IsArchived = project.IsArchived,
                ArchiveDate = project.ArchiveDate,
                ApprovalStatus = project.ApprovalStatus 
            };

            // Log the update activity with old and new values
            await _activityLogService.LogActivityWithChangesAsync(
                userId: currentUser,
                entityType: "Project",
                entityId: project.Id,
                actionType: "Updated",
                oldValues: oldValues,
                newValues: newValues
            );

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return false;

            // Capture values before deleting
            var oldValues = new 
            {
                ProjectName = project.ProjectName,
                Description = project.Description,
                Status = project.Status,
                Priority = project.Priority,
                Department = project.Department,
                CreatedDate = project.CreatedDate,
                DueDate = project.DueDate,
                ProjectOwner = project.ProjectOwner,
                IsArchived = project.IsArchived,
                ArchiveDate = project.ArchiveDate,
                ApprovalStatus = project.ApprovalStatus 
            };

            // Get current user ID from HttpContext
            var currentUserId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            // Log the deletion activity
            await _activityLogService.LogActivityWithChangesAsync(
                userId: currentUserId,
                entityType: "Project",
                entityId: id,
                actionType: "Deleted",
                oldValues: oldValues,
                newValues: null
            );

            return true;
        }

        public async Task ArchiveProjectAsync(int projectId, string currentUser)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new Exception("Project not found");

            // Capture values before archiving
            var oldValues = new 
            {
                IsArchived = project.IsArchived,
                ArchiveDate = project.ArchiveDate
            };

            project.IsArchived = true;
            project.ArchiveDate = DateTime.UtcNow;
            project.UpdatedDate = DateTime.UtcNow;
            project.UpdateUser = currentUser;

            await _context.SaveChangesAsync();

            // Capture values after archiving
            var newValues = new 
            {
                IsArchived = project.IsArchived,
                ArchiveDate = project.ArchiveDate
            };

            // Log the archive activity
            await _activityLogService.LogActivityWithChangesAsync(
                userId: currentUser,
                entityType: "Project",
                entityId: projectId,
                actionType: "Archived",
                oldValues: oldValues,
                newValues: newValues
            );
        }
        
        public async Task<List<ProjectDto>> GetActiveProjectsAsync()
        {
            return await _context.Projects
                .Where(p => !p.IsArchived)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    ProjectName = p.ProjectName,           
                    Description = p.Description,
                    ProjectOwner = p.ProjectOwner,      
                    Priority = p.Priority,
                    Status = p.Status,
                }).ToListAsync();
        }

        public async Task RestoreProjectAsync(int projectId, string currentUser)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new Exception("Project not found");

            if (!project.IsArchived)
                throw new InvalidOperationException("Project is not archived");

            // Capture values before restoring
            var oldValues = new 
            {
                IsArchived = project.IsArchived,
                ArchiveDate = project.ArchiveDate
            };

            project.IsArchived = false;
            project.ArchiveDate = null;
            project.UpdatedDate = DateTime.UtcNow;
            project.UpdateUser = currentUser;

            await _context.SaveChangesAsync();

            // Capture values after restoring
            var newValues = new 
            {
                IsArchived = project.IsArchived,
                ArchiveDate = project.ArchiveDate
            };

            // Log the restore activity
            await _activityLogService.LogActivityWithChangesAsync(
                userId: currentUser,
                entityType: "Project",
                entityId: projectId,
                actionType: "Restored",
                oldValues: oldValues,
                newValues: newValues
            );
        }

        public async Task<List<ProjectDto>> GetAllVisibleAsync(string currentUserId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            if (user == null) return new List<ProjectDto>();

            var roles = await _userManager.GetRolesAsync(user);

            var baseQuery = _context.Projects
                .Include(p => p.ProjectAssignments)
                .AsQueryable();

            var filtered = await _accessControlService.FilterProjectsForUserAsync(baseQuery, user, roles);
            var list = await filtered.ToListAsync();
            return _mapper.Map<List<ProjectDto>>(list);
        }
//         public async Task<(IEnumerable<Project> Projects, int TotalCount)> GetFilteredProjectsAsync(ProjectFilterDto filter)
// {
//     var query = _context.Projects
//         .Include(p => p.CreatedBy)  // Include user data if needed
//         .Include(p => p.AssignedTo) // Include user data if needed
//         .Include(p => p.TeamMembers) // Include team members if needed
//         .AsQueryable();
    
//     // Apply filters
//     if (!string.IsNullOrEmpty(filter.Name))
//         query = query.Where(p => p.Name.Contains(filter.Name));
    
//     if (!string.IsNullOrEmpty(filter.Status))
//         query = query.Where(p => p.Status == filter.Status);
    
//     if (!string.IsNullOrEmpty(filter.Priority))
//         query = query.Where(p => p.Priority == filter.Priority);
    
//     if (filter.StartDateFrom.HasValue)
//         query = query.Where(p => p.StartDate >= filter.StartDateFrom.Value);
    
//     if (filter.StartDateTo.HasValue)
//         query = query.Where(p => p.StartDate <= filter.StartDateTo.Value);
    
//     if (filter.EndDateFrom.HasValue)
//         query = query.Where(p => p.EndDate >= filter.EndDateFrom.Value);
    
//     if (filter.EndDateTo.HasValue)
//         query = query.Where(p => p.EndDate <= filter.EndDateTo.Value);
    
//     if (!string.IsNullOrEmpty(filter.Department))
//         query = query.Where(p => p.Department == filter.Department);
    
//     // User-based filters
//     if (!string.IsNullOrEmpty(filter.CreatedByUserId))
//         query = query.Where(p => p.CreatedByUserId == filter.CreatedByUserId);
    
//     if (!string.IsNullOrEmpty(filter.AssignedToUserId))
//         query = query.Where(p => p.AssignedToUserId == filter.AssignedToUserId);
    
//     if (filter.TeamMemberIds != null && filter.TeamMemberIds.Any())
//         query = query.Where(p => p.TeamMembers.Any(tm => filter.TeamMemberIds.Contains(tm.Id)));
    
//     if (filter.Tags != null && filter.Tags.Any())
//         query = query.Where(p => filter.Tags.Any(t => p.Tags.Contains(t)));
    
//     // Get total count before pagination
//     var totalCount = await query.CountAsync();
    
//     // Apply sorting
//     if (!string.IsNullOrEmpty(filter.SortBy))
//     {
//         query = filter.SortBy.ToLower() switch
//         {
//             "name" => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
//             "startdate" => filter.SortDescending ? query.OrderByDescending(p => p.StartDate) : query.OrderBy(p => p.StartDate),
//             "enddate" => filter.SortDescending ? query.OrderByDescending(p => p.EndDate) : query.OrderBy(p => p.EndDate),
//             "priority" => filter.SortDescending ? query.OrderByDescending(p => p.Priority) : query.OrderBy(p => p.Priority),
//             "createdby" => filter.SortDescending ? query.OrderByDescending(p => p.CreatedBy.UserName) : query.OrderBy(p => p.CreatedBy.UserName),
//             "assignedto" => filter.SortDescending ? query.OrderByDescending(p => p.AssignedTo.UserName) : query.OrderBy(p => p.AssignedTo.UserName),
//             _ => query.OrderBy(p => p.Id)
//         };
//     }
//     else
//     {
//         query = query.OrderBy(p => p.Id);
//     }
    
//     // Apply pagination
//     query = query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize);
    
//     var projects = await query.ToListAsync();
//     return (projects, totalCount);
//         }
   }
}