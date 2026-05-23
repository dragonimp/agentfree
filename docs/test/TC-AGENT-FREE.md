# P003 AgentFree 测试案例

> 最后更新：2026-05-24
> 测试环境：zz.impx.net

## 测试环境

| 项 | 值 |
|----|-----|
| **前端域名** | https://agent.ai.impx.net / https://agentfree.ai.impx.net |
| **API 端点** | http://localhost:5200/api/models, http://localhost:5200/api/chat/completions |

---

## P 核心功能

### TC-P003-001 主页访问
- **优先级**: P0
- **步骤**: 访问 https://agent.ai.impx.net
- **预期**: HTTP 200，页面正常加载

### TC-P003-002 后端 API 健康检查
- **优先级**: P0
- **步骤**: curl -s http://localhost:5200/api/health
- **预期**: HTTP 200，返回健康状态

### TC-P003-003 模型列表接口
- **优先级**: P0
- **步骤**: curl -s http://localhost:5200/api/models
- **预期**: HTTP 200，返回模型列表

### TC-P003-004 对话接口
- **优先级**: P0
- **步骤**: POST http://localhost:5200/api/chat/completions
- **预期**: HTTP 200/401，接受对话请求

## P1 辅助功能

### TC-P003-005 双域名访问
- **优先级**: P1
- **步骤**: 分别访问 agent.ai.impx.net 和 agentfree.ai.impx.net
- **预期**: 两个域名均返回 HTTP 200
