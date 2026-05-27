import React, { useState, useEffect } from 'react'
import { Card, Typography, Button, Tag, Space, Modal, Form, Input, Select, Popconfirm, message, Drawer, Row, Col, Avatar, Tooltip, theme } from 'antd'
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  PoweroffOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  RobotOutlined,
  ThunderboltOutlined,
  ClockCircleOutlined,
  GlobalOutlined,
  KeyOutlined,
} from '@ant-design/icons'
import { getAgents, createAgent, updateAgent, deleteAgent, startAgent, stopAgent } from '@/api'
import type { Agent } from '@/types'

const { Title, Text, Paragraph } = Typography

const agentTypeStyles: Record<string, { bg: string; color: string; tag: string; icon: string }> = {
  Goldfish: { bg: '#f6ffed', color: '#52c41a', tag: 'green', icon: '🐠' },
  Openclaw: { bg: '#e6f7ff', color: '#1890ff', tag: 'blue', icon: '🔗' },
  Hermes:   { bg: '#f9f0ff', color: '#722ed1', tag: 'purple', icon: '🦄' },
}

export default function Agents() {
  const [agents, setAgents] = useState<Agent[]>([])
  const [loading, setLoading] = useState(false)
  const [modalVisible, setModalVisible] = useState(false)
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null)
  const [form] = Form.useForm()
  const [drawerVisible, setDrawerVisible] = useState(false)
  const [detailAgent, setDetailAgent] = useState<Agent | null>(null)

  const agentTypeOptions = [
    { value: 'Goldfish', label: 'Goldfish (自研)' },
    { value: 'Openclaw', label: 'Openclaw' },
    { value: 'Hermes', label: 'Hermes' },
  ]

  const fetchAgents = async () => {
    try {
      setLoading(true)
      const res = await getAgents()
      setAgents(res.data)
    } catch (err: any) {
      console.log('API not ready, using mock data')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { fetchAgents() }, [])

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields()
      // Hermes 类型: 组合 serviceUrl + port, token = hermesKey
      if (values.agentType === 'Hermes') {
        const baseUrl = (values.hermesBaseUrl || '').replace(/\/$/, '')
        const port = values.hermesPort
        values.serviceUrl = port ? `${baseUrl}:${port}` : baseUrl
        values.token = values.hermesKey
      }
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

  // 编辑时预填 Hermes 配置
  const handleEdit = (agent: Agent) => {
    setEditingAgent(agent)
    const hermesValues: Record<string, any> = {
      hermesBaseUrl: agent.serviceUrl?.split(':')[0]?.replace('http://', '').replace('https://', '') || '100.100.59.18',
      hermesPort: agent.serviceUrl?.split(':')[1] || '18788',
      hermesKey: agent.token || '',
    }
    form.setFieldsValue({ ...agent, ...hermesValues })
    setModalVisible(true)
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

  const getStatusBadge = (status: string) => {
    if (status === 'Active') return <Tag icon={<CheckCircleOutlined />} color="success" style={{ borderRadius: 20 }}>运行中</Tag>
    if (status === 'Error') return <Tag icon={<CloseCircleOutlined />} color="error" style={{ borderRadius: 20 }}>异常</Tag>
    return <Tag style={{ borderRadius: 20, background: '#fafafa', color: '#999', border: '1px solid #f0f0f0' }}>已停止</Tag>
  }

  return (
    <div style={{ maxWidth: 1400, margin: '0 auto', padding: '0 20px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24, flexWrap: 'wrap', gap: 12 }}>
        <div>
          <Title level={3} style={{ margin: 0 }}>🤖 智能体管理</Title>
          <Text type="secondary" style={{ fontSize: 14 }}>管理和配置所有智能体服务</Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => { form.resetFields(); setEditingAgent(null); setModalVisible(true) }}
          style={{ borderRadius: 8, height: 40, fontSize: 15 }}
        >
          新增智能体
        </Button>
      </div>

      {/* Stats bar */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 24 }}>
        <Card size="small" style={{ flex: 1, borderRadius: 12, border: '1px solid #f0f0f0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <div style={{ width: 40, height: 40, borderRadius: 10, background: '#e6f7ff', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 20 }}>📊</div>
            <div>
              <div style={{ fontSize: 24, fontWeight: 700 }}>{agents.length}</div>
              <div style={{ fontSize: 13, color: '#999' }}>全部智能体</div>
            </div>
          </div>
        </Card>
        <Card size="small" style={{ flex: 1, borderRadius: 12, border: '1px solid #f0f0f0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <div style={{ width: 40, height: 40, borderRadius: 10, background: '#f6ffed', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 20 }}>✅</div>
            <div>
              <div style={{ fontSize: 24, fontWeight: 700, color: '#52c41a' }}>{agents.filter(a => a.status === 'Active').length}</div>
              <div style={{ fontSize: 13, color: '#999' }}>运行中</div>
            </div>
          </div>
        </Card>
        <Card size="small" style={{ flex: 1, borderRadius: 12, border: '1px solid #f0f0f0' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <div style={{ width: 40, height: 40, borderRadius: 10, background: '#fffbe6', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 20 }}>⏸️</div>
            <div>
              <div style={{ fontSize: 24, fontWeight: 700, color: '#faad14' }}>{agents.filter(a => a.status !== 'Active').length}</div>
              <div style={{ fontSize: 13, color: '#999' }}>已停止</div>
            </div>
          </div>
        </Card>
      </div>

      {/* Card Grid */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: 60, color: '#999', fontSize: 15 }}>加载中...</div>
      ) : agents.length === 0 ? (
        <Card style={{ borderRadius: 12, border: '2px dashed #d9d9d9' }}>
          <div style={{ textAlign: 'center', padding: '40px 0' }}>
            <div style={{ fontSize: 56, marginBottom: 16 }}>📭</div>
            <Title level={5} style={{ color: '#999' }}>暂无智能体</Title>
            <Text type="secondary">点击上方"新增智能体"按钮开始配置</Text>
          </div>
        </Card>
      ) : (
        <Row gutter={[20, 20]}>
          {agents.map(agent => {
            const style = agentTypeStyles[agent.agentType] || { bg: '#f5f5f5', color: '#999', tag: 'default', icon: '🤖' }
            return (
              <Col xs={24} sm={12} md={8} lg={8} xl={6} key={agent.id}>
                <Card
                  hoverable
                  style={{
                    borderRadius: 16,
                    border: `1px solid ${agent.status === 'Active' ? '#b7eb8f' : '#f0f0f0'}`,
                    boxShadow: '0 2px 8px rgba(0,0,0,0.04)',
                    transition: 'all 0.2s',
                  }}
                  bodyStyle={{ padding: 20 }}
                  onClick={() => { setDetailAgent(agent); setDrawerVisible(true) }}
                >
                  {/* Card Header */}
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 16 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 12, flex: 1, minWidth: 0 }}>
                      <div
                        style={{
                          width: 48,
                          height: 48,
                          borderRadius: 12,
                          background: style.bg,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          fontSize: 24,
                          flexShrink: 0,
                        }}
                      >
                        {style.icon}
                      </div>
                      <div style={{ minWidth: 0 }}>
                        <div style={{ fontWeight: 600, fontSize: 16, marginBottom: 4, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {agent.name}
                        </div>
                        {getStatusBadge(agent.status)}
                      </div>
                    </div>
                  </div>

                  {/* Description */}
                  {agent.description && (
                    <Text type="secondary" style={{ fontSize: 13, display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden', lineHeight: 1.5, marginBottom: 12 }}>
                      {agent.description}
                    </Text>
                  )}

                  {/* Tags Row */}
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginBottom: 16 }}>
                    <Tag color={style.tag} style={{ borderRadius: 6, margin: 0 }}>{agent.agentType}</Tag>
                    {agent.serviceUrl && (
                      <Tooltip title={agent.serviceUrl}>
                        <Tag icon={<GlobalOutlined />} style={{ borderRadius: 6, margin: 0, fontSize: 11, padding: '2px 6px' }}>{agent.serviceUrl}</Tag>
                      </Tooltip>
                    )}
                    {agent.agentId && (
                      <Tooltip title={agent.agentId}>
                        <Tag icon={<KeyOutlined />} style={{ borderRadius: 6, margin: 0, fontSize: 11, padding: '2px 6px' }}>#{agent.agentId}</Tag>
                      </Tooltip>
                    )}
                  </div>

                  {/* Footer Actions */}
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderTop: '1px solid #f5f5f5', paddingTop: 12 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 12, color: '#bbb' }}>
                      <ClockCircleOutlined />
                      {new Date(agent.updatedAt).toLocaleDateString('zh-CN')}
                    </div>
                    <Space size={4}>
                      <Button type="text" size="small" icon={<EditOutlined />} onClick={(e) => {
                        e.stopPropagation()
                        handleEdit(agent)
                      }} style={{ borderRadius: 8 }} />
                      {agent.status === 'Active' ? (
                        <Popconfirm title="停止此智能体？" onConfirm={(e) => { e?.stopPropagation(); handleStatus(agent, 'Inactive') }}>
                          <Button type="text" size="small" danger icon={<PoweroffOutlined />} onClick={(e) => e.stopPropagation()} style={{ borderRadius: 8 }} />
                        </Popconfirm>
                      ) : (
                        <Popconfirm title="启动此智能体？" onConfirm={(e) => { e?.stopPropagation(); handleStatus(agent, 'Active') }}>
                          <Button type="text" size="small" icon={<PoweroffOutlined />} style={{ color: '#52c41a', borderRadius: 8 }} onClick={(e) => e.stopPropagation()} />
                        </Popconfirm>
                      )}
                      <Popconfirm title="确定删除？" onConfirm={(e) => { e?.stopPropagation(); handleDelete(agent.id) }}>
                        <Button type="text" size="small" danger icon={<DeleteOutlined />} onClick={(e) => e.stopPropagation()} style={{ borderRadius: 8 }} />
                      </Popconfirm>
                    </Space>
                  </div>
                </Card>
              </Col>
            )
          })}
        </Row>
      )}

      {/* Edit/Create Modal — 紧凑布局 */}
      <Modal
        title={editingAgent ? '编辑智能体' : '新增智能体'}
        open={modalVisible}
        onCancel={() => { setModalVisible(false); form.resetFields(); setEditingAgent(null) }}
        onOk={handleSubmit}
        okText={editingAgent ? '保存' : '创建'}
        cancelText="取消"
        destroyOnClose
        styles={{ body: { padding: '12px 24px' } }}
      >
        <Form form={form} layout="vertical" style={{ fontSize: 13 }}>
          <Form.Item name="name" label="智能体名称" rules={[{ required: true, message: '请输入名称' }]}>
            <Input placeholder="例如：客服助手" />
          </Form.Item>
          <Form.Item name="description" label="描述">
            <Input.TextArea rows={2} placeholder="描述这个智能体的功能" />
          </Form.Item>
          {/* 智能体类型 */}
          <Form.Item name="agentType" label="智能体类型" rules={[{ required: true, message: '请选择类型' }]} initialValue="Goldfish">
            <Select options={agentTypeOptions} />
          </Form.Item>
          
          {/* 服务地址 - 独立一行 */}
          <Form.Item name="serviceUrl" label="服务地址">
            <Input placeholder="http://127.0.0.1:5101" />
          </Form.Item>
          
          {/* 智能体ID */}
          <Form.Item name="agentId" label="智能体ID">
            <Input placeholder="唯一标识" />
          </Form.Item>
          
          {/* TOKEN */}
          <Form.Item name="token" label="TOKEN">
            <Input.Password placeholder="认证 Token" />
          </Form.Item>
          
          {/* API Server 地址 */}
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="apiServerIp" label="API Server IP">
                <Input placeholder="192.168.1.100" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="apiServerPort" label="API Server 端口">
                <Input placeholder="5100" />
              </Form.Item>
            </Col>
          </Row>
          
          {/* 状态 */}
          <Form.Item name="status" label="状态" initialValue="Inactive">
            <Select options={[{ value: 'Active', label: 'Active (运行中)' }, { value: 'Inactive', label: 'Inactive (已停止)' }]} />
          </Form.Item>
          
          {/* Hermes 专用配置 */}
          {form.getFieldValue('agentType') === 'Hermes' && (
            <div style={{ background: '#f9f0ff', borderRadius: 8, padding: 12, marginBottom: 8 }}>
              <Text style={{ fontWeight: 600, fontSize: 13, color: '#722ed1' }}>🔗 Hermes 网关配置</Text>
              <Form.Item name="hermesBaseUrl" label="Tailscale 地址" style={{ marginBottom: 4, marginTop: 8 }}>
                <Input prefix="http://" placeholder="100.100.59.18" />
              </Form.Item>
              <Row gutter={12}>
                <Col span={12}>
                  <Form.Item name="hermesPort" label="端口" style={{ marginBottom: 4 }}>
                    <Input placeholder="18788" />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item name="hermesKey" label="API Key" style={{ marginBottom: 4 }}>
                    <Input.Password placeholder="hermes-local-key" />
                  </Form.Item>
                </Col>
              </Row>
            </div>
          )}
        </Form>
      </Modal>

      {/* Detail Drawer */}
      <Drawer
        title={detailAgent?.name}
        placement="right"
        open={drawerVisible}
        onClose={() => setDrawerVisible(false)}
        width={480}
      >
        {detailAgent && (
          <div>
            <Card size="small" style={{ marginBottom: 16, borderRadius: 12, border: '1px solid #f0f0f0' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 16, marginBottom: 16 }}>
                <Avatar size={64} style={{ background: agentTypeStyles[detailAgent.agentType]?.bg || '#f5f5f5', color: agentTypeStyles[detailAgent.agentType]?.color, fontSize: 32 }}>
                  {agentTypeStyles[detailAgent.agentType]?.icon || '🤖'}
                </Avatar>
                <div>
                  <Title level={4} style={{ margin: 0 }}>{detailAgent.name}</Title>
                  {getStatusBadge(detailAgent.status)}
                </div>
              </div>
              <Paragraph style={{ color: '#666', margin: '0 0 16px 0' }}>{detailAgent.description || '暂无描述'}</Paragraph>
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                <Tag color={agentTypeStyles[detailAgent.agentType]?.tag}>{detailAgent.agentType}</Tag>
                {detailAgent.serviceUrl && <Tag icon={<GlobalOutlined />}>{detailAgent.serviceUrl}</Tag>}
                {detailAgent.agentId && <Tag icon={<KeyOutlined />}>#{detailAgent.agentId}</Tag>}
              </div>
            </Card>

            <div style={{ background: '#fafafa', borderRadius: 12, padding: 16 }}>
              <div style={{ fontSize: 14, fontWeight: 600, marginBottom: 12 }}>时间信息</div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, fontSize: 13 }}>
                <Text type="secondary">创建时间</Text>
                <Text>{detailAgent.createdAt ? new Date(detailAgent.createdAt).toLocaleString('zh-CN') : '-'}</Text>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13 }}>
                <Text type="secondary">更新时间</Text>
                <Text>{detailAgent.updatedAt ? new Date(detailAgent.updatedAt).toLocaleString('zh-CN') : '-'}</Text>
              </div>
            </div>

            <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
              <Button type="primary" icon={<EditOutlined />} block onClick={() => { setDrawerVisible(false); setModalVisible(true); handleEdit(detailAgent) }} style={{ borderRadius: 8 }}>
                编辑智能体
              </Button>
              <Popconfirm title="确定删除此智能体？" onConfirm={() => handleDelete(detailAgent.id)}>
                <Button danger icon={<DeleteOutlined />} block style={{ borderRadius: 8 }}>删除</Button>
              </Popconfirm>
            </div>
          </div>
        )}
      </Drawer>
    </div>
  )
}
