using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using iDataflow.Backend.Services;
using iDataflow.Backend.DTOs;

namespace iDataflow.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WorkflowsController : ControllerBase
    {
        private readonly N8nService _n8nService;
        private readonly ILogger<WorkflowsController> _logger;

        public WorkflowsController(N8nService n8nService, ILogger<WorkflowsController> logger)
        {
            _n8nService = n8nService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkflows()
        {
            try
            {
                var result = await _n8nService.GetWorkflowsAsync();
                
                if (result.Success)
                {
                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Data = result.Data
                    });
                }
                else
                {
                    return StatusCode(503, new ApiResponse
                    {
                        Success = false,
                        Error = result.Error
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching workflows");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to fetch workflows"
                });
            }
        }

        [HttpGet("{workflowId}")]
        public async Task<IActionResult> GetWorkflow(string workflowId)
        {
            try
            {
                var result = await _n8nService.GetWorkflowAsync(workflowId);
                
                if (result.Success)
                {
                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Data = result.Data
                    });
                }
                else
                {
                    return StatusCode(503, new ApiResponse
                    {
                        Success = false,
                        Error = result.Error
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching workflow {WorkflowId}", workflowId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to fetch workflow"
                });
            }
        }

        [HttpPost("{workflowId}/execute")]
        public async Task<IActionResult> ExecuteWorkflow(string workflowId, [FromBody] object? data = null)
        {
            try
            {
                var result = await _n8nService.ExecuteWorkflowAsync(workflowId, data);
                
                if (result.Success)
                {
                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Data = result.Data
                    });
                }
                else
                {
                    return StatusCode(503, new ApiResponse
                    {
                        Success = false,
                        Error = result.Error
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to execute workflow"
                });
            }
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestN8nConnection()
        {
            try
            {
                var result = await _n8nService.TestConnectionAsync();
                
                if (result.Success)
                {
                    return Ok(new ApiResponse
                    {
                        Success = true,
                        Data = result.Data
                    });
                }
                else
                {
                    return StatusCode(503, new ApiResponse
                    {
                        Success = false,
                        Error = result.Error
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing n8n connection");
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Error = "Failed to test n8n connection"
                });
            }
        }
    }
}