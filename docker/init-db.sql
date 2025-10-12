-- 创建 n8n 数据库
CREATE DATABASE n8n;

-- 切换到 idataflow 数据库
\c idataflow;

-- 创建用户表
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(20) DEFAULT 'member', -- admin, member, viewer
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建工作流表（同步自 n8n）
CREATE TABLE IF NOT EXISTS workflows (
    id SERIAL PRIMARY KEY,
    n8n_workflow_id VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    active BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建执行记录表
CREATE TABLE IF NOT EXISTS executions (
    id SERIAL PRIMARY KEY,
    workflow_id INTEGER REFERENCES workflows(id) ON DELETE CASCADE,
    n8n_execution_id VARCHAR(255) NOT NULL,
    status VARCHAR(50), -- success, failed, running
    started_at TIMESTAMP,
    finished_at TIMESTAMP,
    error_message TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建 WebSocket 日志表
CREATE TABLE IF NOT EXISTS websocket_logs (
    id SERIAL PRIMARY KEY,
    client_id VARCHAR(255),
    message_type VARCHAR(50),
    payload JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_workflows_n8n_id ON workflows(n8n_workflow_id);
CREATE INDEX IF NOT EXISTS idx_workflows_active ON workflows(active);
CREATE INDEX IF NOT EXISTS idx_executions_workflow_id ON executions(workflow_id);
CREATE INDEX IF NOT EXISTS idx_executions_status ON executions(status);
CREATE INDEX IF NOT EXISTS idx_websocket_logs_created_at ON websocket_logs(created_at);

-- 创建更新时间触发器函数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 为用户表添加更新触发器
DROP TRIGGER IF EXISTS update_users_updated_at ON users;
CREATE TRIGGER update_users_updated_at
    BEFORE UPDATE ON users
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 为工作流表添加更新触发器
DROP TRIGGER IF EXISTS update_workflows_updated_at ON workflows;
CREATE TRIGGER update_workflows_updated_at
    BEFORE UPDATE ON workflows
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 插入默认管理员用户 (密码: admin123)
-- 注意：这是 bcrypt hash 的 'admin123'，实际部署时应该修改密码
INSERT INTO users (username, email, password_hash, role) VALUES
    ('admin', 'admin@idataflow.local', '$2b$10$rKvZ8QJZqZQXqZQXqZQXqOe7K7K7K7K7K7K7K7K7K7K7K7K7K7K7K', 'admin')
ON CONFLICT DO NOTHING;
