using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using iDataflow.Backend.Data;
using iDataflow.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
var jwtSecretKey = builder.Configuration["JWT:SecretKey"] ?? "your-secret-key-change-in-production";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "iDataflow",
            ValidAudience = builder.Configuration["JWT:Audience"] ?? "iDataflow-users",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add custom services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddHttpClient<N8nService>();
builder.Services.AddSingleton<WebSocketService>();

// Note: WebSockets are configured in app.UseWebSockets() below

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors();

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

// WebSocket endpoint
app.Map("/ws", async (HttpContext context, WebSocketService wsService) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = $"client_{DateTime.Now.Ticks}_{Guid.NewGuid().ToString("N")[..8]}";
        await wsService.HandleWebSocketAsync(webSocket, connectionId);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// Health check endpoint
app.MapGet("/health", async (ApplicationDbContext context) =>
{
    try
    {
        await context.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = "connected"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(new
        {
            status = "unhealthy",
            timestamp = DateTime.UtcNow,
            database = "disconnected",
            error = ex.Message
        }.ToString());
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    // Create default admin user if not exists
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    if (!await userService.UserExistsAsync("admin", "admin@idataflow.com"))
    {
        await userService.CreateUserAsync("admin", "admin@idataflow.com", "admin123", "admin");
        Console.WriteLine("✅ Default admin user created: admin / admin123");
    }
}

Console.WriteLine("==================================");
Console.WriteLine("🚀 iDataflow C# Backend Server");
Console.WriteLine("==================================");
Console.WriteLine($"📡 HTTP Server: http://localhost:{builder.Configuration["PORT"] ?? "3000"}");
Console.WriteLine($"🔌 WebSocket: ws://localhost:{builder.Configuration["PORT"] ?? "3000"}/ws");
Console.WriteLine($"💚 Health: http://localhost:{builder.Configuration["PORT"] ?? "3000"}/health");
Console.WriteLine($"📚 API Endpoints:");
Console.WriteLine($"   - Login: http://localhost:{builder.Configuration["PORT"] ?? "3000"}/api/auth/login");
Console.WriteLine($"   - Workflows: http://localhost:{builder.Configuration["PORT"] ?? "3000"}/api/workflows");
Console.WriteLine($"   - WebSocket Stats: http://localhost:{builder.Configuration["PORT"] ?? "3000"}/api/websocket/stats");
Console.WriteLine("==================================");

app.Run();
