# AgentFree（Goldfish）项目日志

> 按时间顺序记录所有项目进展

---

## 2026-05-26

### 02:30 — 新增聊天会话管理和流式聊天功能

- **新增后端 ChatController**：
  - `GET /api/chat/models` — 获取智能体可用模型
  - `POST /api/chat/stream` — AG-UI 流式聊天端点（SSE Server-Sent Events）
- **新增前端组件**：
  - `Sessions.tsx` — 会话列表页（创建/删除/跳转聊天）
  - `Chat.tsx` — 聊天对话页（Markdown 渲染 + 流式响应）
  - `api/index.ts` 新增 `streamChat()` SSE 流式聊天函数
- **路由更新**：App.tsx 新增 `/sessions` 和 `/chat/:sessionId` 路由
- **部署**：
  - 本地构建前端 dist/ + 后端 publish/
  - 上传到服务器 zz.impx.net
  - 部署到 /var/www/agentfree/web/ + /var/www/agentfree/api/
  - 同步到 /opt/agentfree/ 规范目录
  - 备份到 /opt/backups/agentfree_backup_20260526_023401.tar.gz
  - Nginx 代理端口修正为 5000
  - API 健康检查 ✅ HTTP 200
  - Sessions API ✅ HTTP 200
  - Chat Models API ✅ HTTP 200
  - 前端 ✅ HTTP 200

### 03:xx — 新增适配器层实现 Hermes/Goldfish 双适配器支持

- **新增适配器架构**：
  - 定义 IAdapterService 接口（标准模式 + 流式模式）
  - 实现 GoldfishAdapter（对接本地 LLM）
  - 实现 HermesAdapter（对接 Hermes API Server）
- **ChatController 重构**：
  - 注入 IAdapterService 替代直接使用 IChatClient
  - 根据 Agent.AgentType 自动选择适配器
  - 保留原有 Session 管理和消息持久化逻辑
- **DI 注册**：
  - Program.cs 注册 IAdapterService -> GoldfishAdapter（默认）
  - 支持后续扩展更多适配器
- **前端聊天界面完善**：
  - ChatLayout.tsx：左侧侧边栏会话管理 + 移动端 Drawer
  - ChatView.tsx：流式 SSE 聊天 + Markdown 渲染
  - App.tsx：路由集成 /sessions, /chat/:sessionId
- **构建部署**：
  - 前后端均编译通过
  - 部署到服务器 agent.ai.impx.net
  - 所有 API 端点验证通过

---

## 2026-05-24

### 09:35 — 版本管理 & 部署规范统一

- 初始化 Git 仓库，首次 commit (16733be)
- 远程 GitHub 仓库：https://github.com/dragonimp/agentfree
- 按 `PROJECT-GUIDANCE.md` 规范完善文档结构：
  - 补充 `docs/code-review/` 和 `docs/reports/` 目录
  - 更新 `README.md`（含部署信息、项目结构）
- 统一服务器部署目录规范：
  - `/opt/agentfree/` — 项目根目录
    - `web/` — 前端静态文件（Nginx serve）
    - `api/` — .NET API 后端
    - `data/` — PostgreSQL 数据
  - `/opt/backups/` — 公共备份目录
- 更新 `DEPLOY-GUIDE.md` 适配新部署路径
- 更新 `PROJECT-STATUS.md` 记录部署规范决策

---

## 2026-05-20

- 志山要求删除旧项目，改用 React 重构
- 删除旧 ~/Projects/AGENT-FREE/ 目录
- 创建新项目 ~/Projects/AgentFree/
- 初始化前端 React + TypeScript + Vite
- 初始化后端 .NET 10 Web API
- 创建测试项目 xUnit
- 创建解决方案文件 AgentFree.slnx
- 项目代号确定为 **Goldfish**（志山起名）
- 开始编写需求文档

---

## 2026-05-05

- 项目初始化为 AGENT-FREE
- 技术栈：.NET 10 + Blazor Server + PostgreSQL
- 创建基础脚手架代码
- 状态：调研阶段

---
