const axios = require('axios');

class N8nService {
  constructor() {
    this.baseURL = process.env.N8N_HOST || 'http://localhost:5678';
    this.apiKey = process.env.N8N_API_KEY || '';

    const headers = {
      'Content-Type': 'application/json',
    };

    // 如果设置了 API Key，使用 API Key 认证
    if (this.apiKey) {
      headers['X-N8N-API-KEY'] = this.apiKey;
    }

    this.client = axios.create({
      baseURL: this.baseURL,
      headers: headers,
    });
  }

  // 获取所有工作流
  async getWorkflows() {
    try {
      const response = await this.client.get('/api/v1/workflows');
      return {
        success: true,
        data: response.data.data,
      };
    } catch (error) {
      console.error('Error fetching workflows:', error.message);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  // 获取单个工作流
  async getWorkflow(workflowId) {
    try {
      const response = await this.client.get(`/api/v1/workflows/${workflowId}`);
      return {
        success: true,
        data: response.data,
      };
    } catch (error) {
      console.error(`Error fetching workflow ${workflowId}:`, error.message);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  // 创建工作流
  async createWorkflow(workflowData) {
    try {
      const response = await this.client.post('/api/v1/workflows', workflowData);
      return {
        success: true,
        data: response.data,
      };
    } catch (error) {
      console.error('Error creating workflow:', error.message);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  // 更新工作流
  async updateWorkflow(workflowId, workflowData) {
    try {
      const response = await this.client.patch(
        `/api/v1/workflows/${workflowId}`,
        workflowData
      );
      return {
        success: true,
        data: response.data,
      };
    } catch (error) {
      console.error(`Error updating workflow ${workflowId}:`, error.message);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  // 删除工作流
  async deleteWorkflow(workflowId) {
    try {
      await this.client.delete(`/api/v1/workflows/${workflowId}`);
      return {
        success: true,
        message: 'Workflow deleted successfully',
      };
    } catch (error) {
      console.error(`Error deleting workflow ${workflowId}:`, error.message);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  // 激活/停用工作流
  async toggleWorkflow(workflowId, active) {
    try {
      const response = await this.client.patch(`/api/v1/workflows/${workflowId}`, {
        active,
      });
      return {
        success: true,
        data: response.data,
      };
    } catch (error) {
      console.error(`Error toggling workflow ${workflowId}:`, error.message);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  // 执行工作流
  async executeWorkflow(workflowId, data = {}) {
    try {
      const response = await this.client.post(
        `/api/v1/workflows/${workflowId}/execute`,
        data
      );
      return {
        success: true,
        data: response.data,
      };
    } catch (error) {
      console.error(`Error executing workflow ${workflowId}:`, error.message);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  // 获取工作流执行历史
  async getExecutions(workflowId, limit = 10) {
    try {
      const response = await this.client.get('/api/v1/executions', {
        params: {
          workflowId,
          limit,
        },
      });
      return {
        success: true,
        data: response.data.data,
      };
    } catch (error) {
      console.error('Error fetching executions:', error.message);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  // 获取单个执行详情
  async getExecution(executionId) {
    try {
      const response = await this.client.get(`/api/v1/executions/${executionId}`);
      return {
        success: true,
        data: response.data,
      };
    } catch (error) {
      console.error(`Error fetching execution ${executionId}:`, error.message);
      return {
        success: false,
        error: error.message,
      };
    }
  }

  // 创建基础的 WebSocket 数据接收工作流模板
  createWebSocketWorkflowTemplate(companyId, companyName) {
    return {
      name: `Company_${companyId}_WebSocket_Flow`,
      nodes: [
        {
          parameters: {
            httpMethod: 'POST',
            path: `webhook/company/${companyId}`,
            responseMode: 'onReceived',
            options: {},
          },
          name: 'Webhook',
          type: 'n8n-nodes-base.webhook',
          typeVersion: 1,
          position: [250, 300],
          webhookId: `company-${companyId}-webhook`,
        },
        {
          parameters: {
            functionCode: `// 处理接收到的 WebSocket 数据
const data = $input.all();
const processedData = data.map(item => ({
  companyId: ${companyId},
  companyName: '${companyName}',
  timestamp: new Date().toISOString(),
  data: item.json,
  processed: true
}));

return processedData.map(d => ({ json: d }));`,
          },
          name: 'Process Data',
          type: 'n8n-nodes-base.function',
          typeVersion: 1,
          position: [450, 300],
        },
        {
          parameters: {
            respondWith: 'json',
            responseBody: '={{ { "success": true, "message": "Data processed" } }}',
          },
          name: 'Respond to Webhook',
          type: 'n8n-nodes-base.respondToWebhook',
          typeVersion: 1,
          position: [650, 300],
        },
      ],
      connections: {
        Webhook: {
          main: [[{ node: 'Process Data', type: 'main', index: 0 }]],
        },
        'Process Data': {
          main: [[{ node: 'Respond to Webhook', type: 'main', index: 0 }]],
        },
      },
      active: false,
      settings: {
        executionOrder: 'v1',
      },
      tags: [{ name: `company-${companyId}` }],
    };
  }

  // 测试 n8n 连接
  async testConnection() {
    try {
      // n8n 没有专门的健康检查端点，尝试获取工作流列表
      const response = await this.client.get('/api/v1/workflows');
      return {
        success: true,
        message: 'n8n connection successful',
        data: {
          workflowCount: response.data.data?.length || 0,
        },
      };
    } catch (error) {
      console.error('n8n connection test failed:', error.message);
      return {
        success: false,
        error: 'Failed to connect to n8n',
        message: error.message,
      };
    }
  }
}

module.exports = new N8nService();
