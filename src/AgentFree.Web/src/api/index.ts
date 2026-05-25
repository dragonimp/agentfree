import axios from 'axios'
import type { Agent, Session, ChatMessage, ChatModel, CreateSessionParams, SendMessageParams } from '@/types'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
})

// ============ Agent API ============
export const getAgents = () => api.get<Agent[]>('/agents')
export const getAgent = (id: number) => api.get<Agent>(`/agents/${id}`)
export const createAgent = (data: Partial<Agent>) => api.post<Agent>('/agents', data)
export const updateAgent = (id: number, data: Partial<Agent>) => api.put<Agent>(`/agents/${id}`, data)
export const deleteAgent = (id: number) => api.delete(`/agents/${id}`)
export const startAgent = (id: number) => api.post(`/agents/${id}/start`)
export const stopAgent = (id: number) => api.post(`/agents/${id}/stop`)

// ============ Session API ============
export const getSessions = () => api.get<Session[]>('/sessions')
export const getSession = (id: string) => api.get<Session>(`/sessions/${id}`)
export const getMessages = (sessionId: string) => api.get<ChatMessage[]>(`/sessions/${sessionId}/messages`)
export const createSession = (data: CreateSessionParams) => api.post<Session>('/sessions', data)
export const updateSession = (id: string, data: { name?: string }) => api.put(`/sessions/${id}`, data)
export const deleteSession = (id: string) => api.delete(`/sessions/${id}`)
export const sendMessage = (data: SendMessageParams) => api.post<ChatMessage>(`/sessions/${data.sessionId}/messages`, {
  role: data.role,
  content: data.content,
  toolCallId: data.toolCallId,
})

// ============ Chat API ============
export const getChatModels = (agentId: number) => api.get<ChatModel[]>(`/agents/${agentId}/models`)

export default api
