using System.ComponentModel.DataAnnotations;

namespace iDataflow.Backend.Models
{
    public class Workflow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string N8nWorkflowId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key to User (optional - who synced it)
        public int? UserId { get; set; }
        public virtual User? User { get; set; }

        // Navigation properties
        public virtual ICollection<Execution> Executions { get; set; } = new List<Execution>();
    }
}