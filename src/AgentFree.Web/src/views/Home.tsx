import React from 'react'
import { Card, Typography, Row, Col } from 'antd'
import {
  RocketOutlined,
  TeamOutlined,
  CodeOutlined,
  CloudServerOutlined,
} from '@ant-design/icons'

const { Title, Paragraph, Text } = Typography

const features = [
  { icon: <RocketOutlined style={{ fontSize: 32, color: '#1890ff' }} />, title: '快速启动', desc: 'React 18 + Vite + Ant Design 构建管理后台', color: '#e6f7ff', borderColor: '#91d5ff' },
  { icon: <TeamOutlined style={{ fontSize: 32, color: '#52c41a' }} />, title: 'Agent 管理', desc: '全生命周期管理：创建、编辑、删除、启停', color: '#f6ffed', borderColor: '#b7eb8f' },
  { icon: <CodeOutlined style={{ fontSize: 32, color: '#faad14' }} />, title: '协议支持', desc: 'MCP、A2UI、AG-UI、Skills、A2A 等标准协议', color: '#fff7e6', borderColor: '#ffd591' },
  { icon: <CloudServerOutlined style={{ fontSize: 32, color: '#722ed1' }} />, title: '内置工具', desc: '文件操作、HTTP 请求、终端执行等工具集', color: '#f9f0ff', borderColor: '#d3adf7' },
]

export default function Home() {
  const isMobile = typeof window !== 'undefined' && window.innerWidth <= 768
  const cardStyle = { marginBottom: 16, borderRadius: 8 }
  const paddingStyle = { padding: isMobile ? 16 : 24, background: '#fff', borderRadius: 8, boxShadow: '0 1px 2px rgba(0,0,0,0.06)' }

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: isMobile ? 0 : 0 }}>
      {/* Hero Banner */}
      <Card style={{ marginBottom: 24, background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', borderRadius: 12 }}>
        <div style={{ padding: isMobile ? 20 : 40, textAlign: 'center', color: '#fff' }}>
          <div style={{ fontSize: isMobile ? 48 : 64, marginBottom: 16 }}>🐠</div>
          <Title level={2} style={{ color: '#fff', margin: '0 0 12px 0', fontSize: isMobile ? 24 : 32 }}>
            AgentFree (Goldfish)
          </Title>
          <Paragraph style={{ color: 'rgba(255,255,255,0.85)', fontSize: isMobile ? 14 : 16, margin: 0 }}>
            自主可控智能体框架 MVP — 整合 Hermes + OpenClaw 特性，支持标准协议
          </Paragraph>
        </div>
      </Card>

      {/* Features */}
      <div style={{ marginBottom: 24 }}>
        <Title level={4} style={{ marginBottom: 16 }}>核心功能</Title>
        <Row gutter={[16, 16]}>
          {features.map((item, idx) => (
            <Col xs={24} sm={12} md={6} key={idx}>
              <Card style={{ ...cardStyle, background: item.color, border: `1px solid ${item.borderColor}` }}>
                <div style={{ textAlign: 'center' }}>
                  <div style={{ marginBottom: 12 }}>{item.icon}</div>
                  <div style={{ fontWeight: 600, fontSize: isMobile ? 14 : 16, marginBottom: 8 }}>{item.title}</div>
                  <div style={{ fontSize: isMobile ? 12 : 14, color: '#999' }}>{item.desc}</div>
                </div>
              </Card>
            </Col>
          ))}
        </Row>
      </div>

      {/* Quick Links */}
      <Card style={cardStyle}>
        <div style={paddingStyle}>
          <Title level={5} style={{ marginBottom: 16 }}>快速导航</Title>
          <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
            <a href="/agents" style={{ display: 'inline-block', padding: '8px 20px', background: '#1890ff', color: '#fff', borderRadius: 6, textDecoration: 'none', fontSize: 14 }}>
              → 进入 Agent 管理
            </a>
          </div>
        </div>
      </Card>
    </div>
  )
}
