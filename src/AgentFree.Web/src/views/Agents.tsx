import React, { useState, useEffect } from 'react'
import { Card, Typography, Button, Table, Tag, Space, Modal, Form, Input, Select, Popconfirm, message, Drawer } from 'antd'
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  PoweroffOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
} from '@ant-design/icons'
import type { ColumnsType } from 'antd/es/table'
import { getAgents, createAgent, updateAgent, deleteAgent, startAgent, stopAgent } from '@/api'
import type { Agent } from '@/types'

const { Title, Text } = Typography

export default function Agents() {
  const isMobile = typeof window !== 'undefined' && window.innerWidth <= 768
  const [agents, setAgents] = useState<Agent[]>([])
  const [loading, setLoading] = useState(false)
  const [modalVisible, setModalVisible] = useState(false)
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null)
  const [form] = Form.useForm()
  const [drawerVisible, setDrawerVisible] = useState(false)
  const [detailAgent, setDetailAgent] = useState<Agent | null>(null)

  const protocolOptions = ['MCP', 'A2UI', 'AG-UI', 'Skills', 'A2A']

  const fetchAgents = async () => {
    try {
      setLoading(true)
      const res = await getAgents()
      setAgents(res.data)
    } catch (err: any) {
      // API not ready yet, use mock data
      console.log('API not ready, using mock data')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchAgents() }, [])

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields()
      if (editingAgent) {
        await updateAgent(editingAgent.id, values)
        message.success('更新成功')
      } else {
        await createAgent(values)
        message.success('创建成功')
      }
      setModalVisible(false)
      form.resetFields()
      setEditingAgent(null)
      fetchAgents()
    } catch (err: any) {
      message.error(err.message || '操作失败')
    }
  }

  const handleDelete = async (id: number) => {
    try {
      await deleteAgent(id)
      message.success('删除成功')
      fetchAgents()
    } catch (err: any) {
      message.error(err.message || '删除失败')
    }
  }

  const handleStatus = async (agent: Agent, status: 'Active' | 'Inactive') => {
    try {
      if (status === 'Active') {
        await startAgent(agent.id)
        message.success('已启动')
      } else {
        await stopAgent(agent.id)
        message.success('已停止')
      }
      fetchAgents()
    } catch (err: any) {
      message.error(err.message || '操作失败')
    }
  }

  const columns: ColumnsType<Agent> = [
    {
      title: '名称',
      dataIndex: 'name',
      key: 'name',
      width: isMobile ? 'auto' : 200,
      ellipsis: true,
      render: (text, record) => (
        <a onClick={() => { setDetailAgent(record); setDrawerVisible(true) }}>
          {text}
        </a>
      ),
    },
    {
      title: '协议',
      dataIndex: 'protocolType',
      key: 'protocolType',
      width: isMobile ? 'auto' : 120,
      render: (text) => <Tag color="blue">{text}</Tag>,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: isMobile ? 'auto' : 100,
      render: (status: string) => (
        <Tag color={status === 'Active' ? 'green' : status === 'Error' ? 'red' : 'default'}>
          {status === 'Active' ? <CheckCircleOutlined /> : status === 'Error' ? <CloseCircleOutlined /> : null}
          {status}
        </Tag>
      ),
    },
    {
      title: '操作',
      key: 'actions',
      width: isMobile ? 'auto' : 250,
      render: (_: any, record: Agent) => (
        <Space size="small" wrap>
          <Button type="link" size="small" icon={<EditOutlined />} onClick={() => {
            setEditingAgent(record)
            form.setFieldsValue(record)
            setModalVisible(true)
          }}>编辑</Button>
          {record.status === 'Active' ? (
            <Popconfirm title="停止此 Agent？" onConfirm={() => handleStatus(record, 'Inactive')}>
              <Button type="link" size="small" danger icon={<PoweroffOutlined />}>停止</Button>
            </Popconfirm>
          ) : (
            <Popconfirm title="启动此 Agent？" onConfirm={() => handleStatus(record, 'Active')}>
              <Button type="link" size="small" icon={<PoweroffOutlined />} style={{ color: '#52c41a' }}>启动</Button>
            </Popconfirm>
          )}
          <Popconfirm title="确定删除？" onConfirm={() => handleDelete(record.id)}>
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>删除</Button>
          </Popconfirm>
        </Space>
      ),
    },
  ]

  // Mobile: render table as list
  if (isMobile) {
    return (
      <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 12px' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16, flexWrap: 'wrap', gap: 8 }}>
          <Title level={4} style={{ margin: 0 }}>🤖 Agent 管理</Title>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => { form.resetFields(); setEditingAgent(null); setModalVisible(true) }}>新增</Button>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {loading ? (
            <div style={{ textAlign: 'center', padding: 40, color: '#999' }}>加载中...</div>
          ) : agents.length === 0 ? (
            <div style={{ textAlign: 'center', padding: 40, color: '#999' }}>
              <div style={{ fontSize: 48, marginBottom: 16 }}>📭</div>
              <div>暂无 Agent，点击上方按钮创建</div>
            </div>
          ) : (
            agents.map(agent => (
              <Card
                key={agent.id}
                size="small"
                hoverable
                onClick={() => { setDetailAgent(agent); setDrawerVisible(true) }}
                style={{ borderRadius: 8 }}
              >
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontWeight: 600, marginBottom: 4 }}>
                      {agent.name}
                      <Tag color={agent.status === 'Active' ? 'green' : 'default'} style={{ marginLeft: 8 }}>
                        {agent.status}
                      </Tag>
                    </div>
                    <div style={{ fontSize: 12, color: '#999', marginBottom: 4 }}>{agent.description}</div>
                    <Tag color="blue">{agent.protocolType}</Tag>
                  </div>
                  <div style={{ display: 'flex', gap: 4, flexShrink: 0 }}>
                    <Button type="text" size="small" icon={<EditOutlined />} onClick={(e) => {
                      e.stopPropagation()
                      setEditingAgent(agent)
                      form.setFieldsValue(agent)
                      setModalVisible(true)
                    }} />
                    <Popconfirm title={agent.status === 'Active' ? '停止' : '启动'} onConfirm={(e) => {
                      e?.stopPropagation()
                      handleStatus(agent, agent.status === 'Active' ? 'Inactive' : 'Active')
                    }}>
                      <Button type="text" size="small" icon={<PoweroffOutlined />} style={{ color: agent.status === 'Active' ? '#ff4d4f' : '#52c41a' }} />
                    </Popconfirm>
                    <Popconfirm title="确定删除？" onConfirm={(e) => {
                      e?.stopPropagation()
                      handleDelete(agent.id)
                    }}>
                      <Button type="text" size="small" danger icon={<DeleteOutlined />} />
                    </Popconfirm>
                  </div>
                </div>
              </Card>
            ))
          )}
        </div>

        {/* Edit/Create Modal */}
        <Modal
          title={editingAgent ? '编辑 Agent' : '新增 Agent'}
          open={modalVisible}
          onCancel={() => { setModalVisible(false); form.resetFields(); setEditingAgent(null) }}
          onOk={handleSubmit}
          width={isMobile ? '95vw' : 500}
        >
          <Form form={form} layout="vertical">
            <Form.Item name="name" label="名称" rules={[{ required: true, message: '请输入名称' }]}>
              <Input placeholder="Agent 名称" />
            </Form.Item>
            <Form.Item name="description" label="描述">
              <Input.TextArea rows={3} placeholder="Agent 描述" />
            </Form.Item>
            <Form.Item name="protocolType" label="协议类型" rules={[{ required: true, message: '请选择协议类型' }]}>
              <Select placeholder="选择协议类型" options={protocolOptions.map(p => ({ value: p, label: p }))} />
            </Form.Item>
            <Form.Item name="status" label="状态" initialValue="Inactive">
              <Select options={[{ value: 'Active', label: 'Active' }, { value: 'Inactive', label: 'Inactive' }]} />
            </Form.Item>
          </Form>
        </Modal>

        {/* Detail Drawer */}
        <Drawer
          title="Agent 详情"
          placement="right"
          open={drawerVisible}
          onClose={() => setDrawerVisible(false)}
          width={isMobile ? '100%' : 480}
        >
          {detailAgent && (
            <div>
              <Card size="small" style={{ marginBottom: 16 }}>
                <Title level={5}>{detailAgent.name}</Title>
                <Text type="secondary">{detailAgent.description}</Text>
                <div style={{ marginTop: 12 }}>
                  <Tag color="blue">{detailAgent.protocolType}</Tag>
                  <Tag color={detailAgent.status === 'Active' ? 'green' : 'default'}>{detailAgent.status}</Tag>
                </div>
              </Card>
              <div style={{ fontSize: 14 }}>
                <div style={{ marginBottom: 8, color: '#999' }}>创建时间</div>
                <div style={{ marginBottom: 16 }}>{detailAgent.createdAt || '-'}</div>
                <div style={{ marginBottom: 8, color: '#999' }}>更新时间</div>
                <div>{detailAgent.updatedAt || '-'}</div>
              </div>
            </div>
          )}
        </Drawer>
      </div>
    )
  }

  // Desktop: use Table
  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', padding: '0 24px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <Title level={4} style={{ margin: 0 }}>🤖 Agent 管理</Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => { form.resetFields(); setEditingAgent(null); setModalVisible(true) }}>新增</Button>
      </div>
      <Card style={{ borderRadius: 8 }}>
        <Table
          columns={columns}
          dataSource={agents}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 10 }}
          locale={{ emptyText: '暂无 Agent 数据' }}
        />
      </Card>

      {/* Edit/Create Modal */}
      <Modal
        title={editingAgent ? '编辑 Agent' : '新增 Agent'}
        open={modalVisible}
        onCancel={() => { setModalVisible(false); form.resetFields(); setEditingAgent(null) }}
        onOk={handleSubmit}
        width={500}
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="名称" rules={[{ required: true, message: '请输入名称' }]}>
            <Input placeholder="Agent 名称" />
          </Form.Item>
          <Form.Item name="description" label="描述">
            <Input.TextArea rows={3} placeholder="Agent 描述" />
          </Form.Item>
          <Form.Item name="protocolType" label="协议类型" rules={[{ required: true, message: '请选择协议类型' }]}>
            <Select placeholder="选择协议类型" options={protocolOptions.map(p => ({ value: p, label: p }))} />
          </Form.Item>
          <Form.Item name="status" label="状态" initialValue="Inactive">
            <Select options={[{ value: 'Active', label: 'Active' }, { value: 'Inactive', label: 'Inactive' }]} />
          </Form.Item>
        </Form>
      </Modal>

      {/* Detail Drawer */}
      <Drawer
        title="Agent 详情"
        placement="right"
        open={drawerVisible}
        onClose={() => setDrawerVisible(false)}
        width={480}
      >
        {detailAgent && (
          <div>
            <Card size="small" style={{ marginBottom: 16 }}>
              <Title level={5}>{detailAgent.name}</Title>
              <Text type="secondary">{detailAgent.description}</Text>
              <div style={{ marginTop: 12 }}>
                <Tag color="blue">{detailAgent.protocolType}</Tag>
                <Tag color={detailAgent.status === 'Active' ? 'green' : 'default'}>{detailAgent.status}</Tag>
              </div>
            </Card>
            <div style={{ fontSize: 14 }}>
              <div style={{ marginBottom: 8, color: '#999' }}>创建时间</div>
              <div style={{ marginBottom: 16 }}>{detailAgent.createdAt || '-'}</div>
              <div style={{ marginBottom: 8, color: '#999' }}>更新时间</div>
              <div>{detailAgent.updatedAt || '-'}</div>
            </div>
          </div>
        )}
      </Drawer>
    </div>
  )
}
