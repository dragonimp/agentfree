export interface Agent {
  id: number
  name: string
  description: string
  agentType: 'Openclaw' | 'Hermes' | 'Goldfish'
  agentId?: string
  baseUrl: string
  apiKey?: string
  status: 'Active' | 'Inactive' | 'Error'
  createdAt: string
  updatedAt: string
}

export interface Adapter {
  id: number
  name: string
  protocol: string
  enabled: boolean
  config: Record<string, any>
}

export interface Tool {
  name: string
  enabled: boolean
  category: string
  description: string
}

export interface Session {
  id: string
  agentId: number
  agentName?: string
  name: string
  createdAt: string
  updatedAt: string
  messageCount?: number
}

export interface ChatMessage {
  id: number
  sessionId: string
  role: 'system' | 'user' | 'assistant'
  content: string
  toolCallId?: string
  createdAt: string
}

export interface ChatModel {
  id: string
  name: string
  provider: string
}

export interface CreateSessionParams {
  agentId: number
  name: string
}

export interface SendMessageParams {
  sessionId: string
  role: string
  content: string
  toolCallId?: string
}

export interface VersionInfo {
  name: string
  version: string
  buildDate: string
  buildNumber: number
  description: string
  techStack: string[]
  features: string[]
}
