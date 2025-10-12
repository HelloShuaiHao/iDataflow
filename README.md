# iDataflow - n8n Data Flow Platform

```
┌─────────────────────────────────────┐
│     iDataflow Backend (Port 3000)   │
│                                     │
│  Features:                          │
│  - User Authentication              │
│  - Workflow Monitoring              │
│  - WebSocket Data Hub               │
│  - n8n Integration                  │
└─────────────────┬───────────────────┘
                  ↓ n8n API
┌─────────────────────────────────────┐
│         n8n Engine (Port 5678)      │
│                                     │
│  Features:                          │
│  - Visual Workflow Editor           │
│  - Workflow Execution               │
│  - Powerful Integrations            │
└─────────────────────────────────────┘
```

Single-tenant data flow platform powered by n8n. Each deployment serves one company with independent workflow management.

## Features

- 🔐 **User Authentication** - JWT-based auth with role-based access control
- 🔄 **Workflow Management** - Monitor and sync workflows from n8n
- 🌐 **WebSocket Integration** - Real-time data reception and processing
- 🔌 **n8n Engine** - Visual workflow editor and execution
- 📊 **Data Management** - User and workflow tracking
- 🗄️ **PostgreSQL** - Reliable data persistence
- 🐳 **Docker Deployment** - One-command dev environment

## Tech Stack

- **Backend**: Node.js + Express
- **Workflow Engine**: n8n
- **Database**: PostgreSQL
- **Real-time**: WebSocket (ws)
- **Authentication**: JWT + bcrypt
- **Containerization**: Docker + Docker Compose

## Project Structure

```
iDataflow/
├── backend/                    # Backend service
│   ├── src/
│   │   ├── config/            # Configuration
│   │   │   └── database.js    # Database config
│   │   ├── models/            # Data models
│   │   │   ├── User.js        # User model
│   │   │   └── Workflow.js    # Workflow model
│   │   ├── controllers/       # Controllers
│   │   │   ├── authController.js
│   │   │   ├── n8nController.js
│   │   │   └── websocketController.js
│   │   ├── middleware/        # Middleware
│   │   │   └── auth.js        # JWT authentication
│   │   ├── services/          # Business logic
│   │   │   ├── websocketService.js
│   │   │   └── n8nService.js
│   │   ├── routes/            # API routes
│   │   │   ├── auth.js
│   │   │   ├── n8n.js
│   │   │   └── websocket.js
│   │   └── index.js           # App entry point
│   ├── package.json
│   ├── Dockerfile
│   └── .env.example
├── docker/                     # Docker configuration
│   ├── docker-compose.yml     # Container orchestration
│   └── init-db.sql            # Database initialization
└── docs/                       # Documentation
    └── 需求文档_简化版.md
```

## Quick Start

### Prerequisites

- Docker and Docker Compose
- Node.js 18+ (for local development)
- PostgreSQL 15+ (if not using Docker)

### 1. Clone Project

```bash
git clone <repository-url>
cd iDataflow
```

### 2. Configure Environment

```bash
cd backend
cp .env.example .env
```

Edit `.env` file with your configuration.

### 3. Start with Docker (Recommended)

```bash
cd docker
docker-compose up -d
```

This will start:
- **PostgreSQL** (port 5432)
- **n8n** (port 5678)
- **Backend API** (port 3000)
- **WebSocket** (port 3001)

### 4. Setup n8n

1. Open n8n: http://localhost:5678
2. Register first user:
   - Email: admin@idataflow.local
   - Password: (your secure password)
3. Generate API Key:
   - Settings → API → Create API Key
4. Update `docker/docker-compose.yml` line 68 with your API Key
5. Recreate backend:
   ```bash
   docker-compose up -d --force-recreate backend
   ```

### 5. Default Admin User

Default credentials (change in production!):
- Username: `admin`
- Password: `admin123`

### 6. Verify Services

- **Backend Health**: http://localhost:3000/health
- **n8n Interface**: http://localhost:5678
- **Login**: POST http://localhost:3000/api/auth/login

## API Documentation

### Authentication API

