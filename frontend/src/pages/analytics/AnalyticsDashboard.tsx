import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { riflesService, sessionsService } from '../../services';
import type { SessionListDto } from '../../types';
import { StatCard, StatIcons, Skeleton, Button, DopeBadge, VelocityBadge, GroupBadge } from '../../components/ui';
import { useToast } from '../../hooks';

interface DashboardStats {
  totalRifles: number;
  totalSessions: number;
  totalDopeEntries: number;
  recentSessions: SessionListDto[];
}

export default function AnalyticsDashboard() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [loading, setLoading] = useState(true);
  const [stats, setStats] = useState<DashboardStats>({
    totalRifles: 0,
    totalSessions: 0,
    totalDopeEntries: 0,
    recentSessions: [],
  });

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async () => {
    try {
      setLoading(true);
      const [riflesRes, sessionsRes] = await Promise.all([
        riflesService.getAll({ pageSize: 1 }),
        sessionsService.getAll({ pageSize: 5 }),
      ]);

      setStats({
        totalRifles: riflesRes.pagination.totalItems,
        totalSessions: sessionsRes.pagination.totalItems,
        totalDopeEntries: sessionsRes.items.reduce((sum, s) => sum + s.dopeCount, 0),
        recentSessions: sessionsRes.items,
      });
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load dashboard' });
    } finally {
      setLoading(false);
    }
  };


  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Skeleton className="h-8 w-48 mb-8" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
        </div>
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <Button onClick={() => navigate('/sessions/new')}>+ New Session</Button>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        <StatCard
          label="Total Rifles"
          value={stats.totalRifles}
          icon={StatIcons.rifle}
          onClick={() => navigate('/rifles')}
        />
        <StatCard
          label="Total Sessions"
          value={stats.totalSessions}
          icon={StatIcons.session}
          onClick={() => navigate('/sessions')}
        />
        <StatCard
          label="DOPE Entries"
          value={stats.totalDopeEntries}
          icon={StatIcons.distance}
        />
      </div>

      {/* Recent Sessions */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Recent Sessions</h2>
          <Button variant="outline" size="sm" onClick={() => navigate('/sessions')}>
            View All
          </Button>
        </div>
        {stats.recentSessions.length === 0 ? (
          <div className="text-center py-8">
            <p className="text-gray-500 mb-4">No sessions yet. Record your first range session!</p>
            <Button onClick={() => navigate('/sessions/new')}>New Session</Button>
          </div>
        ) : (
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
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {new Date(session.sessionDate).toLocaleDateString()}
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
