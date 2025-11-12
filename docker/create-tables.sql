-- Drop existing tables if they exist
DROP TABLE IF EXISTS "WebSocketLogs" CASCADE;
DROP TABLE IF EXISTS "Executions" CASCADE;
DROP TABLE IF EXISTS "Workflows" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;

-- Create Users table
CREATE TABLE "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(50) UNIQUE NOT NULL,
    "Email" VARCHAR(255) UNIQUE NOT NULL,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "Role" VARCHAR(20) DEFAULT 'member',
    "CreatedAt" TIMESTAMP DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP DEFAULT NOW()
);

-- Create Workflows table
CREATE TABLE "Workflows" (
    "Id" SERIAL PRIMARY KEY,
    "N8nWorkflowId" VARCHAR(255) UNIQUE NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "Active" BOOLEAN DEFAULT false,
    "UserId" INTEGER REFERENCES "Users"("Id"),
    "CreatedAt" TIMESTAMP DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP DEFAULT NOW()
);

-- Create Executions table
CREATE TABLE "Executions" (
    "Id" SERIAL PRIMARY KEY,
    "WorkflowId" INTEGER REFERENCES "Workflows"("Id") ON DELETE CASCADE,
    "N8nExecutionId" VARCHAR(255),
    "Status" VARCHAR(50),
    "StartedAt" TIMESTAMP,
    "FinishedAt" TIMESTAMP,
    "ErrorMessage" TEXT,
    "CreatedAt" TIMESTAMP DEFAULT NOW()
);

-- Create WebSocketLogs table
CREATE TABLE "WebSocketLogs" (
    "Id" SERIAL PRIMARY KEY,
    "ClientId" VARCHAR(255),
    "MessageType" VARCHAR(50),
    "Payload" TEXT,
    "CreatedAt" TIMESTAMP DEFAULT NOW()
);

-- Verify tables were created
SELECT 'Tables created successfully' AS result;
\dt "Users"
\dt "Workflows" 
\dt "Executions"
\dt "WebSocketLogs"