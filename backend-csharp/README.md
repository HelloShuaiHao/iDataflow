# iDataflow C# Backend

这是iDataflow平台的C#后端实现，使用ASP.NET Core 8.0开发。

## 功能特性

- ✅ **用户认证系统** - JWT认证，支持角色权限管理
- ✅ **数据库集成** - 使用Entity Framework Core + PostgreSQL
- ✅ **n8n工作流集成** - 完整的n8n API调用支持
- ✅ **WebSocket服务** - 实时数据推送和设备通信
- ✅ **RESTful API** - 完整的后台管理接口
- ✅ **健康检查** - 服务状态监控
- ✅ **Docker支持** - 容器化部署

## 技术栈

- **框架**: ASP.NET Core 8.0
- **数据库**: PostgreSQL 15 + Entity Framework Core
- **认证**: JWT Bearer Authentication
- **密码加密**: BCrypt
- **实时通信**: WebSocket
- **HTTP客户端**: HttpClient (用于n8n集成)
- **容器化**: Docker

## 项目结构

```
backend-csharp/
├── Controllers/          # API控制器
│   ├── AuthController.cs
│   ├── WorkflowsController.cs
│   └── WebSocketController.cs
├── Services/            # 业务服务层
│   ├── UserService.cs
│   ├── JwtService.cs
│   ├── N8nService.cs
│   └── WebSocketService.cs
├── Models/              # 数据模型
│   ├── User.cs
│   ├── Workflow.cs
│   ├── Execution.cs
│   └── WebSocketLog.cs
├── Data/               # 数据访问层
│   └── ApplicationDbContext.cs
├── DTOs/               # 数据传输对象
│   └── AuthDTOs.cs
├── Program.cs          # 应用程序入口点
├── appsettings.json    # 配置文件
└── Dockerfile         # Docker构建文件
```

## 快速开始

### 前置条件

- .NET 8.0 SDK
- PostgreSQL 15
- (可选) Docker 和 Docker Compose

### 方式一：本地运行

1. **启动PostgreSQL数据库**
   ```bash
   # 使用Docker启动PostgreSQL
   docker run --name postgres \
     -e POSTGRES_PASSWORD=postgres_password \
     -e POSTGRES_DB=idataflow \
     -p 5432:5432 \
     -d postgres:15-alpine
   ```

2. **配置环境变量**
   编辑 `appsettings.json` 或设置环境变量：
   ```bash
   export ConnectionStrings__DefaultConnection="Host=localhost;Database=idataflow;Username=postgres;Password=postgres_password"
   export N8N__BaseUrl="http://localhost:5678"
   export JWT__SecretKey="your-secret-key-change-in-production-make-it-at-least-32-characters-long"
   ```

3. **构建和运行**
   ```bash
   cd backend-csharp
   dotnet build
   dotnet run
   ```

4. **使用启动脚本**
   ```bash
   # 在项目根目录下
   ./start-csharp-backend.sh
   ```

### 方式二：Docker Compose

```bash
# 在项目根目录下
cd docker
docker-compose up -d
```

## API 接口

服务启动后，可以通过以下接口访问：

### 健康检查
- `GET /health` - 服务健康状态

### 用户认证
- `POST /api/auth/login` - 用户登录
- `GET /api/auth/me` - 获取当前用户信息  
- `POST /api/auth/change-password` - 修改密码
- `GET /api/auth/users` - 获取所有用户 (仅管理员)
- `POST /api/auth/users` - 创建用户 (仅管理员)
- `PUT /api/auth/users/{id}` - 更新用户 (仅管理员)
- `DELETE /api/auth/users/{id}` - 删除用户 (仅管理员)

### 工作流管理
- `GET /api/workflows` - 获取所有工作流
- `GET /api/workflows/{id}` - 获取工作流详情
- `POST /api/workflows/{id}/execute` - 执行工作流
- `GET /api/workflows/test-connection` - 测试n8n连接

