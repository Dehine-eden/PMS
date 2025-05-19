namespace ProjectManagementSystem1.Model.Dtos
{
public class ReadProjectDto
{
    public int Id { get; set; }
    public required string ProjectName { get; set; }
    public required string ProjectOwner { get; set; }
    public required string Status { get; set; }
    public required string Description { get; set; }
    public DateTime? ProjectDueDate { get; set; }
}
}
