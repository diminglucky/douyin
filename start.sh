#!/bin/bash
# 抖小云 一键启动脚本

cd "$(dirname "$0")"

# 配置 .NET 6 环境 (Homebrew 安装)
export PATH="/opt/homebrew/opt/dotnet@6/bin:$PATH"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@6/libexec"

# 激活 conda work 环境 (包含 ffmpeg)
eval "$(conda shell.bash hook)"
conda activate work

# 停止已有进程
echo "🔄 停止已有进程..."
pkill -f "dy.net" 2>/dev/null
sleep 1

# 检查前端是否已构建
if [ ! -d "app/dist" ]; then
    echo "📦 前端未构建，正在构建..."
    cd app
    npm install --legacy-peer-deps
    npm run build
    cd ..
fi

# 启动项目
echo ""
echo "🚀 正在启动抖小云..."
echo "📍 访问地址: http://localhost:10101"
echo "👤 默认账号: douyin / douyin2025"
echo ""
echo "按 Ctrl+C 停止服务"
echo "----------------------------------------"

dotnet run --environment Production
