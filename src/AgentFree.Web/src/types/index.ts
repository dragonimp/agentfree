export interface Agent {
  id: number
  name: string
  description: string
  protocolType: string
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
