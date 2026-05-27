# 🐠 AgentFree (Goldfish)

**自主可控智能体框架 MVP** — 整合 Hermes + OpenClaw 特性，支持标准协议

---

## 项目概述

AgentFree 是一个自主可控的智能体管理框架，整合了 Hermes Agent 和 OpenClaw 的核心特性，支持 MCP、A2UI、AG-UI、Skills、A2A 等标准协议。

### 核心特性

- **快速启动**: 基于 React 18 + Vite + Ant Design 构建的管理后台
- **Agent 管理**: 全生命周期管理（创建、编辑、删除、启停）
- **协议支持**: MCP、A2UI、AG-UI、Skills、A2A 等标准协议
- **内置工具**: 文件操作、HTTP 请求、终端执行等工具集

## 技术栈

### 前端
- **框架**: React 18 + TypeScript
- **UI 库**: Ant Design
- **构建工具**: Vite 5.x
- **路由**: React Router

### 后端
- **框架**: .NET 8 (ASP.NET Core)
- **ORM**: EF Core
- **数据库**: SQLite (开发) / PostgreSQL (生产)
- **AI**: Microsoft.Extensions.AI

## 目录结构

```
AgentFree/
├── src/
│   ├── AgentFree.API/          # .NET API 后端
│   │   ├── Controllers/        # API 控制器
│   │   ├── Services/           # 业务逻辑服务
│   │   ├── DTOs/               # 数据传输对象
│   │   └── Program.cs          # 入口
│   └── AgentFree.Web/          # React 前端
│       ├── public/             # 静态资源
│       └── src/
│           ├── api/            # API 调用
│           ├── types/          # TypeScript 类型
│           └── views/          # 页面组件
├── .gitignore
└── README.md
```

## 快速开始

### 前置要求
- Node.js 18+
- .NET 8 SDK
- npm 或 yarn

### 后端启动
```bash
cd src/AgentFree.API
dotnet restore
dotnet run
# API 运行在 http://localhost:5201
```

### 前端启动
```bash
cd src/AgentFree.Web
npm install
npm run dev
# 前端运行在 http://localhost:5173
```

### 生产构建
```bash
cd src/AgentFree.Web
npm run build
# 构建输出到 dist/
```

## 部署

### 服务器部署结构
```
/opt/agentfree/
├── web/      ← Nginx 托管前端
├── api/      ← .NET API 服务
└── data/     ← 数据库文件
```

### Nginx 配置
```nginx
server {
    listen 443 ssl;
    server_name agent.ai.impx.net;

    ssl_certificate     /etc/letsencrypt/live/impx.net/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/impx.net/privkey.pem;

    root /var/www/agentfree/web;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://127.0.0.1:5201;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

## 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0.0 | 2026-05-28 | MVP 发布，基础 Agent 管理功能 |

## 许可证

MIT License
