using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using iDataflow.Backend.Data;
using iDataflow.Backend.Models;

namespace iDataflow.Backend.Services
{
    public class WebSocketService
    {
        private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WebSocketService> _logger;

        public WebSocketService(IServiceProvider serviceProvider, ILogger<WebSocketService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task HandleWebSocketAsync(WebSocket webSocket, string connectionId)
        {
            var connection = new WebSocketConnection
            {
                Id = connectionId,
                WebSocket = webSocket,
                ConnectedAt = DateTime.UtcNow
            };

            _connections.TryAdd(connectionId, connection);
            _logger.LogInformation("New WebSocket connection: {ConnectionId}", connectionId);

            await SendMessage(webSocket, new
            {
                type = "connected",
                message = "Connected to iDataflow WebSocket server",
                clientId = connectionId
            });

            var buffer = new byte[4096];
            
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await HandleMessage(connection, message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, "WebSocket exception for connection {ConnectionId}", connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection {ConnectionId}", connectionId);
            }
            finally
            {
                _connections.TryRemove(connectionId, out _);
                _logger.LogInformation("WebSocket disconnected: {ConnectionId}", connectionId);
            }
        }

        private async Task HandleMessage(WebSocketConnection connection, string messageText)
        {
            try
            {
                using var document = JsonDocument.Parse(messageText);
                var messageType = document.RootElement.GetProperty("type").GetString();

                _logger.LogInformation("Received message from {ConnectionId}: {MessageType}", connection.Id, messageType);

                switch (messageType)
                {
                    case "register":
                        await HandleRegister(connection, document.RootElement);
                        break;

                    case "data":
                        await HandleData(connection, document.RootElement);
                        break;

                    case "ping":
                        await SendMessage(connection.WebSocket, new { type = "pong" });
                        break;

                    default:
                        _logger.LogWarning("Unknown message type: {MessageType}", messageType);
                        break;
                }

                // Log to database
                await LogMessageToDatabase(connection.Id, messageType ?? "unknown", messageText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing WebSocket message");
                await SendMessage(connection.WebSocket, new
                {
                    type = "error",
                    message = "Invalid message format"
                });
            }
        }

        private async Task HandleRegister(WebSocketConnection connection, JsonElement message)
        {
            if (message.TryGetProperty("companyId", out var companyIdElement))
            {
                var companyId = companyIdElement.GetString();
                if (!string.IsNullOrEmpty(companyId))
                {
                    connection.CompanyId = companyId;
                    _logger.LogInformation("Client registered: companyId={CompanyId}", companyId);

                    await SendMessage(connection.WebSocket, new
                    {
                        type = "registered",
                        message = $"Successfully registered for company {companyId}",
                        companyId
                    });
                    return;
                }
            }

            await SendMessage(connection.WebSocket, new
            {
                type = "error",
                message = "Company ID is required"
            });
        }

        private async Task HandleData(WebSocketConnection connection, JsonElement message)
        {
            if (message.TryGetProperty("payload", out var payloadElement))
            {
                var payload = payloadElement.GetRawText();
                
                _logger.LogInformation("Received data from {CompanyId}: {Payload}", connection.CompanyId ?? "unregistered", payload);
                
                // 无论是否注册，都发送数据到n8n webhook
                try
                {
                    using var httpClient = new HttpClient();
                    var n8nWebhookUrl = "http://localhost:5678/webhook-test/1158ae5b-867d-40ec-868c-5b3ffbdd7b7d";
                    
                    // 构造发送给n8n的数据
                    var dataToN8n = new
                    {
                        type = "websocket_data",
                        companyId = connection.CompanyId ?? "unregistered",
                        clientId = connection.Id,
                        timestamp = DateTime.UtcNow.ToString("O"),
                        payload = JsonSerializer.Deserialize<object>(payload)
                    };
                    
                    var json = JsonSerializer.Serialize(dataToN8n);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    _logger.LogInformation("🚀 Sending data to n8n webhook: {Url}", n8nWebhookUrl);
                    _logger.LogInformation("📤 Data being sent: {Data}", json);
                    
                    var response = await httpClient.PostAsync(n8nWebhookUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("✅ Successfully sent data to n8n: {Response}", responseContent);
                        
                        await SendMessage(connection.WebSocket, new
                        {
                            type = "data_processed",
                            message = "Data sent to n8n successfully",
                            n8nResponse = responseContent
                        });
                    }
                    else
                    {
                        _logger.LogWarning("❌ Failed to send data to n8n: {StatusCode} {Response}", response.StatusCode, responseContent);
                        
                        await SendMessage(connection.WebSocket, new
                        {
                            type = "data_error",
                            message = $"Failed to send to n8n: {response.StatusCode}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error sending data to n8n webhook");
                    
                    await SendMessage(connection.WebSocket, new
                    {
                        type = "data_error",
                        message = "Error sending data to n8n: " + ex.Message
                    });
                }
            }
            else
            {
                _logger.LogWarning("No payload found in data message");
            }
        }

        private async Task LogMessageToDatabase(string clientId, string messageType, string payload)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var log = new WebSocketLog
                {
                    ClientId = clientId,
                    MessageType = messageType,
                    Payload = payload,
                    CreatedAt = DateTime.UtcNow
                };

                context.WebSocketLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging WebSocket message to database");
            }
        }

        public async Task SendToClient(string connectionId, object message)
        {
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                await SendMessage(connection.WebSocket, message);
            }
        }

        public async Task BroadcastToAll(object message)
        {
            var tasks = _connections.Values
                .Where(conn => conn.WebSocket.State == WebSocketState.Open)
                .Select(conn => SendMessage(conn.WebSocket, message));

            await Task.WhenAll(tasks);
        }

        public async Task BroadcastToCompany(string companyId, object message)
        {
            var tasks = _connections.Values
                .Where(conn => conn.CompanyId == companyId && conn.WebSocket.State == WebSocketState.Open)
                .Select(conn => SendMessage(conn.WebSocket, message));

            await Task.WhenAll(tasks);
        }

        private static async Task SendMessage(WebSocket webSocket, object message)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public WebSocketStats GetStats()
        {
            return new WebSocketStats
            {
                TotalConnections = _connections.Count,
                RegisteredClients = _connections.Values.Count(c => !string.IsNullOrEmpty(c.CompanyId)),
                Clients = _connections.Values.Select(c => new WebSocketClientInfo
                {
                    ClientId = c.Id,
                    CompanyId = c.CompanyId,
                    ConnectedAt = c.ConnectedAt
                }).ToList()
            };
        }
    }

    public class WebSocketConnection
    {
        public string Id { get; set; } = string.Empty;
        public WebSocket WebSocket { get; set; } = null!;
        public string? CompanyId { get; set; }
        public DateTime ConnectedAt { get; set; }
    }

    public class WebSocketStats
    {
        public int TotalConnections { get; set; }
        public int RegisteredClients { get; set; }
        public List<WebSocketClientInfo> Clients { get; set; } = new();
    }

    public class WebSocketClientInfo
    {
        public string ClientId { get; set; } = string.Empty;
        public string? CompanyId { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}