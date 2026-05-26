import React, { useState, useEffect, useRef } from 'react'
import { Card, Input, Button, Typography, Avatar, Spin, message } from 'antd'
import { SendOutlined, StopOutlined, UserOutlined } from '@ant-design/icons'
import ReactMarkdown from 'react-markdown'
import { getMessages, streamChat, getSession } from '../api'
import type { ChatMessage, Session as SessionType } from '../types'

const { TextArea } = Input
const { Text } = Typography

interface ChatViewProps {
  sessionId: string
}

export default function ChatView({ sessionId }: ChatViewProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [session, setSession] = useState<SessionType | null>(null)
  const [streamingContent, setStreamingContent] = useState('')
  const [error, setError] = useState('')
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const abortRef = useRef<() => void>(() => {})

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }

  useEffect(() => { scrollToBottom() }, [messages, streamingContent])

  useEffect(() => {
    if (!sessionId) return
    loadMessages()
  }, [sessionId])

  const loadMessages = async () => {
    if (!sessionId) return
    setLoading(true)
    setError('')
    try {
      const [messagesRes, sessionRes] = await Promise.all([
        getMessages(sessionId),
        getSession(sessionId),
      ])
      setMessages(messagesRes.data || [])
      setSession(sessionRes.data || null)
    } catch (err: any) {
      const msg = err.response?.data?.message || err.message
      setError('加载消息失败: ' + msg)
      console.error('Load messages error:', err)
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
    setError('')

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
      const msg = err.response?.data?.message || err.message
      setError('发送失败: ' + msg)
      console.error('Send message error:', err)
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
          <Avatar style={{ backgroundColor: '#667eea', marginRight: 8, flexShrink: 0, width: 36, height: 36, fontSize: 16 }}>
            🐠
          </Avatar>
        )}
        <Card
          size="small"
          style={{
            maxWidth: '75%',
            background: isUser ? '#e6f4ff' : '#fff',
            borderColor: isUser ? '#91d5ff' : '#f0f0f0',
            borderRadius: 12,
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
          <Text type="secondary" style={{ fontSize: 11, display: 'block', marginTop: 4, textAlign: 'right' }}>
            {new Date(msg.createdAt).toLocaleTimeString('zh-CN')}
          </Text>
        </Card>
        {isUser && (
          <Avatar style={{ backgroundColor: '#1890ff', marginLeft: 8, flexShrink: 0, width: 36, height: 36, fontSize: 16 }}>
            <UserOutlined />
          </Avatar>
        )}
      </div>
    )
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%', overflow: 'hidden' }}>
      {/* Messages area - internal scroll */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '16px 24px', background: '#f5f6fa' }}>
        {error && (
          <div style={{ textAlign: 'center', padding: '12px 0', color: '#ff4d4f', fontSize: 13 }}>
            {error}
          </div>
        )}
        {messages.map(renderMessage)}
        {streamingContent && (
          <div style={{ display: 'flex', marginBottom: 16, justifyContent: 'flex-start' }}>
            <Avatar style={{ backgroundColor: '#667eea', marginRight: 8, flexShrink: 0, width: 36, height: 36, fontSize: 16 }}>🐠</Avatar>
            <Card size="small" style={{ maxWidth: '75%', background: '#fff', borderColor: '#f0f0f0', borderRadius: 12 }}>
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

      {/* Input area - fixed at bottom */}
      <div style={{ padding: '12px 24px 16px', background: '#fff', borderTop: '1px solid #f0f0f0', flexShrink: 0 }}>
        <div style={{ display: 'flex', gap: 8, alignItems: 'flex-end' }}>
          <TextArea
            value={input}
            onChange={e => setInput(e.target.value)}
            onPressEnter={e => {
              if (e.ctrlKey || e.metaKey) {
                handleSend()
              }
            }}
            placeholder="输入消息... (Enter 发送, Ctrl+Enter 换行)"
            autoSize={{ minRows: 1, maxRows: 4 }}
            disabled={loading}
            style={{ flex: 1, borderRadius: 8 }}
          />
          {streamingContent ? (
            <Button
              type="primary"
              danger
              icon={<StopOutlined />}
              onClick={handleCancel}
              style={{ borderRadius: 8 }}
            >
              停止
            </Button>
          ) : (
            <Button
              type="primary"
              icon={<SendOutlined />}
              onClick={handleSend}
              loading={loading}
              disabled={!input.trim()}
              style={{ borderRadius: 8 }}
            />
          )}
        </div>
      </div>
    </div>
  )
}
