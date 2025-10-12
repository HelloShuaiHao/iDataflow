const n8nService = require('../services/n8nService');
const Workflow = require('../models/Workflow');

// 测试 n8n 连接
exports.testConnection = async (req, res) => {
  try {
    const result = await n8nService.testConnection();
    res.json(result);
  } catch (error) {
    console.error('Error testing n8n connection:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to test n8n connection',
    });
  }
};

// 获取所有工作流（从数据库）
exports.getAllWorkflows = async (req, res) => {
  try {
    const workflows = await Workflow.findAll();
    res.json({
      success: true,
      data: workflows,
    });
  } catch (error) {
    console.error('Error fetching workflows:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to fetch workflows',
    });
  }
};

// 从 n8n 同步工作流
exports.syncWorkflows = async (req, res) => {
  try {
    // 从 n8n 获取所有工作流
    const n8nResult = await n8nService.getWorkflows();

    if (!n8nResult.success) {
      return res.status(500).json(n8nResult);
    }

    // 同步到数据库
    const synced = await Workflow.syncFromN8n(n8nResult.data);

    res.json({
      success: true,
      message: `Synced ${synced.length} workflows from n8n`,
      data: synced,
    });
  } catch (error) {
    console.error('Error syncing workflows:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to sync workflows',
    });
  }
};

// 获取单个工作流
exports.getWorkflow = async (req, res) => {
  try {
    const { workflowId } = req.params;

    // 从数据库获取
    const workflow = await Workflow.findById(workflowId);

    if (!workflow) {
      return res.status(404).json({
        success: false,
        error: 'Workflow not found',
      });
    }

    // 从 n8n 获取详细信息
    const n8nResult = await n8nService.getWorkflow(workflow.n8n_workflow_id);

    res.json({
      success: true,
      data: {
        ...workflow,
        n8nData: n8nResult.data,
      },
    });
  } catch (error) {
    console.error('Error fetching workflow:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to fetch workflow',
    });
  }
};

// 创建工作流
exports.createWorkflow = async (req, res) => {
  try {
    const { name, description, workflowData } = req.body;

    if (!workflowData) {
      return res.status(400).json({
        success: false,
        error: 'workflowData is required',
      });
    }

    // 在 n8n 中创建工作流
    const n8nResult = await n8nService.createWorkflow(workflowData);

    if (!n8nResult.success) {
      return res.status(500).json(n8nResult);
    }

    // 在数据库中记录工作流
    const dbWorkflow = await Workflow.create(
      n8nResult.data.id,
      name || workflowData.name,
      description || '',
      n8nResult.data.active || false
    );

    res.status(201).json({
      success: true,
      data: {
        workflow: dbWorkflow,
        n8nWorkflow: n8nResult.data,
      },
    });
  } catch (error) {
    console.error('Error creating workflow:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to create workflow',
    });
  }
};

// 更新工作流
exports.updateWorkflow = async (req, res) => {
  try {
    const { workflowId } = req.params;
    const workflowData = req.body;

    // 从数据库获取工作流
    const workflow = await Workflow.findById(workflowId);

    if (!workflow) {
      return res.status(404).json({
        success: false,
        error: 'Workflow not found',
      });
    }

    // 更新 n8n 中的工作流
    const n8nResult = await n8nService.updateWorkflow(
      workflow.n8n_workflow_id,
      workflowData
    );

    if (!n8nResult.success) {
      return res.status(500).json(n8nResult);
    }

    // 更新数据库记录
    const updated = await Workflow.update(
      workflowId,
      workflowData.name || workflow.name,
      workflowData.description || workflow.description,
      workflowData.active !== undefined ? workflowData.active : workflow.active
    );

    res.json({
      success: true,
      data: updated,
    });
  } catch (error) {
    console.error('Error updating workflow:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to update workflow',
    });
  }
};

// 删除工作流
exports.deleteWorkflow = async (req, res) => {
  try {
    const { workflowId } = req.params;

    // 从数据库获取工作流
    const workflow = await Workflow.findById(workflowId);

    if (!workflow) {
      return res.status(404).json({
        success: false,
        error: 'Workflow not found',
      });
    }

    // 删除 n8n 中的工作流
    await n8nService.deleteWorkflow(workflow.n8n_workflow_id);

    // 删除数据库记录
    await Workflow.delete(workflowId);

    res.json({
      success: true,
      message: 'Workflow deleted successfully',
    });
  } catch (error) {
    console.error('Error deleting workflow:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to delete workflow',
    });
  }
};

// 激活/停用工作流
exports.toggleWorkflow = async (req, res) => {
  try {
    const { workflowId } = req.params;
    const { active } = req.body;

    // 从数据库获取工作流
    const workflow = await Workflow.findById(workflowId);

    if (!workflow) {
      return res.status(404).json({
        success: false,
        error: 'Workflow not found',
      });
    }

    // 更新 n8n 中的状态
    const n8nResult = await n8nService.toggleWorkflow(
      workflow.n8n_workflow_id,
      active
    );

    if (!n8nResult.success) {
      return res.status(500).json(n8nResult);
    }

    // 更新数据库状态
    await Workflow.updateActive(workflow.n8n_workflow_id, active);

    res.json({
      success: true,
      message: `Workflow ${active ? 'activated' : 'deactivated'} successfully`,
    });
  } catch (error) {
    console.error('Error toggling workflow:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to toggle workflow',
    });
  }
};

// 执行工作流
exports.executeWorkflow = async (req, res) => {
  try {
    const { workflowId } = req.params;
    const data = req.body;

    // 从数据库获取工作流
    const workflow = await Workflow.findById(workflowId);

    if (!workflow) {
      return res.status(404).json({
        success: false,
        error: 'Workflow not found',
      });
    }

    // 执行工作流
    const result = await n8nService.executeWorkflow(
      workflow.n8n_workflow_id,
      data
    );

    res.json(result);
  } catch (error) {
    console.error('Error executing workflow:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to execute workflow',
    });
  }
};

// 获取执行历史
exports.getExecutions = async (req, res) => {
  try {
    const { workflowId } = req.params;
    const { limit = 10 } = req.query;

    // 从数据库获取工作流
    const workflow = await Workflow.findById(workflowId);

    if (!workflow) {
      return res.status(404).json({
        success: false,
        error: 'Workflow not found',
      });
    }

    // 获取执行历史
    const result = await n8nService.getExecutions(
      workflow.n8n_workflow_id,
      parseInt(limit)
    );

    res.json(result);
  } catch (error) {
    console.error('Error fetching executions:', error);
    res.status(500).json({
      success: false,
      error: 'Failed to fetch executions',
    });
  }
};
