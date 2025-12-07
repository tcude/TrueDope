import { useEffect, useState } from 'react'

interface HealthData {
  status: string
  checks: Record<string, string>
  version: string
  environment: string
  timestamp: string
}

interface ApiResponse {
  success: boolean
  data: HealthData | null
  message?: string
}

export default function Home() {
  const [health, setHealth] = useState<HealthData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/health')
      .then((res) => res.json())
      .then((data: ApiResponse) => {
        if (data.success) {
          setHealth(data.data)
        } else {
          setError(data.message || 'Failed to fetch health status')
        }
      })
      .catch(() => setError('Failed to connect to API'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center">
      <div className="bg-white rounded-lg shadow-lg p-8 max-w-md w-full">
        <h1 className="text-3xl font-bold text-gray-900 mb-6">TrueDope v2</h1>

        {loading && (
          <p className="text-gray-600">Loading...</p>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 rounded p-4">
            <p className="text-red-700">{error}</p>
          </div>
        )}

        {health && (
          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <span className={`w-3 h-3 rounded-full ${health.status === 'Healthy' ? 'bg-green-500' : 'bg-red-500'}`}></span>
              <span className="text-gray-700 font-medium">API Status: {health.status}</span>
            </div>
            <div className="text-sm text-gray-600">
              <p>Version: {health.version}</p>
              <p>Environment: {health.environment}</p>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
