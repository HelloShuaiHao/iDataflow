Dataflow

预先安装node-red
通过命令行输入node-red启动本地服务

启动命令
cd docker
docker-compose up -d

n8n会监听5678端口，node-red端口可以在命令行看到

n8n默认管理员账号：
  - 邮箱：admin@idataflow.local
  - 密码：ChangeMe123!
  - 访问地址：http://localhost:5678

确保node-red 和 n8n部署完成以后 分别向其中导入json文件夹的flow文件

------------------------------------------------------------------------------------------
测试n8n 连接
curl -X POST http://localhost:5678/webhook/1158ae5b-867d-40ec-868c-5b3ffbdd7b7d \
  -H "Content-Type: application/json" \
  -d '{"test mode": "direct webhook test", "temperature": 25.5, "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}'

curl -X POST http://localhost:5678/webhook-test/1158ae5b-867d-40ec-868c-5b3ffbdd7b7d \
  -H "Content-Type: application/json" \
  -d '{"test mode": "direct webhook test", "temperature": 25.5, "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"}'



看 docker 日志
docker-compose logs -f backend                                                                                   


Node-red
启动命令：node-red
网址：
http://127.0.0.1:1880/

Was 触发：
wscat -c ws://localhost:1880/ws/example
传输数据
{"test mode": "test", "temperature": 25.5, "timestamp": "2023-10-01T12:00:00Z"}

Wss out:
wscat -c ws://localhost:1880/ws/example/out



自动化 Node-red wss 创建流程

步骤：
创建 channel
curl -X POST http://localhost:1880/channel
返回：
{"channelId":"mgqdjkffrwl71k","ws":"ws://localhost:1880/ws-relay?ch=mgqdjkffrwl71k"}%                                                                                             

用终端加入连接 加入监听
wscat -c ws://localhost:1880/ws

连接后在终端输出加入通道的命令
{"type":"join","channelId":"mgqh47f9rgn63b"}
{"type":"join","channelId":"mgqdjkffrwl71k"}

或者 
wscat -c 'ws://localhost:1880/ws-relay?ch=mgqdjkffrwl71k'

模拟n8n 向该通道推送数据
curl -X POST http://localhost:1880/relay \
     -H "Content-Type: application/json" \
     -d '{"channelId":"mgqdjkffrwl71k","data":{"msg":"Hello from n8n!"}}'



n8n 触发
curl -X POST http://localhost:5678/webhook/wss-relay \
     -H "Content-Type: application/json" \
     -d '{"sensor_id":"temp_001","value":23.5,"unit":"celsius"}'



curl -X POST http://localhost:5678/webhook-test/wss-relay \
     -H "Content-Type: application/json" \
     -d '{"sensor_id":"temp_001","value":23.5,"unit":"celsius"}'


curl -X POST http://localhost:5678/webhook-test/1158ae5b-867d-40ec-868c-5b3ffbdd7b7d \
     -H "Content-Type: application/json" \
     -d '{"sensor_id":"temp_001","value":23.5,"unit":"celsius"}'


—
1. WSS Data Input - 接收 webhook 数据到 /wss-relay
2. Check Channel Status - 检查是否已有频道
3. Has Channel? - 判断是否需要创建新频道
4. Create New Channel - 如果没有频道，调用 Node-RED 创建新频道
5. Store New Channel - 保存频道信息到工作流静态数据
6. Prepare Relay Data - 准备转发数据，添加 source: 'n8n_wss_auto'
7. Relay to Node-RED - 发送数据到 Node-RED
8. Success Response - 返回成功响应
