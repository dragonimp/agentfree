# AgentFree（Goldfish）— 自主可控智能体框架

> 项目编号：P003 | 代号：Goldfish | 域名：agent.ai.impx.net

---

## 项目简介

自主可控智能体框架 MVP，整合 Hermes + OpenClaw 特性，支持标准协议（A2UI、AG-UI、MCP、Skills、A2A）。

## 技术栈

| 层级 | 技术 | 说明 |
|------|------|------|
| 前端 | React 18 + TypeScript + Vite | 管理面板 |
| 后端 | .NET 10 (C#) + EF Core | API 服务 |
| 数据库 | PostgreSQL | 数据存储 |
| 测试 | xUnit | 单元测试 |

## 项目结构

```
~/Projects/AgentFree/
├── src/
│   ├── AgentFree.API/          # .NET API 后端
│   │   ├── Controllers/        # API 控制器
│   │   ├── Core/               # 核心引擎（Agent/工具/协议）
│   │   ├── Models/             # 数据模型
│   │   ├── Services/           # 业务服务
│   │   └── Program.cs          # 启动配置
│   ├── AgentFree.Web/          # React 前端
│   │   ├── src/                # 源码
│   │   └── dist/               # 构建产物
│   └── AgentFree.Tests/        # xUnit 测试
├── docs/                       # 项目文档
│   ├── PROJECT-LOGS.md         # 进展日志
│   ├── PROJECT-STATUS.md       # 状态摘要
│   ├── TASK-BREAKDOWN.md       # 任务分解
│   ├── requirements/           # 需求文档
│   ├── design/                 # 设计文档
│   ├── code-review/            # 代码审查
│   └── reports/                # 报告
├── deploy/                     # 部署配置
├── scripts/                    # 工具脚本
└── README.md                   # 本文件
```

## 部署信息

| 项 | 值 |
|---|---|
| 域名 | https://agent.ai.impx.net/ |
| 服务器 | zz.impx.net (root SSH) |
| API 端口 | 待分配 |
| 静态目录 | /var/www/agent-free/ |

## 快速开始

```bash
# 后端
cd src/AgentFree.API && dotnet run

# 前端
cd src/AgentFree.Web && npm install && npm run dev
```

## 当前状态

- **阶段**：需求调研中
- **已完成**：项目初始化（前后端脚手架、测试项目）
- **进行中**：需求文档编写、技术方案设计
- **待开始**：核心引擎实现、渠道适配器、管理面板

---

> 项目规范详见 [PROJECT-GUIDANCE.md](../../PROJECT-GUIDANCE.md)
