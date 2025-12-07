import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';

interface HealthData {
  status: string;
  checks: Record<string, string>;
  version: string;
  environment: string;
  timestamp: string;
}

interface ApiHealthResponse {
  success: boolean;
  data: HealthData | null;
  message?: string;
}

export default function Home() {
  const { user, isAuthenticated } = useAuth();
  const [health, setHealth] = useState<HealthData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('/api/health')
      .then((res) => res.json())
      .then((data: ApiHealthResponse) => {
        if (data.success) {
          setHealth(data.data);
        }
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="container mx-auto px-4 py-12">
      <div className="mx-auto max-w-4xl">
        {/* Hero Section */}
        <div className="mb-12 text-center">
          <h1 className="mb-4 text-4xl font-bold text-gray-900">Welcome to TrueDope</h1>
          <p className="mb-8 text-xl text-gray-600">
            Professional ballistics data logging and analysis
          </p>

          {isAuthenticated ? (
            <div className="space-y-4">
              <p className="text-lg text-gray-700">
                Welcome back, <span className="font-medium">{user?.firstName || user?.email}</span>!
              </p>
              <div className="flex justify-center gap-4">
                <Link to="/settings">
                  <Button variant="outline">Settings</Button>
                </Link>
                {user?.isAdmin && (
                  <Link to="/admin/users">
                    <Button variant="outline">User Management</Button>
                  </Link>
                )}
              </div>
            </div>
          ) : (
            <div className="flex justify-center gap-4">
              <Link to="/login">
                <Button variant="outline" size="lg">
                  Sign in
                </Button>
              </Link>
              <Link to="/register">
                <Button size="lg">Get Started</Button>
              </Link>
            </div>
          )}
        </div>

        {/* System Status */}
        <Card>
          <CardHeader>
            <CardTitle>System Status</CardTitle>
            <CardDescription>API and service health</CardDescription>
          </CardHeader>
          <CardContent>
            {loading ? (
              <div className="flex items-center gap-2">
                <div className="h-4 w-4 animate-spin rounded-full border-2 border-gray-300 border-t-blue-600" />
                <span className="text-gray-600">Checking status...</span>
              </div>
            ) : health ? (
              <div className="space-y-4">
                <div className="flex items-center gap-3">
                  <span
                    className={`h-3 w-3 rounded-full ${
                      health.status === 'Healthy' ? 'bg-green-500' : 'bg-red-500'
                    }`}
                  />
                  <span className="font-medium text-gray-900">
                    API Status: {health.status}
                  </span>
                </div>

                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-gray-500">Version:</span>
                    <span className="ml-2 font-medium">{health.version}</span>
                  </div>
                  <div>
                    <span className="text-gray-500">Environment:</span>
                    <span className="ml-2 font-medium">{health.environment}</span>
                  </div>
                </div>

                {health.checks && Object.keys(health.checks).length > 0 && (
                  <div className="border-t pt-4">
                    <p className="mb-2 text-sm font-medium text-gray-700">Service Checks:</p>
                    <div className="grid gap-2 text-sm">
                      {Object.entries(health.checks).map(([service, status]) => (
                        <div key={service} className="flex items-center gap-2">
                          <span
                            className={`h-2 w-2 rounded-full ${
                              status === 'Healthy' ? 'bg-green-500' : 'bg-yellow-500'
                            }`}
                          />
                          <span className="text-gray-600">{service}:</span>
                          <span className="font-medium">{status}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ) : (
              <p className="text-gray-500">Unable to fetch status</p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