#### Login
```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

Response:
```json
{
  "success": true,
  "data": {
    "user": {
      "id": 1,
      "username": "admin",
      "email": "admin@idataflow.local",
      "role": "admin"
    },
    "token": "eyJhbGc..."
  }
}
```

#### Get Current User
```bash
GET /api/auth/me
Authorization: Bearer <token>
```

#### Change Password
```bash
POST /api/auth/change-password
Authorization: Bearer <token>
Content-Type: application/json

{
  "oldPassword": "admin123",
  "newPassword": "new_secure_password"
}
```

### User Management API (Admin Only)

#### Get All Users
```bash
GET /api/auth/users
Authorization: Bearer <token>
```

#### Create User
```bash
POST /api/auth/users
Authorization: Bearer <token>
Content-Type: application/json

{
  "username": "john",
  "email": "john@example.com",
  "password": "password123",
  "role": "member"
}
```

#### Update User
```bash
PUT /api/auth/users/:userId
Authorization: Bearer <token>
Content-Type: application/json

{
  "username": "john_updated",
  "email": "john.new@example.com",
  "role": "admin"
}
```

#### Delete User
```bash
DELETE /api/auth/users/:userId
Authorization: Bearer <token>
```

### Workflow API

#### Sync Workflows from n8n
```bash
POST /api/n8n/workflows/sync
Authorization: Bearer <token>
```

#### Get All Workflows
```bash
GET /api/n8n/workflows
Authorization: Bearer <token>
```

#### Get Workflow Details
```bash
GET /api/n8n/workflows/:workflowId
Authorization: Bearer <token>
```

#### Execute Workflow
```bash
POST /api/n8n/workflows/:workflowId/execute
Authorization: Bearer <token>
Content-Type: application/json

{
  "data": {
    // Input data
  }
}
```

#### Toggle Workflow (Activate/Deactivate)
```bash
POST /api/n8n/workflows/:workflowId/toggle
Authorization: Bearer <token>
Content-Type: application/json

{
  "active": true
}
```

### WebSocket API

#### Get WebSocket Stats
```bash
GET /api/websocket/stats
Authorization: Bearer <token>
```

#### Send Message to Client
```bash
POST /api/websocket/send/:clientId
Authorization: Bearer <token>
Content-Type: application/json

{
  "message": {
    "type": "notification",
    "data": {}
  }
}
```

#### Broadcast Message
```bash
POST /api/websocket/broadcast
Authorization: Bearer <token>
Content-Type: application/json

{
  "message": {
    "type": "announcement",
    "data": {}
  }
}
```

## WebSocket Client Example

### Node.js Client

```javascript
const WebSocket = require('ws');

// Connect to WebSocket server
const ws = new WebSocket('ws://localhost:3001');

ws.on('open', () => {
  console.log('Connected to WebSocket server');

  // Register client
  ws.send(JSON.stringify({
    type: 'register',
    clientId: 'device-001'
  }));
});

ws.on('message', (data) => {
  const message = JSON.parse(data);
  console.log('Received:', message);
});

// Send data
ws.send(JSON.stringify({
  type: 'data',
  payload: {
    temperature: 25.5,
    humidity: 60
  }
}));

// Heartbeat
setInterval(() => {
  ws.send(JSON.stringify({ type: 'ping' }));
}, 30000);
```

### Browser Client

```html
<!DOCTYPE html>
<html>
<head>
  <title>WebSocket Client</title>
</head>
<body>
  <h1>iDataflow WebSocket Client</h1>
  <div id="status">Disconnected</div>
  <button onclick="connect()">Connect</button>
  <button onclick="sendData()">Send Data</button>

  <script>
    let ws;

    function connect() {
      ws = new WebSocket('ws://localhost:3001');

      ws.onopen = () => {
        document.getElementById('status').textContent = 'Connected';
        ws.send(JSON.stringify({
          type: 'register',
          clientId: 'browser-001'
        }));
      };

      ws.onmessage = (event) => {
        console.log('Message:', JSON.parse(event.data));
      };

      ws.onclose = () => {
        document.getElementById('status').textContent = 'Disconnected';
      };
    }

    function sendData() {
      if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({
          type: 'data',
          payload: {
            value: Math.random() * 100
          }
        }));
      }
    }
  </script>
