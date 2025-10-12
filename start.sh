#!/bin/bash

echo "========================================"
echo "🚀 Starting iDataflow Platform"
echo "========================================"

# 检查 Docker 是否安装
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install Docker Compose first."
    exit 1
fi

echo "✅ Docker and Docker Compose are installed"

# 检查后端 .env 文件
if [ ! -f "backend/.env" ]; then
    echo "⚠️  .env file not found. Creating from example..."
    cp backend/.env.example backend/.env
    echo "✅ Created backend/.env file. Please review and update it."
fi

# 启动服务
echo ""
echo "📦 Starting services..."
cd docker

docker-compose up -d

echo ""
echo "⏳ Waiting for services to be ready..."
sleep 10

# 检查服务状态
echo ""
echo "📊 Service Status:"
docker-compose ps

echo ""
echo "========================================"
echo "✅ iDataflow Platform is running!"
echo "========================================"
echo ""
echo "🌐 Access URLs:"
echo "   - Backend API:    http://localhost:3000"
echo "   - Health Check:   http://localhost:3000/health"
echo "   - n8n Interface:  http://localhost:5678"
echo "   - n8n Login:      admin / admin123"
echo "   - WebSocket:      ws://localhost:3001"
echo ""
echo "📚 Quick Tests:"
echo "   curl http://localhost:3000/health"
echo "   curl http://localhost:3000/api/companies"
echo "   curl http://localhost:3000/api/n8n/test"
echo "   curl http://localhost:3000/api/websocket/stats"
echo ""
echo "📖 View logs:"
echo "   docker-compose logs -f backend"
echo "   docker-compose logs -f n8n"
echo "   docker-compose logs -f postgres"
echo ""
echo "🛑 Stop services:"
echo "   docker-compose down"
echo ""
echo "========================================"
