# AgentFree（Goldfish）项目日志

> 按时间顺序记录所有项目进展

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
