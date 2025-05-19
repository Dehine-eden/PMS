using AutoMapper;
using ProjectManagementSystem1.Controllers;
using ProjectManagementSystem1.Model.Entities;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using ProjectManagementSystem1.Model.Dto;
using Microsoft.AspNetCore.Identity.Data;

namespace ProjectManagementSystem1.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Project mappings
            CreateMap<Project, ProjectDto>().ReverseMap();
            CreateMap<Project, CreateProjectDto>().ReverseMap();
            CreateMap<Project, UpdateProjectDto>().ReverseMap();

            // ProjectTask mappings
            CreateMap<ProjectTask, ProjectTaskDto>().ReverseMap();
            CreateMap<ProjectTask, CreateProjectTaskDto>().ReverseMap();
            CreateMap<ProjectTask, UpdateProjectTaskDto>().ReverseMap();

            // ProjectAssignment mappings
            CreateMap<ProjectAssignment, ProjectAssignmentDto>().ReverseMap();
            CreateMap<ProjectAssignment, CreateProjectAssignmentDto>().ReverseMap();
            CreateMap<ProjectAssignment, UpdateProjectAssignmentDto>().ReverseMap();

            // User mappings
            CreateMap<User, LoginRequestDto>().ReverseMap();
            CreateMap<User, RegisterUserDto>().ReverseMap();
            // CreateMap<User, UpdateUserDto>().ReverseMap();
        }
    }
}
