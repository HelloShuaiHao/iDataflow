const express = require('express');
const router = express.Router();
const n8nController = require('../controllers/n8nController');

// n8n 连接测试
router.get('/test', n8nController.testConnection);

// 工作流管理
router.get('/workflows', n8nController.getAllWorkflows);
router.post('/workflows/sync', n8nController.syncWorkflows); // 从 n8n 同步工作流
router.get('/workflows/:workflowId', n8nController.getWorkflow);
router.post('/workflows', n8nController.createWorkflow);
router.patch('/workflows/:workflowId', n8nController.updateWorkflow);
router.delete('/workflows/:workflowId', n8nController.deleteWorkflow);

// 工作流控制
router.post('/workflows/:workflowId/toggle', n8nController.toggleWorkflow);
router.post('/workflows/:workflowId/execute', n8nController.executeWorkflow);

// 执行历史
router.get('/workflows/:workflowId/executions', n8nController.getExecutions);

module.exports = router;
