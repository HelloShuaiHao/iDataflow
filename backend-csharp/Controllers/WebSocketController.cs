using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iDataflow.Backend.Services;
using iDataflow.Backend.DTOs;
using iDataflow.Backend.Data;

namespace iDataflow.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WebSocketController : ControllerBase
    {
        private readonly WebSocketService _webSocketService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WebSocketController> _logger;

        public WebSocketController(WebSocketService webSocketService, ApplicationDbContext context, ILogger<WebSocketController> logger)
        {
            _webSocketService = webSocketService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            try
            {
                var stats = _webSocketService.GetStats();
                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting WebSocket stats");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to get WebSocket stats"
                });
            }
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            try
            {
                var logs = await _context.WebSocketLogs
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip(offset)
                    .Take(Math.Min(limit, 1000)) // Max 1000 records
                    .ToListAsync();

                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = logs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting WebSocket logs");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to get WebSocket logs"
                });
            }
        }

        [HttpPost("send/{connectionId}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> SendToClient(string connectionId, [FromBody] object message)
        {
            try
            {
                await _webSocketService.SendToClient(connectionId, message);
                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = new { Message = "Message sent successfully" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to client {ConnectionId}", connectionId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to send message"
                });
            }
        }

        [HttpPost("broadcast")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Broadcast([FromBody] object message)
        {
            try
            {
                await _webSocketService.BroadcastToAll(message);
                return Ok(new ApiResponse
                {
                    Success = true,
                    Data = new { Message = "Message broadcasted successfully" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting message");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to broadcast message"
                });
            }
        }
    }
}