</body>
</html>
```

## Database Structure

### users table
```sql
id            SERIAL PRIMARY KEY
username      VARCHAR(50) UNIQUE
email         VARCHAR(255) UNIQUE
password_hash VARCHAR(255)
role          VARCHAR(20)        -- admin, member, viewer
created_at    TIMESTAMP
updated_at    TIMESTAMP
```

### workflows table
```sql
id                SERIAL PRIMARY KEY
n8n_workflow_id   VARCHAR(255) UNIQUE
name              VARCHAR(255)
description       TEXT
active            BOOLEAN
created_at        TIMESTAMP
updated_at        TIMESTAMP
```

### executions table
```sql
id                 SERIAL PRIMARY KEY
workflow_id        INTEGER (FK)
n8n_execution_id   VARCHAR(255)
status             VARCHAR(50)     -- success, failed, running
started_at         TIMESTAMP
finished_at        TIMESTAMP
error_message      TEXT
created_at         TIMESTAMP
```

### websocket_logs table
```sql
id             SERIAL PRIMARY KEY
client_id      VARCHAR(255)
message_type   VARCHAR(50)
payload        JSONB
created_at     TIMESTAMP
```

## Development Guide

### Install Dependencies

```bash
cd backend
npm install
```

### Start Development Server

```bash
# Start only database and n8n
cd docker
docker-compose up -d postgres n8n

# In another terminal, start backend
cd ../backend
npm run dev
```

### Debug

```bash
# View backend logs
docker-compose logs -f backend

# View n8n logs
docker-compose logs -f n8n

# View database logs
docker-compose logs -f postgres
```

## Deployment

### Single Server Deployment

```bash
# Deploy to server
git clone <repository-url>
cd iDataflow/docker
cp ../backend/.env.example ../backend/.env
# Edit .env with production values
docker-compose up -d
```

### Environment Variables

Key environment variables to configure:

```env
# Server
PORT=3000
NODE_ENV=production

# n8n
N8N_HOST=http://n8n:5678
N8N_API_KEY=<your-n8n-api-key>

# Database
DB_HOST=postgres
DB_PORT=5432
DB_NAME=idataflow
DB_USER=postgres
DB_PASSWORD=<strong-password>

# WebSocket
WS_PORT=3001

# Security
JWT_SECRET=<generate-strong-secret>
JWT_EXPIRES_IN=24h
```

## Common Issues

### 1. Cannot connect to n8n

Ensure n8n is running:
```bash
docker-compose ps
```

### 2. Database connection failed

Check PostgreSQL status:
```bash
docker-compose exec postgres psql -U postgres -d idataflow -c "SELECT 1;"
```

### 3. WebSocket connection failed

Confirm port 3001 is not in use and check firewall settings.

### 4. JWT authentication failed

Verify JWT_SECRET in .env matches between restarts.

## User Roles

- **admin**: Full access, can manage users and workflows
- **member**: Can view and execute workflows
- **viewer**: Read-only access

## Security Best Practices

1. **Change default passwords** immediately in production
2. **Use strong JWT_SECRET** (at least 32 characters)
3. **Enable HTTPS** with proper SSL certificates
4. **Restrict n8n port** access (only backend should access)
5. **Regular backups** of PostgreSQL database
6. **Update dependencies** regularly

## Backup and Restore

### Backup Database

```bash
docker exec idataflow-postgres pg_dump -U postgres idataflow > backup.sql
```

### Restore Database

```bash
docker exec -i idataflow-postgres psql -U postgres idataflow < backup.sql
```

## Next Steps

Check `docs/需求文档_简化版.md` for detailed requirements and roadmap.

### Current Status
- [x] Backend API
- [x] User authentication
- [x] Workflow management
- [x] WebSocket service
- [ ] Frontend dashboard
- [ ] Monitoring and alerts
- [ ] Production deployment

## Contributing

Contributions welcome! Please submit issues and pull requests.

## License

MIT License