### WebSocket监控
- `GET /api/websocket/stats` - WebSocket连接统计
- `GET /api/websocket/logs` - WebSocket消息日志
- `POST /api/websocket/send/{connectionId}` - 发送消息到指定连接 (仅管理员)
- `POST /api/websocket/broadcast` - 广播消息 (仅管理员)

### WebSocket连接
- `ws://localhost:3000/ws` - WebSocket端点

## 默认管理员账号

首次启动时，系统会自动创建默认管理员账号：
- **用户名**: admin
- **密码**: admin123
- **邮箱**: admin@idataflow.com

## 数据库表结构

系统会自动创建以下数据表：（其实并没有创建 需要进入容器创建数据库表！）

### users (用户表)
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) DEFAULT 'member',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

### workflows (工作流表)
```sql
CREATE TABLE workflows (
    id SERIAL PRIMARY KEY,
    n8n_workflow_id VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    active BOOLEAN DEFAULT false,
    user_id INTEGER REFERENCES users(id),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

### executions (执行历史表)
```sql
CREATE TABLE executions (
    id SERIAL PRIMARY KEY,
    workflow_id INTEGER REFERENCES workflows(id) ON DELETE CASCADE,
    n8n_execution_id VARCHAR(255),
    status VARCHAR(50),
    started_at TIMESTAMP,
    finished_at TIMESTAMP,
    error_message TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);
```

### websocket_logs (WebSocket日志表)
```sql
CREATE TABLE websocket_logs (
    id SERIAL PRIMARY KEY,
    client_id VARCHAR(255),
    message_type VARCHAR(50),
    payload TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);
```

## 配置项说明

### 数据库连接
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=idataflow;Username=postgres;Password=postgres_password"
  }
}
```

### JWT认证配置
```json
{
  "JWT": {
    "SecretKey": "your-secret-key-change-in-production-make-it-at-least-32-characters-long",
    "Issuer": "iDataflow",
    "Audience": "iDataflow-users",
    "ExpirationMinutes": "1440"
  }
}
```

### n8n集成配置
```json
{
  "N8N": {
    "BaseUrl": "http://localhost:5678",
    "ApiKey": ""
  }
}
```

## 开发说明

### 添加新的API端点
1. 在对应的Controller中添加方法
2. 确保使用适当的认证和授权属性
3. 返回统一的ApiResponse格式

### 数据库迁移
```bash
# 添加新的迁移
dotnet ef migrations add MigrationName

# 更新数据库
dotnet ef database update
```

### WebSocket消息格式

客户端连接后，可以发送以下类型的消息：

```json
// 注册客户端
{
  "type": "register",
  "companyId": "your-company-id"
}

// 发送数据
{
  "type": "data",
  "payload": {
    "sensorId": "sensor-001",
    "value": 25.5,
    "timestamp": "2025-01-01T00:00:00Z"
  }
}

// 心跳
{
  "type": "ping"
}
```

## 部署指南

### Docker部署

1. **构建镜像**
   ```bash
   cd backend-csharp
   docker build -t idataflow-backend-csharp .
   ```

2. **运行容器**
   ```bash
   docker run -d \
     --name idataflow-backend \
     -p 3000:3000 \
     -e ConnectionStrings__DefaultConnection="Host=postgres;Database=idataflow;Username=postgres;Password=postgres_password" \
     idataflow-backend-csharp
   ```

### 生产环境配置

1. **更改JWT密钥**
2. **使用更安全的数据库密码**
3. **配置HTTPS**
4. **设置日志级别**
5. **配置健康检查和监控**

## 与JavaScript版本的差异

1. **性能提升**: C#版本具有更好的性能和内存管理
2. **类型安全**: 编译时类型检查，减少运行时错误
3. **企业级特性**: 更好的企业应用支持
4. **生态系统**: 丰富的.NET生态系统
5. **部署简单**: 单一可执行文件部署

## 许可证

MIT License