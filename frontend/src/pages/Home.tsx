import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { riflesService, sessionsService, ammunitionService, locationsService } from '../services';
import type { SessionListDto } from '../types';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { StatCard, StatIcons, Skeleton, DopeBadge, VelocityBadge, GroupBadge } from '../components/ui';

interface DashboardStats {
  totalRifles: number;
  totalSessions: number;
  totalAmmo: number;
  totalLocations: number;
  recentSessions: SessionListDto[];
}

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
  const navigate = useNavigate();
  const [health, setHealth] = useState<HealthData | null>(null);
  const [healthLoading, setHealthLoading] = useState(true);
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [statsLoading, setStatsLoading] = useState(true);

  useEffect(() => {
    fetch('/api/health')
      .then((res) => res.json())
      .then((data: ApiHealthResponse) => {
        if (data.success) {
          setHealth(data.data);
        }
      })
      .catch(() => {})
      .finally(() => setHealthLoading(false));
  }, []);

  useEffect(() => {
    if (isAuthenticated) {
      loadDashboardStats();
    }
  }, [isAuthenticated]);

  const loadDashboardStats = async () => {
    try {
      setStatsLoading(true);
      const [riflesRes, sessionsRes, ammoRes, locationsRes] = await Promise.all([
        riflesService.getAll({ pageSize: 1 }),
        sessionsService.getAll({ pageSize: 5 }),
        ammunitionService.getAll({ pageSize: 1 }),
        locationsService.getAll(),
      ]);

      setStats({
        totalRifles: riflesRes.pagination.totalItems,
        totalSessions: sessionsRes.pagination.totalItems,
        totalAmmo: ammoRes.pagination.totalItems,
        totalLocations: locationsRes.length,
        recentSessions: sessionsRes.items,
      });
    } catch (error) {
      console.error('Failed to load dashboard stats:', error);
    } finally {
      setStatsLoading(false);
    }
  };

  // Authenticated user dashboard
  if (isAuthenticated) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
            <p className="text-gray-500">
              Welcome back, {user?.firstName || user?.email}
            </p>
          </div>
          <Button onClick={() => navigate('/sessions/new')}>+ New Session</Button>
        </div>

        {/* Stats Grid */}
        {statsLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            {[...Array(4)].map((_, i) => (
              <Skeleton key={i} className="h-32 w-full" />
            ))}
          </div>
        ) : stats && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            <StatCard
              label="Sessions"
              value={stats.totalSessions}
              icon={<StatIcons.session />}
              onClick={() => navigate('/sessions')}
            />
            <StatCard
              label="Rifles"
              value={stats.totalRifles}
              icon={<StatIcons.rifle />}
              onClick={() => navigate('/rifles')}
            />
            <StatCard
              label="Ammunition"
              value={stats.totalAmmo}
              icon={<StatIcons.target />}
              onClick={() => navigate('/ammunition')}
            />
            <StatCard
              label="Locations"
              value={stats.totalLocations}
              icon={<StatIcons.distance />}
              onClick={() => navigate('/locations')}
            />
          </div>
        )}

        {/* Quick Actions */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-8">
          <Card className="cursor-pointer hover:border-gray-300 transition-colors" onClick={() => navigate('/sessions/new')}>
            <CardHeader>
              <CardTitle className="text-lg">Record Session</CardTitle>
              <CardDescription>Log a new range session with DOPE, chrono, and group data</CardDescription>
            </CardHeader>
          </Card>
          <Card className="cursor-pointer hover:border-gray-300 transition-colors" onClick={() => navigate('/rifles/new')}>
            <CardHeader>
              <CardTitle className="text-lg">Add Rifle</CardTitle>
              <CardDescription>Add a new rifle to your collection</CardDescription>
            </CardHeader>
          </Card>
          <Card className="cursor-pointer hover:border-gray-300 transition-colors" onClick={() => navigate('/ammunition/new')}>
            <CardHeader>
              <CardTitle className="text-lg">Add Ammunition</CardTitle>
              <CardDescription>Track a new ammunition in your library</CardDescription>
            </CardHeader>
          </Card>
        </div>

        {/* Recent Sessions */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Recent Sessions</h2>
            <Button variant="outline" size="sm" onClick={() => navigate('/sessions')}>
              View All
            </Button>
          </div>
          {statsLoading ? (
            <Skeleton className="h-48 w-full" />
          ) : stats && stats.recentSessions.length === 0 ? (
            <div className="text-center py-8">
              <p className="text-gray-500 mb-4">No sessions recorded yet. Start by logging your first range session!</p>
              <Button onClick={() => navigate('/sessions/new')}>Record Session</Button>
            </div>
          ) : stats && (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Date</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Rifle</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Location</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">DOPE</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Chrono</th>
                    <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Groups</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {stats.recentSessions.map((session) => (
                    <tr
                      key={session.id}
                      className="hover:bg-gray-50 cursor-pointer"
                      onClick={() => navigate(`/sessions/${session.id}`)}
                    >
                      <td className="px-4 py-3 text-sm">
                        <Link to={`/sessions/${session.id}`} className="text-blue-600 hover:underline font-medium">
                          {new Date(session.sessionDate).toLocaleDateString()}
                        </Link>
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-900">{session.rifleName}</td>
                      <td className="px-4 py-3 text-sm text-gray-500">{session.locationName || '-'}</td>
                      <td className="px-4 py-3 text-sm">
                        {session.dopeCount > 0 ? <DopeBadge count={session.dopeCount} /> : '-'}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {session.chronoCount > 0 ? <VelocityBadge count={session.chronoCount} /> : '-'}
                      </td>
                      <td className="px-4 py-3 text-sm">
                        {session.groupCount > 0 ? <GroupBadge count={session.groupCount} /> : '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    );
  }

  // Public landing page
  return (
    <div className="container mx-auto px-4 py-12">
      <div className="mx-auto max-w-4xl">
        {/* Hero Section */}
        <div className="mb-12 text-center">
          <h1 className="mb-4 text-4xl font-bold text-gray-900">Welcome to TrueDope</h1>
          <p className="mb-8 text-xl text-gray-600">
            Professional ballistics data logging and analysis
          </p>

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
        </div>

        {/* Features */}
        <div className="grid md:grid-cols-3 gap-6 mb-12">
          <Card>
            <CardHeader>
              <CardTitle>Track DOPE</CardTitle>
              <CardDescription>
                Record your elevation holds at every distance for each rifle/ammo combination.
              </CardDescription>
            </CardHeader>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>Chronograph Data</CardTitle>
              <CardDescription>
                Log velocity readings with automatic statistical analysis (ES, SD, Avg).
              </CardDescription>
            </CardHeader>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>Group Analysis</CardTitle>
              <CardDescription>
                Measure and track group sizes in both inches and MOA.
              </CardDescription>
            </CardHeader>
          </Card>
        </div>

        {/* System Status */}
        <Card>
          <CardHeader>
            <CardTitle>System Status</CardTitle>
            <CardDescription>API and service health</CardDescription>
          </CardHeader>
          <CardContent>
            {healthLoading ? (
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
