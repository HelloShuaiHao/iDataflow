using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace iDataflow.Backend.Models
{
    public class WebSocketLog
    {
        [Key]
        public int Id { get; set; }

        [StringLength(255)]
        public string? ClientId { get; set; }

        [StringLength(50)]
        public string MessageType { get; set; } = string.Empty;

        public string? Payload { get; set; } // JSON string

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}