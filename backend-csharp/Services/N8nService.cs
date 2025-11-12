using System.Text;
using System.Text.Json;
using iDataflow.Backend.Models;

namespace iDataflow.Backend.Services
{
    public class N8nService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string? _apiKey;
        private readonly ILogger<N8nService> _logger;

        public N8nService(HttpClient httpClient, IConfiguration configuration, ILogger<N8nService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["N8N:BaseUrl"] ?? "http://localhost:5678";
            _apiKey = configuration["N8N:ApiKey"];

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-N8N-API-KEY", _apiKey);
            }
        }

        public async Task<ApiResponse<List<N8nWorkflow>>> GetWorkflowsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching workflows from n8n...");
                var response = await _httpClient.GetAsync("/api/v1/workflows");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<N8nApiResponse<List<N8nWorkflow>>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return new ApiResponse<List<N8nWorkflow>>
                    {
                        Success = true,
                        Data = apiResponse?.Data ?? new List<N8nWorkflow>()
                    };
                }
                else
                {
                    _logger.LogError("Failed to fetch workflows: {StatusCode}", response.StatusCode);
                    return new ApiResponse<List<N8nWorkflow>>
                    {
                        Success = false,
                        Error = $"Failed to fetch workflows: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching workflows from n8n");
                return new ApiResponse<List<N8nWorkflow>>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResponse<N8nWorkflow>> GetWorkflowAsync(string workflowId)
        {
            try
            {
                _logger.LogInformation("Fetching workflow {WorkflowId} from n8n...", workflowId);
                var response = await _httpClient.GetAsync($"/api/v1/workflows/{workflowId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var workflow = JsonSerializer.Deserialize<N8nWorkflow>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return new ApiResponse<N8nWorkflow>
                    {
                        Success = true,
                        Data = workflow
                    };
                }
                else
                {
                    _logger.LogError("Failed to fetch workflow {WorkflowId}: {StatusCode}", workflowId, response.StatusCode);
                    return new ApiResponse<N8nWorkflow>
                    {
                        Success = false,
                        Error = $"Failed to fetch workflow: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching workflow {WorkflowId} from n8n", workflowId);
                return new ApiResponse<N8nWorkflow>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResponse<object>> ExecuteWorkflowAsync(string workflowId, object? data = null)
        {
            try
            {
                _logger.LogInformation("Executing workflow {WorkflowId}...", workflowId);
                
                var json = JsonSerializer.Serialize(data ?? new { });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"/api/v1/workflows/{workflowId}/execute", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<object>(responseContent);

                    return new ApiResponse<object>
                    {
                        Success = true,
                        Data = result
                    };
                }
                else
                {
                    _logger.LogError("Failed to execute workflow {WorkflowId}: {StatusCode}", workflowId, response.StatusCode);
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Failed to execute workflow: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow {WorkflowId}", workflowId);
                return new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<ApiResponse<object>> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Testing n8n connection...");
                var response = await _httpClient.GetAsync("/api/v1/workflows");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<N8nApiResponse<List<N8nWorkflow>>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return new ApiResponse<object>
                    {
                        Success = true,
                        Data = new { 
                            Message = "n8n connection successful",
                            WorkflowCount = apiResponse?.Data?.Count ?? 0
                        }
                    };
                }
                else
                {
                    return new ApiResponse<object>
                    {
                        Success = false,
                        Error = $"Failed to connect to n8n: {response.StatusCode}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "n8n connection test failed");
                return new ApiResponse<object>
                {
                    Success = false,
                    Error = "Failed to connect to n8n: " + ex.Message
                };
            }
        }
    }

    // DTOs for n8n API
    public class N8nWorkflow
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class N8nApiResponse<T>
    {
        public T? Data { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }
    }
}