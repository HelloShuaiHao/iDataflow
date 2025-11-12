using System.ComponentModel.DataAnnotations;

namespace iDataflow.Backend.Models
{
    public class Execution
    {
        [Key]
        public int Id { get; set; }

        // Foreign key to Workflow
        [Required]
        public int WorkflowId { get; set; }
        public virtual Workflow Workflow { get; set; } = null!;

        [StringLength(255)]
        public string? N8nExecutionId { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = string.Empty; // success, failed, running

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}