# 部署指南

> 每次部署完成后必须：1) 完成全部测试验证；2) 发送部署测试报告；3) 将互联网访问地址发送给志山。

---

## 项目基本信息

| 项 | 值 |
|----|-----|
| **项目清单** | `~/Projects/PROJECT-LIST.md` |
| **远程服务器** | zz.impx.net |
| **域名** | https://agent.ai.impx.net/ |
| **API 域名** | https://agent.ai.impx.net/api/ |
| **前端远程路径** | /var/www/agent-free/dist/ |
| **API 端口** | 5200 |
| **HTTPS** | ✅ 通配证书 (impx.net) |

---

## 部署步骤

### 1. 编译发布

```bash
cd ~/Projects/AgentFree/src
dotnet publish AgentFree.csproj -c Release -o ./publish
```

### 2. 打包上传

```bash
tar -czf /tmp/agent-free.tar.gz publish/
scp /tmp/agent-free.tar.gz root@zz.impx.net:/tmp/
```

### 3. 远程部署

```bash
ssh root@zz.impx.net "cd /var/www/agent-free/dist && rm -rf * && tar -xzf /tmp/agent-free.tar.gz -C /var/www/agent-free/dist/ && rm /tmp/agent-free.tar.gz"
```

### 4. 启动 API 服务

```bash
ssh root@zz.impx.net "cd /var/www/agent-free/ && ASPNETCORE_URLS='http://localhost:5200' dotnet AgentFree.API.dll"
```

### 5. 测试验证

```bash
curl -s -o /dev/null -w "HTTP %{http_code}" https://agent.ai.impx.net/
curl -s -o /dev/null -w "HTTP %{http_code}" https://agent.ai.impx.net/api/
```

---

## 部署完成通知

发送给志山：

```
📋 AgentFree 部署测试报告

部署时间：YYYY-MM-DD HH:MM
远程服务器：zz.impx.net
域名：https://agent.ai.impx.net/

🔗 访问地址：https://agent.ai.impx.net/

页面测试：✅
API 测试：✅

请用户访问测试。
```
