import React, { useState, useEffect } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Layout, Menu, Button, Input, Modal, Form, Select, Typography, Drawer, Avatar, Popconfirm, message, theme } from 'antd'
import { PlusOutlined, DeleteOutlined, MenuFoldOutlined, MenuUnfoldOutlined, RobotOutlined, UserOutlined, MessageOutlined } from '@ant-design/icons'
import { getSessions, getAgents, createSession, deleteSession } from '../api'
import type { Session, Agent } from '../types'
import ChatView from './ChatView'

const { Sider, Content } = Layout
const { Title } = Typography

export default function ChatLayout() {
  const navigate = useNavigate()
  const location = useLocation()
  const { token } = theme.useToken()

  const [sessions, setSessions] = useState<Session[]>([])
  const [agents, setAgents] = useState<Agent[]>([])
  const [loading, setLoading] = useState(false)
  const [createModal, setCreateModal] = useState(false)
  const [form] = Form.useForm()
  const [collapsed, setCollapsed] = useState(false)
  const [isMobile, setIsMobile] = useState(window.innerWidth <= 768)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const currentSessionId = location.pathname.split('/chat/')[1]

  useEffect(() => {
    const handler = () => setIsMobile(window.innerWidth <= 768)
    window.addEventListener('resize', handler)
    return () => window.removeEventListener('resize', handler)
  }, [])

  useEffect(() => {
    if (isMobile) setCollapsed(true)
  }, [isMobile])

  const fetchData = async () => {
    setLoading(true)
    try {
      const [sessionsRes, agentsRes] = await Promise.all([getSessions(), getAgents()])
      setSessions(sessionsRes.data || [])
      setAgents(agentsRes.data || [])
    } catch (err: any) {
      message.error('加载失败: ' + (err.response?.data?.message || err.message))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchData() }, [])

  const handleCreate = async () => {
    try {
      const values = await form.validateFields()
      const res = await createSession({ agentId: values.agentId, name: values.name })
      const newSession = res.data
      message.success('创建成功')
      setCreateModal(false)
      form.resetFields()
      if (newSession && newSession.id) {
        navigate(`/chat/${newSession.id}`)
      } else {
        fetchData()
      }
    } catch (err: any) {
      message.error('创建失败: ' + (err.response?.data?.message || err.message))
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteSession(id)
      message.success('删除成功')
      fetchData()
      if (currentSessionId === id) navigate('/chat')
    } catch (err: any) {
      message.error('删除失败: ' + (err.response?.data?.message || err.message))
    }
  }

  const handleNewChat = () => {
    setCreateModal(true)
    setCollapsed(false)
  }

  const menuItems = sessions.map(s => ({
    key: s.id,
    icon: <MessageOutlined style={{ fontSize: 14 }} />,
    label: (
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', minWidth: 0 }}>
        <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1, fontSize: 13 }}>{s.name}</span>
        <Popconfirm title="删除此会话？" onConfirm={() => handleDelete(s.id)} okText="确定" cancelText="取消">
          <DeleteOutlined style={{ color: '#bbb', fontSize: 13, flexShrink: 0, marginLeft: 4 }} />
        </Popconfirm>
      </div>
    ),
  }))

  const siderContent = (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <div style={{ padding: '16px 16px 8px' }}>
        <Button type="primary" icon={<PlusOutlined />} block onClick={handleNewChat} style={{ borderRadius: 10, height: 40 }}>
          新建会话
        </Button>
      </div>
      <div style={{ flex: 1, overflowY: 'auto', padding: '0 8px 8px' }}>
        {sessions.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '32px 16px', color: '#bbb', fontSize: 13 }}>
            <div style={{ fontSize: 32, marginBottom: 8 }}>📭</div>
            暂无会话
          </div>
        ) : (
          <Menu
            mode="inline"
            selectedKeys={currentSessionId ? [currentSessionId] : []}
            items={menuItems}
            onClick={({ key }) => { navigate(`/chat/${key}`); setCollapsed(false) }}
            style={{ borderRight: 0 }}
          />
        )}
      </div>
    </div>
  )

  // Mobile layout with fixed chat area
  if (isMobile) {
    return (
      <Layout style={{ height: '100dvh', overflow: 'hidden' }}>
        <Drawer
          title="会话列表"
          placement="left"
          width={280}
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          styles={{ body: { padding: 0 } }}
        >
          {siderContent}
        </Drawer>
        <Layout style={{ height: '100dvh', overflow: 'hidden' }}>
          <Content style={{ padding: 0, height: '100%', overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
            {/* Mobile header bar */}
            <div style={{ background: '#fff', padding: '10px 12px', display: 'flex', alignItems: 'center', borderBottom: '1px solid #f0f0f0', flexShrink: 0 }}>
              <Button type="text" icon={<MenuFoldOutlined />} onClick={() => setDrawerOpen(true)} style={{ marginRight: 8 }} />
              <Title level={5} style={{ margin: 0, flex: 1, fontSize: 15 }}>
                {currentSessionId ? sessions.find(s => s.id === currentSessionId)?.name || '聊天' : 'AgentFree'}
              </Title>
              <Button type="text" icon={<PlusOutlined />} onClick={handleNewChat} style={{ marginLeft: 8 }} />
            </div>
            {/* Chat area fills remaining space */}
            <div style={{ flex: 1, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
              {currentSessionId ? <ChatView sessionId={currentSessionId} /> : (
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', flexDirection: 'column', color: '#bbb' }}>
                  <div style={{ fontSize: 48, marginBottom: 12 }}>🐠</div>
                  <div style={{ fontSize: 16, marginBottom: 4 }}>欢迎使用 AgentFree</div>
                  <div style={{ fontSize: 13 }}>打开左侧菜单选择或创建会话</div>
                </div>
              )}
            </div>
          </Content>
        </Layout>
        <Modal title="新建会话" open={createModal} onCancel={() => setCreateModal(false)} onOk={handleCreate} width={340}>
          <Form form={form} layout="vertical">
            <Form.Item name="agentId" label="智能体" rules={[{ required: true, message: '请选择智能体' }]}>
              <Select placeholder="选择智能体">
                {agents.map(a => <Select.Option key={a.id} value={a.id}>{a.name}</Select.Option>)}
              </Select>
            </Form.Item>
            <Form.Item name="name" label="会话名称" rules={[{ required: true, message: '请输入会话名称' }]}>
              <Input placeholder="输入会话名称" />
            </Form.Item>
          </Form>
        </Modal>
      </Layout>
    )
  }

  // Desktop layout with fixed chat area
  return (
    <Layout style={{ height: '100dvh', overflow: 'hidden' }}>
      <Sider
        collapsible
        collapsed={collapsed}
        onCollapse={setCollapsed}
        width={280}
        theme="light"
        style={{
          borderRight: `1px solid ${token.colorBorderSecondary}`,
          background: '#fff',
          overflow: 'hidden',
        }}
        breakpoint="lg"
        collapsedWidth={80}
      >
        {siderContent}
      </Sider>
      <Content style={{ padding: 0, background: '#f5f6fa', height: '100%', overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        <div style={{ flex: 1, overflow: 'hidden', display: 'flex', justifyContent: 'center' }}>
          <div style={{ width: '100%', maxWidth: 860, height: '100%', display: 'flex', flexDirection: 'column' }}>
            {currentSessionId ? (
              <ChatView sessionId={currentSessionId} />
            ) : (
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', flexDirection: 'column', color: '#bbb' }}>
                <div style={{ fontSize: 64, marginBottom: 16 }}>🐠</div>
                <div style={{ fontSize: 20, marginBottom: 8, fontWeight: 500 }}>欢迎使用 AgentFree</div>
                <div style={{ fontSize: 14 }}>从左侧选择或创建一个会话开始聊天</div>
              </div>
            )}
          </div>
        </div>
      </Content>
      <Modal title="新建会话" open={createModal} onCancel={() => setCreateModal(false)} onOk={handleCreate} width={420}>
        <Form form={form} layout="vertical">
          <Form.Item name="agentId" label="智能体" rules={[{ required: true, message: '请选择智能体' }]}>
            <Select placeholder="选择智能体">
              {agents.map(a => <Select.Option key={a.id} value={a.id}>{a.name}</Select.Option>)}
            </Select>
          </Form.Item>
          <Form.Item name="name" label="会话名称" rules={[{ required: true, message: '请输入会话名称' }]}>
            <Input placeholder="输入会话名称" />
          </Form.Item>
        </Form>
      </Modal>
    </Layout>
  )
}
