# 抖小云 (dysync.net) 本地开发启动指南

## 环境要求

| 环境 | 版本 | 安装命令 (macOS) |
|------|------|-----------------|
| .NET SDK | 6.0+ | `brew install dotnet@6` |
| Node.js | 18+ | `brew install node` |
| npm | 10+ | 随Node.js安装 |

## 环境配置 (macOS)

如果使用 Homebrew 安装的 .NET 6，需要配置环境变量：

```bash
# 添加到 ~/.zshrc 或 ~/.bash_profile
export PATH="/opt/homebrew/opt/dotnet@6/bin:$PATH"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@6/libexec"
```

配置后执行 `source ~/.zshrc` 使其生效。

---

## 快速启动

### 1. 安装后端依赖
```bash
cd /Users/diming/code/douyin
dotnet restore
```

### 2. 安装前端依赖
```bash
cd app
npm install --legacy-peer-deps
```

### 3. 构建前端
```bash
npm run build
```

### 4. 启动项目
```bash
cd ..
dotnet run --environment Production
```

### 5. 访问
- 地址: http://localhost:10101
- 账号: `douyin`
- 密码: `douyin2025`

---

## 一键启动脚本

创建 `start.sh` 文件：

```bash
#!/bin/bash
cd "$(dirname "$0")"

# 配置 .NET 环境
export PATH="/opt/homebrew/opt/dotnet@6/bin:$PATH"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@6/libexec"

# 停止已有进程
pkill -f "dy.net" 2>/dev/null

# 启动项目
echo "正在启动抖小云..."
dotnet run --environment Production
```

赋予执行权限并运行：
```bash
chmod +x start.sh
./start.sh
```

---

## 目录结构

```
douyin/
├── app/                    # Vue.js 前端
│   ├── src/               # 源码
│   └── dist/              # 构建输出 (需要先执行 npm run build)
├── Controllers/           # API 控制器
├── service/               # 业务逻辑
├── repository/            # 数据仓储
├── db/                    # SQLite 数据库
│   └── dy.sqlite
├── logs/                  # 日志目录
├── collect/               # 收藏视频存储
├── favorite/              # 喜欢视频存储
├── uper/                  # 博主视频存储
├── images/                # 图文视频存储
└── Program.cs             # 入口文件
```

---

## 开发模式 vs 生产模式

| 模式 | 命令 | 特点 |
|------|------|------|
| 开发 | `dotnet run` | 启用Swagger、热重载、前端需另起服务 |
| 生产 | `dotnet run --environment Production` | SPA静态文件服务、定时任务自动启动 |

**推荐使用生产模式**，因为前端SPA只在生产模式下被正确加载。

---

## 常见问题

### Q: 页面显示空白？
A: 确保前端已构建 (`npm run build`) 并使用 `--environment Production` 启动。

### Q: npm install 报错？
A: 使用 `npm install --legacy-peer-deps` 解决依赖冲突。

### Q: 日志文件找不到？
A: 确保 `logs/` 目录存在，程序会自动创建日志文件。

### Q: 同步任务不执行？
A: 检查是否配置了有效的抖音Cookie（在"抖音授权"页面配置）。

---

## 端口配置

默认端口：`10101`

修改方式：
1. 环境变量: `ASPNETCORE_URLS=http://*:8080`
2. appsettings.json 配置

---

## 停止项目

```bash
pkill -f "dy.net"
```

或按 `Ctrl+C` 终止运行中的进程。
