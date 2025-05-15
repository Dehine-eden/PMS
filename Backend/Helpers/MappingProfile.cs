using AutoMapper;
using ProjectManagementSystem1.Model.Dto;
using ProjectManagementSystem1.Model.Dto.ProjectAssignmentDto;
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
            CreateMap<CreateAssignmentDto, ProjectAssignment>();
            CreateMap<UpdateAssignmentDto, ProjectAssignment>();
            CreateMap<ProjectAssignment, AssignmentDto>()
                //.ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project.ProjectName))
                //.ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Project.Priority))
                //.ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.Project.DueDate))
                .ForMember(dest => dest.MemberFullName, opt => opt.MapFrom(src => src.Member.FullName))
                .ForMember(dest => dest.MemberId, opt => opt.MapFrom(src => src.Member.EmployeeId));

        }
    }
}
