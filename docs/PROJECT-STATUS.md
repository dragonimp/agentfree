# AgentFree（Goldfish）项目状态

> 最后更新：2026-05-24

## 项目信息

|| 项 | 值 |
|--|---|
|| **项目编号** | P003 |
|| **项目代号** | Goldfish |
|| **项目名称** | AgentFree |
|| **状态** | 🟢 开发中 — 聊天功能已上线 |
|| **负责人** | 志山 |
|| **AI 助手** | @图灵IT-研发马儿 |
|| **开始日期** | 2026-05-05（原始）|
|| **重构日期** | 2026-05-20 |
|| **描述** | 自主可控智能体框架 MVP，React + Vite + .NET 10，支持标准协议 |
|| **域名** | `agent.ai.impx.net` |
|| **技术栈** | React 18 + TypeScript + Vite / .NET 10 / PostgreSQL |
|| **路径** | `~/Projects/AgentFree/` |
|| **服务器目录** | /opt/agentfree/ (web/api/data) |
|| **备份目录** | /opt/backups/ |

---

## 当前阶段：功能开发

### 已完成

- [x] 删除旧 AGENT-FREE（Blazor）项目
- [x] 重构为 React + .NET 技术栈
- [x] 项目初始化（目录结构、解决方案文件）
- [x] 前端 React + TS + Vite 初始化完成
- [x] 后端 .NET 10 Web API 初始化完成
- [x] 测试项目 xUnit 初始化完成
- [x] 版本管理（Git + GitHub）
- [x] 部署目录规范统一（/opt/agentfree/ web/api/data）
- [x] 需求文档编写（docs/requirements/02-需求文档.md）
- [x] 技术方案设计（docs/design/architecture.md）
- [x] **聊天会话管理功能**：
  - 后端 ChatController（流式聊天 API + 模型查询）
  - 前端 Sessions.tsx（会话列表 CRUD）
  - 前端 Chat.tsx（Markdown + 流式 SSE 响应）
  - 路由集成（/sessions, /chat/:sessionId）
  - 服务器部署测试全部通过

### 进行中

- [ ] AG-UI/A2UI 协议对接实现
- [ ] 智能体运行时核心（Agentic Loop Engine 优化）
- [ ] MCP Server 基础能力

### 待开始

- [ ] A2A 协议支持
- [ ] 技能系统标准化
- [ ] 监控面板开发
- [ ] 集成测试
- [ ] 用户验收测试

---

## 关键决策

1. **技术栈变更**：前端从 Blazor Server 改为 React + TypeScript + Vite
2. **项目重构**：删除旧代码，从零开始初始化
3. **部署规范**：统一使用 `/opt/<项目>/` 结构，内部分 web/、api/、data/ 三个平行子目录

---

## 阻塞项

无

---

## 下一步

1. 编写需求文档 `docs/requirements/agentfree-v1.md`
2. 编写技术方案设计
3. 确认功能范围和优先级
