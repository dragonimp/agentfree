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
      // 直接跳转到新创建的会话，进入聊天界面
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
    // 直接打开新建会话对话框
    setCreateModal(true)
    setCollapsed(false)
  }

  const menuItems = sessions.map(s => ({
    key: s.id,
    icon: <MessageOutlined />,
    label: (
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{s.name}</span>
        <Popconfirm title="删除此会话？" onConfirm={() => handleDelete(s.id)} okText="确定" cancelText="取消">
          <DeleteOutlined style={{ color: '#999', fontSize: 12, flexShrink: 0, marginLeft: 4 }} />
        </Popconfirm>
      </div>
    ),
  }))

  const siderContent = (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <div style={{ padding: '12px 16px' }}>
        <Button type="primary" icon={<PlusOutlined />} block onClick={handleNewChat} style={{ borderRadius: 8 }}>
          新建会话
        </Button>
      </div>
      <div style={{ flex: 1, overflowY: 'auto', padding: '0 8px' }}>
        {sessions.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '32px 16px', color: '#999', fontSize: 13 }}>
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

  if (isMobile) {
    return (
      <Layout style={{ minHeight: '100vh' }}>
        <Layout>
          <Drawer
            title="聊天会话"
            placement="left"
            width={280}
            open={drawerOpen}
            onClose={() => setDrawerOpen(false)}
            styles={{ body: { padding: 0 } }}
          >
            {siderContent}
          </Drawer>
          <Content style={{ padding: 0 }}>
            <div style={{ background: '#fff', padding: '12px 16px', display: 'flex', alignItems: 'center', boxShadow: '0 1px 4px rgba(0,0,0,0.06)' }}>
              <Button type="text" icon={<MenuFoldOutlined />} onClick={() => setDrawerOpen(true)} style={{ marginRight: 8 }} />
              <Title level={5} style={{ margin: 0, flex: 1 }}>
                {currentSessionId ? sessions.find(s => s.id === currentSessionId)?.name || '聊天' : '新建会话'}
              </Title>
              <Button type="text" icon={<PlusOutlined />} onClick={handleNewChat} />
            </div>
            <ChatLayoutContent />
          </Content>
        </Layout>
        <Modal title="新建会话" open={createModal} onCancel={() => setCreateModal(false)} onOk={handleCreate} width={360}>
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

  return (
    <Layout style={{ minHeight: 'calc(100vh - 64px)' }}>
      <Sider
        collapsible
        collapsed={collapsed}
        onCollapse={setCollapsed}
        width={280}
        theme="light"
        style={{
          borderRight: `1px solid ${token.colorBorderSecondary}`,
          background: '#fff',
        }}
      >
        {siderContent}
      </Sider>
      <Content style={{ padding: 0, background: '#f5f6fa' }}>
        <ChatLayoutContent />
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

function ChatLayoutContent() {
  const location = useLocation()
  const sessionId = location.pathname.split('/chat/')[1]

  return (
    <div style={{ height: 'calc(100vh - 64px)', display: 'flex', justifyContent: 'center', overflow: 'hidden' }}>
      <div style={{ width: '100%', maxWidth: 860 }}>
        {sessionId ? (
          <ChatView sessionId={sessionId} />
        ) : (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', flexDirection: 'column', color: '#999' }}>
            <div style={{ fontSize: 64, marginBottom: 16 }}>🐠</div>
            <div style={{ fontSize: 18, marginBottom: 8 }}>欢迎使用 AgentFree</div>
            <div style={{ fontSize: 14 }}>选择或创建一个会话开始聊天</div>
          </div>
        )}
      </div>
    </div>
  )
}
