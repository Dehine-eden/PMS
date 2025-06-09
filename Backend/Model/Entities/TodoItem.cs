using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementSystem1.Model.Entities
{
    public enum TodoItemStatus { Pending, Accepted, Rejected }

    public class TodoItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectTaskId { get; set; }

        [ForeignKey("ProjectTaskId")]
        public ProjectTask ProjectTask { get; set; }

        [Required, MaxLength(250)]
        public string Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required, Range(0, 100)]
        public int Weight { get; set; }

        [Required, Range(0, 100)]
        public double Progress { get; set; } = 0;

        // Add any other properties that might be relevant for a todo item
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public TodoItemStatus Status { get; set; } = TodoItemStatus.Pending;
    }
}
