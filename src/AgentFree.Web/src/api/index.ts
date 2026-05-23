import axios from 'axios'
import type { Agent } from '@/types'

const api = axios.create({
  baseURL: '/api',
  timeout: 10000,
})

export const getAgents = () => api.get<Agent[]>('/agents')
export const getAgent = (id: number) => api.get<Agent>(`/agents/${id}`)
export const createAgent = (data: Partial<Agent>) => api.post<Agent>('/agents', data)
export const updateAgent = (id: number, data: Partial<Agent>) => api.put<Agent>(`/agents/${id}`, data)
export const deleteAgent = (id: number) => api.delete(`/agents/${id}`)
export const startAgent = (id: number) => api.post(`/agents/${id}/start`)
export const stopAgent = (id: number) => api.post(`/agents/${id}/stop`)

export default api
