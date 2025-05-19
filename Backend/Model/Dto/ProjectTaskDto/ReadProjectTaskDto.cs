namespace ProjectManagementSystem1.Model.Dtos
{
public class ReadProjectTaskDto
{
    public int Id { get; set; }
    public required string ProjectName { get; set; }
    public required string ProjectOwner { get; set; }
    public required string Status { get; set; }
    public DateTime? ProjectDueDate { get; set; }
    public byte[]? Version { get; set; } 
}
}
