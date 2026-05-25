import React, { useState, useEffect } from 'react'
import { Card, Table, Button, Input, Modal, Form, Select, Typography, Popconfirm, message } from 'antd'
import { PlusOutlined, DeleteOutlined, MessageOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { getSessions, getAgents, createSession, deleteSession } from '../api'
import type { Session, Agent } from '../types'

const { Title } = Typography

export default function Sessions() {
  const navigate = useNavigate()
  const [sessions, setSessions] = useState<Session[]>([])
  const [agents, setAgents] = useState<Agent[]>([])
  const [loading, setLoading] = useState(false)
  const [createModal, setCreateModal] = useState(false)
  const [form] = Form.useForm()

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

  useEffect(() => {
    fetchData()
  }, [])

  const handleCreate = async () => {
    try {
      const values = await form.validateFields()
      await createSession({ agentId: values.agentId, name: values.name })
      message.success('创建成功')
      setCreateModal(false)
      form.resetFields()
      fetchData()
    } catch (err: any) {
      message.error('创建失败: ' + (err.response?.data?.message || err.message))
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteSession(id)
      message.success('删除成功')
      fetchData()
    } catch (err: any) {
      message.error('删除失败: ' + (err.response?.data?.message || err.message))
    }
  }

  const columns = [
    {
      title: '会话名称',
      dataIndex: 'name',
      key: 'name',
      render: (name: string, record: Session) => (
        <a onClick={() => navigate(`/chat/${record.id}`)}>
          <MessageOutlined /> {name}
        </a>
      ),
    },
    {
      title: '智能体',
      dataIndex: 'agentId',
      key: 'agentId',
      render: (agentId: number) => {
        const agent = agents.find(a => a.id === agentId)
        return agent ? agent.name : `#${agentId}`
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'actions',
      render: (_: any, record: Session) => (
        <Popconfirm title="确定删除此会话？" onConfirm={() => handleDelete(record.id)}>
          <Button type="link" danger icon={<DeleteOutlined />} />
        </Popconfirm>
      ),
    },
  ]

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 16 }}>
        <Title level={4}>会话管理</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateModal(true)}>
          新建会话
        </Button>
      </div>
      <Card>
        <Table
          columns={columns}
          dataSource={sessions}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 10 }}
        />
      </Card>

      <Modal
        title="新建会话"
        open={createModal}
        onCancel={() => setCreateModal(false)}
        onOk={handleCreate}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="agentId" label="智能体" rules={[{ required: true, message: '请选择智能体' }]}>
            <Select placeholder="选择智能体">
              {agents.map(a => (
                <Select.Option key={a.id} value={a.id}>{a.name}</Select.Option>
              ))}
            </Select>
          </Form.Item>
          <Form.Item name="name" label="会话名称" rules={[{ required: true, message: '请输入会话名称' }]}>
            <Input placeholder="输入会话名称" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
