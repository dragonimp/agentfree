import React, { useState, useEffect, useRef } from 'react'
import { Card, Input, Button, Typography, Avatar, Spin, message } from 'antd'
import { SendOutlined } from '@ant-design/icons'
import { useNavigate, useParams } from 'react-router-dom'
import ReactMarkdown from 'react-markdown'
import { getMessages, getSession, streamChat } from '../api'
import type { ChatMessage, Session as SessionType } from '../types'

const { TextArea } = Input
const { Text } = Typography

export default function Chat() {
  const { sessionId } = useParams<{ sessionId: string }>()
  const navigate = useNavigate()
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [session, setSession] = useState<SessionType | null>(null)
  const [streamingContent, setStreamingContent] = useState('')
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const abortRef = useRef<() => void>(() => {})

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  useEffect(() => {
    scrollToBottom()
  }, [messages, streamingContent])

  useEffect(() => {
    if (!sessionId) return
    loadMessages()
  }, [sessionId])

  const loadMessages = async () => {
    if (!sessionId) return
    setLoading(true)
    try {
      const [messagesRes, sessionRes] = await Promise.all([
        getMessages(sessionId),
        getSession(sessionId),
      ])
      setMessages(messagesRes.data || [])
      setSession(sessionRes.data || null)
    } catch (err: any) {
      message.error('加载消息失败: ' + (err.response?.data?.message || err.message))
    } finally {
      setLoading(false)
    }
  }

  const parseSSE = async (reader: ReadableStreamDefaultReader<Uint8Array>) => {
    const decoder = new TextDecoder()
    let buffer = ''

    while (true) {
      const { done, value } = await reader.read()
      if (done) break

      buffer += decoder.decode(value, { stream: true })
      const lines = buffer.split('\n')
      buffer = lines.pop() || ''

      for (const line of lines) {
        if (!line.startsWith('data: ')) continue
        const jsonStr = line.slice(6)
        try {
          const data = JSON.parse(jsonStr)
          if (data.delta !== undefined) {
            setStreamingContent(prev => prev + data.delta)
          }
          if (data.done) {
            reader.releaseLock()
            return true
          }
        } catch (e) {
          // skip parse errors
        }
      }
    }
    return true
  }

  const handleSend = async () => {
    if (!input.trim()) return
    if (!sessionId) return

    const userMessage: ChatMessage = {
      id: Date.now(),
      sessionId,
      role: 'user',
      content: input.trim(),
      createdAt: new Date().toISOString(),
    }

    setMessages(prev => [...prev, userMessage])
    setInput('')
    setStreamingContent('')

    try {
      const { reader, abort } = await streamChat({ sessionId, content: userMessage.content })
      abortRef.current = abort

      await parseSSE(reader)

      // Save assistant message to database
      if (streamingContent.trim()) {
        const assistantMessage: ChatMessage = {
          id: Date.now() + 1,
          sessionId,
          role: 'assistant',
          content: streamingContent.trim(),
          createdAt: new Date().toISOString(),
        }
        setMessages(prev => [...prev, assistantMessage])
        setStreamingContent('')
      }
    } catch (err: any) {
      message.error('发送失败: ' + (err.response?.data?.message || err.message))
      setStreamingContent('')
    }
  }

  const handleCancel = () => {
    abortRef.current()
    setStreamingContent('')
    message.info('已取消')
  }

  const renderMessage = (msg: ChatMessage) => {
    const isUser = msg.role === 'user'
    return (
      <div
        key={msg.id}
        style={{
          display: 'flex',
          marginBottom: 16,
          justifyContent: isUser ? 'flex-end' : 'flex-start',
        }}
      >
        {!isUser && (
          <Avatar style={{ backgroundColor: '#667eea', marginRight: 8, flexShrink: 0 }}>
            🐠
          </Avatar>
        )}
        <Card
          style={{
            maxWidth: '70%',
            background: isUser ? '#f6ffed' : '#fff',
            borderColor: isUser ? '#b7eb8f' : '#f0f0f0',
          }}
        >
          <ReactMarkdown
            components={{
              p: ({ children }) => <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{children}</p>,
              code: ({ children }) => (
                <code style={{ background: '#f5f5f5', padding: '2px 4px', borderRadius: 4 }}>
                  {children}
                </code>
              ),
              pre: ({ children }) => (
                <pre style={{ background: '#f5f5f5', padding: 12, borderRadius: 4, overflow: 'auto' }}>
                  {children}
                </pre>
              ),
            }}
          >
            {msg.content}
          </ReactMarkdown>
          <Text type="secondary" style={{ fontSize: 12, display: 'block', marginTop: 4 }}>
            {new Date(msg.createdAt).toLocaleTimeString('zh-CN')}
          </Text>
        </Card>
        {isUser && (
          <Avatar style={{ backgroundColor: '#52c41a', marginLeft: 8, flexShrink: 0 }}>
            {msg.role[0].toUpperCase()}
          </Avatar>
        )}
      </div>
    )
  }

  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Button type="link" onClick={() => navigate('/sessions')}>← 返回会话列表</Button>
        {session && <Text type="secondary">{session.name} — {session.agentId ? `智能体#${session.agentId}` : ''}</Text>}
      </div>

      <Card style={{ minHeight: 500, maxHeight: 'calc(100vh - 200px)', display: 'flex', flexDirection: 'column' }}>
        <div style={{ flex: 1, overflowY: 'auto', padding: '8px 0' }}>
          {messages.map(renderMessage)}
          {streamingContent && (
            <div style={{ display: 'flex', marginBottom: 16, justifyContent: 'flex-start' }}>
              <Avatar style={{ backgroundColor: '#667eea', marginRight: 8, flexShrink: 0 }}>🐠</Avatar>
              <Card style={{ maxWidth: '70%', background: '#fff', borderColor: '#f0f0f0' }}>
                <ReactMarkdown
                  components={{
                    p: ({ children }) => <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{children}</p>,
                    code: ({ children }) => (
                      <code style={{ background: '#f5f5f5', padding: '2px 4px', borderRadius: 4 }}>
                        {children}
                      </code>
                    ),
                  }}
                >
                  {streamingContent}
                </ReactMarkdown>
                <Spin size="small" style={{ marginTop: 8 }} />
              </Card>
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>
      </Card>

      <Card style={{ marginTop: 16 }}>
        <div style={{ display: 'flex', gap: 8 }}>
          <TextArea
            value={input}
            onChange={e => setInput(e.target.value)}
            onPressEnter={e => {
              if (e.ctrlKey || e.metaKey) {
                handleSend()
              }
            }}
            placeholder="输入消息... (Ctrl+Enter 发送)"
            autoSize={{ minRows: 2, maxRows: 6 }}
            disabled={loading}
          />
          {streamingContent ? (
            <Button type="primary" danger onClick={handleCancel}>
              取消
            </Button>
          ) : (
            <Button
              type="primary"
              icon={<SendOutlined />}
              onClick={handleSend}
              loading={loading}
              disabled={!input.trim()}
              style={{ alignSelf: 'flex-end' }}
            />
          )}
        </div>
      </Card>
    </div>
  )
}
