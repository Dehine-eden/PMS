using AutoMapper;
using ProjectManagementSystem1.Model.Dto;
using ProjectManagementSystem1.Model.Dto.ProjectManagementDto;
using ProjectManagementSystem1.Model.Entities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProjectManagementSystem1.Helpers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Project, ProjectDto>();
            CreateMap<CreateProjectDto, Project>();
            CreateMap<UpdateProjectDto, Project>();
        }
    }
}
