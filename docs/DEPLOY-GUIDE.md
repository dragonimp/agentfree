# 部署指南

> 每次部署完成后必须：1) 完成全部测试验证；2) 发送部署测试报告；3) 将互联网访问地址发送给志山。
>
> 部署规则：统一使用 `/opt/<项目>/` 结构，内部分 web/、api/、data/ 三个平行子目录。备份放到 `/opt/backups/`。

---

## 项目基本信息

|| 项 | 值 |
|--|---|
|| **项目清单** | `~/Projects/PROJECT-LIST.md` |
|| **远程服务器** | zz.impx.net |
|| **域名** | https://agent.ai.impx.net/ |
|| **API 域名** | https://agent.ai.impx.net/api/ |
|| **项目根目录** | /opt/agentfree/ |
|| ├─ 前端 (web) | /opt/agentfree/web/ |
|| ├─ 后端 (api) | /opt/agentfree/api/ |
|| ├─ 数据 (data) | /opt/agentfree/data/ |
|| **备份目录** | /opt/backups/ |
|| **API 端口** | 待分配 |
|| **HTTPS** | ✅ 通配证书 (impx.net) |

---

## 部署步骤

### 1. 编译发布

```bash
# 后端
cd ~/Projects/AgentFree/src
dotnet publish AgentFree.API/AgentFree.API.csproj -c Release -o ./publish

# 前端
cd ~/Projects/AgentFree/src/AgentFree.Web
npm install
npm run build
```

### 2. 打包上传

```bash
# 后端
cd ~/Projects/AgentFree/src
tar -czf /tmp/agentfree-api.tar.gz publish/
scp /tmp/agentfree-api.tar.gz root@zz.impx.net:/tmp/

# 前端
cd ~/Projects/AgentFree/src/AgentFree.Web
tar -czf /tmp/agentfree-web.tar.gz dist/
scp /tmp/agentfree-web.tar.gz root@zz.impx.net:/tmp/
```

### 3. 远程部署

```bash
ssh root@zz.impx.net << 'EOF'
# 创建项目目录结构
mkdir -p /opt/agentfree/{web,api,data} /opt/backups

# 部署前端
cd /opt/agentfree/web && rm -rf * && tar -xzf /tmp/agentfree-web.tar.gz -C /opt/agentfree/web/ && rm /tmp/agentfree-web.tar.gz

# 部署后端
mkdir -p /opt/agentfree/api
cd /opt/agentfree/api && rm -rf * && tar -xzf /tmp/agentfree-api.tar.gz -C /opt/agentfree/api/ && rm /tmp/agentfree-api.tar.gz
EOF
```

### 4. 备份

```bash
ssh root@zz.impx.net << 'EOF'
# 备份当前部署版本
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
tar -czf /opt/backups/agentfree_backup_${TIMESTAMP}.tar.gz -C /opt agentfree/
echo "Backup: /opt/backups/agentfree_backup_${TIMESTAMP}.tar.gz"
EOF
```

### 5. 启动 API 服务

```bash
ssh root@zz.impx.net << 'EOF'
cd /opt/agentfree/api && dotnet AgentFree.API.dll
EOF
```

### 6. 测试验证

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
项目目录：/opt/agentfree/

🔗 访问地址：https://agent.ai.impx.net/

页面测试：✅
API 测试：✅

请用户访问测试。
```
