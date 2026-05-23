# P003 — AgentFree（Goldfish）项目描述

> 自主可控智能体框架 | agent.ai.impx.net
> 最后更新：2026-05-22

## 基本信息

| 项 | 值 |
|----|-----|
| **项目编号** | P003 |
| **项目名称** | AgentFree |
| **项目代号** | Goldfish |
| **状态** | 🟢 进行中 |
| **负责人** | 志山 |
| **AI 助手** | hermes-coding（小马） |
| **开始日期** | 2026-05-05（原始）/ 2026-05-20（重构） |
| **本地源码** | `~/Projects/AgentFree/` |
| **文档路径** | `~/Projects/AgentFree/docs/` |
| **企业微信群聊ID** | （待确认） |

## 项目描述

自主可控智能体框架 MVP，支持标准协议（A2UI、AG-UI、MCP、Skills、A2A），React + Vite + .NET 10。

## 技术栈

- **前端**：React 18 + TypeScript + Vite
- **后端**：.NET 10 / PostgreSQL
- **测试**：xUnit

## 部署信息

### 服务器

| 项 | 值 |
|----|-----|
| **部署服务器** | zz.impx.net |
| **前端远程路径** | /var/www/agentfree/web/（Nginx serve 静态文件） |
| **API 远程路径** | /var/www/agentfree/api/ |
| **API 进程** | `dotnet AgentFree.API.dll` (127.0.0.1:5201) |
| **API systemd 服务** | ✅ `agentfree-api.service` |
| **API 端口** | 5201 |
| **HTTPS** | ✅ 通配证书 (impx.net) — `/etc/letsencrypt/live/impx.net/` |

### 域名

| 项 | 值 |
|----|-----|
| **域名** | agent.ai.impx.net |

## 核心文档

- `docs/requirements/agentfree-v1.md` — 需求文档（待编写）
- `docs/design/` — 设计文档（待编写）
- `docs/PROJECT-LOGS.md` — 项目日志
- `docs/PROJECT-STATUS.md` — 项目状态

## 项目群成员

> （待确认：请志山补充常一起沟通的群成员名单）
