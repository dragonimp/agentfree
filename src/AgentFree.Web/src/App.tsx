import React, { useState, useEffect, useCallback } from 'react'
import { BrowserRouter, Routes, Route, useNavigate, useLocation } from 'react-router-dom'
import { Layout, Menu, Typography, Button, theme } from 'antd'
import {
  HomeOutlined,
  ClusterOutlined,
  MessageOutlined,
} from '@ant-design/icons'
import Home from './views/Home'
import Agents from './views/Agents'
import ChatLayout from './views/ChatLayout'

const { Header, Content } = Layout
const { Title } = Typography

const menuItems = [
  { key: '/', icon: <HomeOutlined />, label: '首页' },
  { key: '/chat', icon: <MessageOutlined />, label: '聊天' },
  { key: '/agents', icon: <ClusterOutlined />, label: 'Agent 管理' },
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
    </div>
  )
}

export default function App() {
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <HeaderContent />
      <Content style={{ padding: 24, background: '#f0f2f5', minHeight: 280 }}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/chat" element={<ChatLayout />} />
          <Route path="/chat/:sessionId" element={<ChatLayout />} />
          <Route path="/agents" element={<Agents />} />
        </Routes>
      </Content>
    </Layout>
  )
}
