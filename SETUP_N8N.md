# n8n 设置指南

## 问题：打开 n8n 显示注册界面

n8n 最新版本需要先创建第一个管理员账户才能使用。以下是完整的设置步骤。

## 方法 1：通过 Web 界面注册（推荐）

### 步骤 1：访问 n8n

打开浏览器，访问：http://localhost:5678

### 步骤 2：注册第一个用户

在注册页面填写以下信息：
- **Email**: admin@idataflow.local
- **First Name**: Admin
- **Last Name**: User
- **Password**: Admin123（或你自己设定的安全密码）

点击"Create account"完成注册。

### 步骤 3：登录 n8n

注册完成后，你会自动登录到 n8n 界面。

### 步骤 4：生成 API Key（重要！）

为了让后端服务能够调用 n8n API，你需要生成 API Key：

1. 在 n8n 界面，点击右上角的用户头像
2. 选择 **Settings** (设置)
3. 在左侧菜单选择 **API**
4. 点击 **Create API Key**
5. 给 API Key 一个名称，比如 "Backend Service"
6. 复制生成的 API Key（只会显示一次！）

### 步骤 5：配置后端服务

将生成的 API Key 添加到后端配置文件：

```bash
# 编辑 backend/.env 文件
cd backend
nano .env
```

更新以下行：
```env
N8N_API_KEY=你的API密钥
```

### 步骤 6：重启后端服务

```bash
cd ../docker
docker-compose restart backend
```

### 步骤 7：测试连接

```bash
curl http://localhost:3000/api/n8n/test
```

应该返回成功响应：
```json
{
  "success": true,
  "message": "n8n connection successful",
  "data": {
    "workflowCount": 0
  }
}
```

## 方法 2：使用环境变量预设用户（自动化部署）

如果你想在部署时自动创建用户，可以在 `docker-compose.yml` 中添加以下环境变量：

```yaml
n8n:
  environment:
    # ... 其他配置 ...
    - N8N_OWNER_EMAIL=admin@idataflow.local
    - N8N_OWNER_PASSWORD=admin123
```

但是，这种方法在 n8n 最新版本中可能不再支持。推荐使用方法 1。

## 常见问题

### Q1: 我忘记了 n8n 的登录密码怎么办？

**方案 A：重置 n8n 数据（会丢失所有工作流）**
```bash
cd docker
docker-compose down
docker volume rm docker_n8n_data
docker-compose up -d
```

**方案 B：通过数据库重置密码**
```bash
# 进入 PostgreSQL 容器
docker exec -it idataflow-postgres psql -U postgres -d n8n

# 查看用户
SELECT * FROM public.user;

# 注意：直接修改密码需要加密，建议使用方案 A
```

### Q2: API 调用返回 401 错误

检查以下几点：
1. 确保你已经在 n8n 中生成了 API Key
2. 检查 `backend/.env` 文件中的 `N8N_API_KEY` 是否正确
3. 重启后端服务：`docker-compose restart backend`

### Q3: 无法访问 n8n 界面

检查服务状态：
```bash
cd docker
docker-compose ps
docker-compose logs n8n
```

确保 n8n 容器正在运行且健康。

### Q4: 如何查看 API Key 权限？

n8n 的 API Key 默认具有完整的 API 访问权限，可以：
- 创建、读取、更新、删除工作流
- 执行工作流
- 查看执行历史
- 管理凭证（如果需要）

## 推荐的生产环境配置

在生产环境中，建议：

1. **使用强密码**
   - Email: 使用真实的管理员邮箱
   - Password: 至少 12 位，包含大小写字母、数字和特殊字符

2. **配置 HTTPS**
   ```yaml
   n8n:
     environment:
       - N8N_PROTOCOL=https
       - N8N_HOST=your-domain.com
   ```

3. **限制 API 访问**
   - 使用防火墙规则限制 n8n 端口只能被后端服务访问
   - 定期轮换 API Key

4. **备份数据**
   ```bash
   # 备份 PostgreSQL 数据库
   docker exec idataflow-postgres pg_dump -U postgres n8n > n8n_backup.sql

   # 备份 n8n 数据卷
   docker run --rm -v docker_n8n_data:/data -v $(pwd):/backup \
     alpine tar czf /backup/n8n_data_backup.tar.gz /data
   ```

## 快速测试 n8n API

注册并配置 API Key 后，可以使用以下命令测试：

```bash
# 测试连接
curl http://localhost:3000/api/n8n/test

# 获取所有工作流
curl http://localhost:3000/api/n8n/workflows

# 获取所有公司
curl http://localhost:3000/api/companies

# 为公司创建工作流
curl -X POST http://localhost:3000/api/n8n/workflows/company/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "测试工作流",
    "description": "这是一个测试工作流"
  }'
```

## 下一步

1. ✅ 注册 n8n 账户
2. ✅ 生成 API Key
3. ✅ 配置后端服务
4. ✅ 测试 API 连接
5. 📝 开始创建工作流
6. 🔌 集成 WebSocket 数据流
7. 🚀 部署到生产环境

需要帮助？查看 [n8n 官方文档](https://docs.n8n.io/)
