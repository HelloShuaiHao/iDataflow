const { query } = require('../config/database');

class Workflow {
  // 获取所有工作流
  static async findAll() {
    const result = await query(
      `SELECT * FROM workflows ORDER BY created_at DESC`
    );
    return result.rows;
  }

  // 根据 ID 获取工作流
  static async findById(id) {
    const result = await query(
      `SELECT * FROM workflows WHERE id = $1`,
      [id]
    );
    return result.rows[0];
  }

  // 根据 n8n workflow ID 获取工作流
  static async findByN8nId(n8nWorkflowId) {
    const result = await query(
      `SELECT * FROM workflows WHERE n8n_workflow_id = $1`,
      [n8nWorkflowId]
    );
    return result.rows[0];
  }

  // 创建工作流
  static async create(n8nWorkflowId, name, description = '', active = false) {
    const result = await query(
      `INSERT INTO workflows (n8n_workflow_id, name, description, active)
       VALUES ($1, $2, $3, $4) RETURNING *`,
      [n8nWorkflowId, name, description, active]
    );
    return result.rows[0];
  }

  // 更新工作流
  static async update(id, name, description, active) {
    const result = await query(
      `UPDATE workflows
       SET name = $1, description = $2, active = $3
       WHERE id = $4 RETURNING *`,
      [name, description, active, id]
    );
    return result.rows[0];
  }

  // 更新激活状态
  static async updateActive(n8nWorkflowId, active) {
    const result = await query(
      `UPDATE workflows
       SET active = $1
       WHERE n8n_workflow_id = $2 RETURNING *`,
      [active, n8nWorkflowId]
    );
    return result.rows[0];
  }

  // 删除工作流
  static async delete(id) {
    const result = await query(
      'DELETE FROM workflows WHERE id = $1 RETURNING *',
      [id]
    );
    return result.rows[0];
  }

  // 同步所有工作流（从 n8n）
  static async syncFromN8n(n8nWorkflows) {
    // 简单的同步策略：upsert
    const results = [];
    for (const wf of n8nWorkflows) {
      const existing = await this.findByN8nId(wf.id);
      if (existing) {
        // 更新
        const updated = await this.update(
          existing.id,
          wf.name,
          wf.description || '',
          wf.active || false
        );
        results.push(updated);
      } else {
        // 创建
        const created = await this.create(
          wf.id,
          wf.name,
          wf.description || '',
          wf.active || false
        );
        results.push(created);
      }
    }
    return results;
  }
}

module.exports = Workflow;
