require('dotenv').config();
const express = require('express');
const cors = require('cors');
const { pool } = require('./config/database');

// 导入服务
const websocketService = require('./services/websocketService');

// 导入路由
const authRouter = require('./routes/auth');
const websocketRouter = require('./routes/websocket');
const n8nRouter = require('./routes/n8n');

const app = express();
const PORT = process.env.PORT || 3000;

// 中间件
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// 请求日志中间件
app.use((req, res, next) => {
  console.log(`${new Date().toISOString()} - ${req.method} ${req.path}`);
  next();
});

// 健康检查
app.get('/health', async (req, res) => {
  try {
    await pool.query('SELECT 1');
    res.json({
      status: 'healthy',
      timestamp: new Date().toISOString(),
      database: 'connected',
    });
  } catch (error) {
    res.status(503).json({
      status: 'unhealthy',
      timestamp: new Date().toISOString(),
      database: 'disconnected',
      error: error.message,
    });
  }
});

// API 路由
app.use('/api/auth', authRouter);
app.use('/api/websocket', websocketRouter);
app.use('/api/n8n', n8nRouter);

// 404 处理
app.use((req, res) => {
  res.status(404).json({
    success: false,
    error: 'Route not found',
  });
});

// 错误处理中间件
app.use((err, req, res, next) => {
  console.error('Error:', err);
  res.status(500).json({
    success: false,
    error: 'Internal server error',
    message: err.message,
  });
});

// 初始化 WebSocket 服务
const WS_PORT = process.env.WS_PORT || 3001;
websocketService.initialize(WS_PORT);

// 监听 WebSocket 事件
websocketService.on('data', (data) => {
  console.log('📨 WebSocket data received:', data);
  // 这里可以触发 n8n 工作流或其他处理逻辑
});

websocketService.on('register', (data) => {
  console.log('✅ Client registered:', data);
});

// 启动 HTTP 服务器
app.listen(PORT, () => {
  console.log('=================================');
  console.log('🚀 iDataflow Backend Server');
  console.log('=================================');
  console.log(`📡 HTTP Server: http://localhost:${PORT}`);
  console.log(`🔌 WebSocket: ws://localhost:${WS_PORT}`);
  console.log(`💚 Health: http://localhost:${PORT}/health`);
  console.log(`📚 API Endpoints:`);
  console.log(`   - Login: http://localhost:${PORT}/api/auth/login`);
  console.log(`   - WebSocket Stats: http://localhost:${PORT}/api/websocket/stats`);
  console.log(`   - n8n Test: http://localhost:${PORT}/api/n8n/test`);
  console.log(`   - Workflows: http://localhost:${PORT}/api/n8n/workflows`);
  console.log('=================================');
});

// 优雅关闭
process.on('SIGTERM', async () => {
  console.log('SIGTERM received, closing server...');
  websocketService.close();
  await pool.end();
  process.exit(0);
});
