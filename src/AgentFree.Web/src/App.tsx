import React, { useState, useEffect, useCallback } from 'react'
import { BrowserRouter, Routes, Route, useNavigate, useLocation } from 'react-router-dom'
import { Layout, Menu, Typography, theme } from 'antd'
import {
  HomeOutlined,
  ClusterOutlined,
  MessageOutlined,
} from '@ant-design/icons'
import Home from './views/Home'
import Agents from './views/Agents'
import ChatLayout from './views/ChatLayout'

const { Header, Content, Footer } = Layout
const { Title } = Typography

const menuItems = [
  { key: '/', icon: <HomeOutlined />, label: '首页' },
  { key: '/chat', icon: <MessageOutlined />, label: '聊天' },
  { key: '/agents', icon: <ClusterOutlined />, label: '管理' },
]

function useIsMobile() {
  const [isMobile, setIsMobile] = useState(typeof window !== 'undefined' && window.innerWidth <= 768)
  useEffect(() => {
    const handler = () => setIsMobile(window.innerWidth <= 768)
    window.addEventListener('resize', handler)
    return () => window.removeEventListener('resize', handler)
  }, [])
  return isMobile
}

function HeaderContent() {
  const location = useLocation()
  const navigate = useNavigate()
  const isMobile = useIsMobile()

  const handleMenuClick = useCallback(({ key }: { key: string }) => {
    navigate(key)
  }, [navigate])

  return (
    <Header style={{
      background: '#fff',
      padding: '0 24px',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      boxShadow: '0 2px 8px rgba(0,0,0,0.06)',
      position: 'sticky',
      top: 0,
      zIndex: 1000,
    }}>
      <div style={{ display: 'flex', alignItems: 'center', flex: 1, minWidth: 0 }}>
        <Title level={4} style={{
          margin: 0, color: '#001529', fontWeight: 600,
          marginRight: isMobile ? 12 : 32,
          whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
        }}>
          🐠 AgentFree (Goldfish)
        </Title>
        {!isMobile && (
          <Menu
            mode="horizontal"
            selectedKeys={[location.pathname]}
            items={menuItems}
            onClick={handleMenuClick}
            style={{ border: 'none', flex: 1 }}
          />
        )}
      </div>
    </Header>
  )
}

// 移动端底部导航栏
function MobileBottomNav() {
  const location = useLocation()
  const navigate = useNavigate()

  const navItems = [
    { key: '/', icon: <HomeOutlined />, label: '首页' },
    { key: '/chat', icon: <MessageOutlined />, label: '聊天' },
    { key: '/agents', icon: <ClusterOutlined />, label: '管理' },
  ]

  return (
    <Footer
      style={{
        display: 'flex',
        justifyContent: 'space-around',
        background: '#fff',
        borderTop: '1px solid #f0f0f0',
        padding: '0',
        position: 'fixed',
        bottom: 0,
        left: 0,
        right: 0,
        zIndex: 999,
      }}
    >
      {navItems.map(item => (
        <div
          key={item.key}
          onClick={() => navigate(item.key)}
          style={{
            flex: 1,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            padding: '8px 0',
            color: location.pathname === item.key ? '#1890ff' : '#999',
            fontSize: 12,
            cursor: 'pointer',
            background: location.pathname === item.key ? '#e6f7ff' : 'transparent',
            borderRadius: 8,
          }}
        >
          {item.icon}
          <span style={{ marginTop: 2 }}>{item.label}</span>
        </div>
      ))}
    </Footer>
  )
}

export default function App() {
  const isMobile = useIsMobile()

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <HeaderContent />
      <Content style={{
        padding: isMobile ? 16 : 24,
        background: '#f0f2f5',
        minHeight: 280,
        paddingBottom: isMobile ? 70 : 24,
      }}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/chat" element={<ChatLayout />} />
          <Route path="/chat/:sessionId" element={<ChatLayout />} />
          <Route path="/agents" element={<Agents />} />
        </Routes>
      </Content>
      {isMobile && <MobileBottomNav />}
    </Layout>
  )
}
