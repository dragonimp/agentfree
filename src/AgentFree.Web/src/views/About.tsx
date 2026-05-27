import React, { useState, useEffect } from 'react'
import { Card, Typography, Tag, Divider, Space, Button, Spin, Alert, Tooltip } from 'antd'
import { 
  ClockCircleOutlined, 
  CodeOutlined, 
  CloudServerOutlined, 
  ToolOutlined,
  InfoCircleOutlined,
  CheckCircleOutlined,
  LoadingOutlined,
  RocketOutlined,
} from '@ant-design/icons'
import type { VersionInfo } from '@/types'

const { Title, Paragraph, Text } = Typography

const versionApiUrl = '/version.json'

export default function About() {
  const [versionInfo, setVersionInfo] = useState<VersionInfo | null>(null)
  const [currentVersion, setCurrentVersion] = useState<VersionInfo | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string>('')

  useEffect(() => {
    loadVersions()
  }, [])

  const loadVersions = async () => {
    setLoading(true)
    try {
      // 加载当前运行的版本信息
      const currentRes = await fetch('/version.json')
      const currentData: VersionInfo = await currentRes.json()
      setVersionInfo(currentData)
      setCurrentVersion(currentData)
      
      // 尝试加载最新版本（实际项目中可以从 GitHub 或 CDN 获取）
      const latestRes = await fetch('/version.json?t=' + Date.now())
      const latestData: VersionInfo = await latestRes.json()
      setCurrentVersion(latestData)
    } catch (err: any) {
      setError('加载版本信息失败: ' + (err.message || '未知错误'))
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
        <Spin size="large" tip="加载中..." />
      </div>
    )
  }

  if (error) {
    return (
      <Alert
        message="加载失败"
        description={error}
        type="error"
        showIcon
        action={
          <Button size="small" onClick={loadVersions}>
            重试
          </Button>
        }
      />
    )
  }

  const isVersionMatch = versionInfo?.version === currentVersion?.version
  
  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 20px' }}>
      {/* Header */}
      <div style={{ marginBottom: 32 }}>
        <Title level={2} style={{ margin: 0 }}>
          <RocketOutlined style={{ marginRight: 12 }} />
          关于 AgentFree
        </Title>
        <Text type="secondary">版本信息与项目介绍</Text>
      </div>

      {/* Version Info Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: 24, marginBottom: 32 }}>
        <Card 
          title="当前运行版本" 
          bordered={false}
          style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.06)' }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
            <Tag color="blue" style={{ fontSize: 16, padding: '4px 12px', borderRadius: 20 }}>
              v{versionInfo?.version}
            </Tag>
            {isVersionMatch ? (
              <Tag icon={<CheckCircleOutlined />} color="success">版本一致</Tag>
            ) : (
              <Tag icon={<InfoCircleOutlined />} color="warning">版本不一致</Tag>
            )}
          </div>
          <div style={{ marginBottom: 8 }}>
            <Text type="secondary">构建日期：</Text>
            <Text>{new Date(versionInfo?.buildDate || '').toLocaleString('zh-CN')}</Text>
          </div>
          <div>
            <Text type="secondary">构建号：</Text>
            <Text>#{versionInfo?.buildNumber || 'N/A'}</Text>
          </div>
        </Card>

        <Card 
          title="最新版本" 
          bordered={false}
          style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.06)' }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
            <Tag color="green" style={{ fontSize: 16, padding: '4px 12px', borderRadius: 20 }}>
              v{currentVersion?.version}
            </Tag>
            <Tag color="default">当前发布版本</Tag>
          </div>
          <div style={{ marginBottom: 8 }}>
            <Text type="secondary">构建日期：</Text>
            <Text>{new Date(currentVersion?.buildDate || '').toLocaleString('zh-CN')}</Text>
          </div>
          <div>
            <Text type="secondary">构建号：</Text>
            <Text>#{currentVersion?.buildNumber || 'N/A'}</Text>
          </div>
        </Card>
      </div>

      {/* Version Comparison */}
      <Card 
        title="版本对比" 
        bordered={false}
        style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.06)', marginBottom: 32 }}
      >
        {isVersionMatch ? (
          <Alert
            message="部署版本与构建版本一致"
            description="当前运行的版本与最新版本匹配，部署成功！"
            type="success"
            showIcon
            icon={<CheckCircleOutlined />}
          />
        ) : (
          <Alert
            message="版本不一致"
            description={`当前版本 v${versionInfo?.version} 与最新版本 v${currentVersion?.version} 不匹配`}
            type="warning"
            showIcon
            icon={<InfoCircleOutlined />}
          />
        )}
        <Button 
          type="primary" 
          icon={<LoadingOutlined />}
          onClick={loadVersions}
          style={{ marginTop: 16 }}
        >
          刷新版本信息
        </Button>
      </Card>

      <Divider />

      {/* Tech Stack */}
      <Card 
        title="技术栈" 
        bordered={false}
        style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.06)', marginBottom: 32 }}
      >
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 16 }}>
          {versionInfo?.techStack?.map((tech, index) => (
            <Card 
              key={index} 
              size="small"
              style={{ borderRadius: 8, border: '1px solid #f0f0f0' }}
            >
              <Space>
                <CodeOutlined style={{ color: '#1890ff' }} />
                <Text>{tech}</Text>
              </Space>
            </Card>
          ))}
        </div>
      </Card>

      {/* Features */}
      <Card 
        title="核心功能" 
        bordered={false}
        style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.06)', marginBottom: 32 }}
      >
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', gap: 16 }}>
          {versionInfo?.features?.map((feature, index) => (
            <Card 
              key={index} 
              size="small"
              style={{ borderRadius: 8, border: '1px solid #f0f0f0' }}
            >
              <Space>
                <ToolOutlined style={{ color: '#52c41a' }} />
                <Text>{feature}</Text>
              </Space>
            </Card>
          ))}
        </div>
      </Card>

      {/* Description */}
      <Card 
        title="项目介绍" 
        bordered={false}
        style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.06)' }}
      >
        <Paragraph>
          <Text type="secondary">
            AgentFree 是一个自主可控的智能体框架，整合了 Hermes 和 OpenClaw 的特性，
            支持多种标准协议（MCP、A2UI、AG-UI、Skills、A2A）。
          </Text>
        </Paragraph>
        <Paragraph>
          <Text type="secondary">
            项目采用现代化的技术栈，前端使用 React 18 + Vite + Ant Design，
            后端使用 .NET 10 + Entity Framework Core，数据库支持 PostgreSQL 和 InMemory。
          </Text>
        </Paragraph>
      </Card>

      {/* Footer */}
      <div style={{ textAlign: 'center', marginTop: 48, padding: '24px 0', borderTop: '1px solid #f0f0f0' }}>
        <Space direction="vertical" size="small">
          <Text type="secondary">
            <ClockCircleOutlined /> 最后更新：{new Date().toLocaleString('zh-CN')}
          </Text>
          <Text type="secondary">
            构建环境：Node.js / .NET 10
          </Text>
        </Space>
      </div>
    </div>
  )
}
