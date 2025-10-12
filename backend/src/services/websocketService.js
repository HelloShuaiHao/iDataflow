const WebSocket = require('ws');
const EventEmitter = require('events');

class WebSocketService extends EventEmitter {
  constructor() {
    super();
    this.wss = null;
    this.clients = new Map(); // 存储客户端连接，key: companyId, value: WebSocket 实例
  }

  // 初始化 WebSocket 服务器
  initialize(port = 3001) {
    this.wss = new WebSocket.Server({ port });

    this.wss.on('listening', () => {
      console.log(`✅ WebSocket server is running on port ${port}`);
    });

    this.wss.on('connection', (ws, req) => {
      const clientId = this.generateClientId();
      console.log(`🔗 New WebSocket connection: ${clientId}`);

      // 连接元数据
      ws.metadata = {
        id: clientId,
        companyId: null,
        connectedAt: new Date(),
      };

      // 监听消息
      ws.on('message', (data) => {
        this.handleMessage(ws, data);
      });

      // 监听关闭
      ws.on('close', () => {
        console.log(`❌ WebSocket disconnected: ${clientId}`);
        if (ws.metadata.companyId) {
          this.clients.delete(ws.metadata.companyId);
        }
        this.emit('disconnect', ws.metadata);
      });

      // 监听错误
      ws.on('error', (error) => {
        console.error(`⚠️ WebSocket error for ${clientId}:`, error);
        this.emit('error', { metadata: ws.metadata, error });
      });

      // 发送欢迎消息
      this.sendMessage(ws, {
        type: 'connected',
        message: 'Connected to iDataflow WebSocket server',
        clientId,
      });
    });

    return this;
  }

  // 处理接收到的消息
  handleMessage(ws, data) {
    try {
      const message = JSON.parse(data.toString());
      console.log(`📩 Received message from ${ws.metadata.id}:`, message);

      switch (message.type) {
        case 'register':
          // 注册客户端的公司 ID
          this.registerClient(ws, message.companyId);
          break;

        case 'data':
          // 处理数据消息
          this.emit('data', {
            companyId: ws.metadata.companyId,
            data: message.payload,
            timestamp: new Date(),
          });
          break;

        case 'ping':
          // 心跳响应
          this.sendMessage(ws, { type: 'pong' });
          break;

        default:
          console.warn(`Unknown message type: ${message.type}`);
      }
    } catch (error) {
      console.error('Error parsing WebSocket message:', error);
      this.sendMessage(ws, {
        type: 'error',
        message: 'Invalid message format',
      });
    }
  }

  // 注册客户端
  registerClient(ws, companyId) {
    if (!companyId) {
      this.sendMessage(ws, {
        type: 'error',
        message: 'Company ID is required',
      });
      return;
    }

    ws.metadata.companyId = companyId;
    this.clients.set(companyId, ws);

    console.log(`✅ Client registered: companyId=${companyId}`);

    this.sendMessage(ws, {
      type: 'registered',
      message: `Successfully registered for company ${companyId}`,
      companyId,
    });

    this.emit('register', { companyId, clientId: ws.metadata.id });
  }

  // 发送消息给特定客户端
  sendToCompany(companyId, message) {
    const client = this.clients.get(companyId);
    if (client && client.readyState === WebSocket.OPEN) {
      this.sendMessage(client, message);
      return true;
    }
    return false;
  }

  // 广播消息给所有客户端
  broadcast(message) {
    this.wss.clients.forEach((client) => {
      if (client.readyState === WebSocket.OPEN) {
        this.sendMessage(client, message);
      }
    });
  }

  // 发送消息
  sendMessage(ws, message) {
    try {
      ws.send(JSON.stringify(message));
    } catch (error) {
      console.error('Error sending WebSocket message:', error);
    }
  }

  // 生成客户端 ID
  generateClientId() {
    return `client_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  // 获取连接统计
  getStats() {
    return {
      totalConnections: this.wss ? this.wss.clients.size : 0,
      registeredClients: this.clients.size,
      clients: Array.from(this.clients.entries()).map(([companyId, ws]) => ({
        companyId,
        clientId: ws.metadata.id,
        connectedAt: ws.metadata.connectedAt,
      })),
    };
  }

  // 关闭服务器
  close() {
    if (this.wss) {
      this.wss.close(() => {
        console.log('WebSocket server closed');
      });
    }
  }
}

module.exports = new WebSocketService();
