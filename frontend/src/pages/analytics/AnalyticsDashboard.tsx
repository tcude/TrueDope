import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { analyticsService, sessionsService } from '../../services';
import type { AnalyticsSummaryDto, SessionListDto } from '../../types';
import { StatCard, StatIcons, Skeleton, Button, DopeBadge, VelocityBadge, GroupBadge } from '../../components/ui';
import { useToast } from '../../hooks';

// Achievement Card Component
function AchievementCard({
  title,
  value,
  subtitle,
  icon,
  onClick,
}: {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: React.ReactNode;
  onClick?: () => void;
}) {
  return (
    <div
      className={`bg-white rounded-lg border border-gray-200 p-4 ${onClick ? 'cursor-pointer hover:border-blue-300 hover:shadow-sm transition-all' : ''}`}
      onClick={onClick}
    >
      <div className="flex items-start gap-3">
        <div className="p-2 bg-blue-50 rounded-lg text-blue-600">{icon}</div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-gray-500">{title}</p>
          <p className="text-lg font-semibold text-gray-900 truncate">{value}</p>
          {subtitle && <p className="text-xs text-gray-500 mt-1">{subtitle}</p>}
        </div>
      </div>
    </div>
  );
}

export default function AnalyticsDashboard() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<AnalyticsSummaryDto | null>(null);
  const [recentSessions, setRecentSessions] = useState<SessionListDto[]>([]);

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async () => {
    try {
      setLoading(true);
      const [summaryData, sessionsRes] = await Promise.all([
        analyticsService.getSummary(),
        sessionsService.getAll({ pageSize: 5 }),
      ]);

      setSummary(summaryData);
      setRecentSessions(sessionsRes.items);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load dashboard' });
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateStr: string | null | undefined) => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString();
  };

  const formatCurrency = (amount: number | null | undefined) => {
    if (amount == null) return 'N/A';
    return `$${amount.toFixed(2)}`;
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
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex items-center justify-between mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Analytics Dashboard</h1>
        <Button onClick={() => navigate('/sessions/new')}>+ New Session</Button>
      </div>

      {/* Main Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        <StatCard
          label="Total Sessions"
          value={summary?.totalSessions ?? 0}
          icon={<StatIcons.session />}
          onClick={() => navigate('/sessions')}
        />
        <StatCard
          label="Rounds Fired"
          value={summary?.totalRoundsFired ?? 0}
          icon={<StatIcons.bullet />}
        />
        <StatCard
          label="Total Cost"
          value={formatCurrency(summary?.totalCost?.amount)}
          icon={<StatIcons.cost />}
        />
      </div>

      {/* Achievement Cards */}
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Personal Bests</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Longest Shot */}
          <AchievementCard
            title="Longest Shot"
            value={summary?.longestShot ? `${summary.longestShot.distance} yds` : 'No data'}
            subtitle={
              summary?.longestShot
                ? `${summary.longestShot.rifleName} - ${formatDate(summary.longestShot.sessionDate)}`
                : undefined
            }
            icon={<StatIcons.distance className="w-5 h-5" />}
            onClick={
              summary?.longestShot
                ? () => navigate(`/sessions/${summary.longestShot!.sessionId}`)
                : undefined
            }
          />

          {/* Best Group */}
          <AchievementCard
            title="Best Group"
            value={summary?.bestGroup ? `${summary.bestGroup.sizeMoa.toFixed(2)} MOA` : 'No data'}
            subtitle={
              summary?.bestGroup
                ? `${summary.bestGroup.numberOfShots}-shot at ${summary.bestGroup.distance} yds`
                : undefined
            }
            icon={<StatIcons.group className="w-5 h-5" />}
            onClick={
              summary?.bestGroup
                ? () => navigate(`/sessions/${summary.bestGroup!.sessionId}`)
                : undefined
            }
          />

          {/* Lowest SD Ammo */}
          <AchievementCard
            title="Most Consistent Ammo"
            value={summary?.lowestSdAmmo ? `${summary.lowestSdAmmo.averageSd} fps SD` : 'No data'}
            subtitle={summary?.lowestSdAmmo ? summary.lowestSdAmmo.ammoName : undefined}
            icon={<StatIcons.velocity className="w-5 h-5" />}
            onClick={
              summary?.lowestSdAmmo
                ? () => navigate(`/ammo/${summary.lowestSdAmmo!.ammoId}`)
                : undefined
            }
          />

          {/* Recent Activity */}
          <AchievementCard
            title="Last 30 Days"
            value={
              summary?.recentActivity
                ? `${summary.recentActivity.sessionsLast30Days} sessions`
                : 'No data'
            }
            subtitle={
              summary?.recentActivity
                ? `${summary.recentActivity.roundsLast30Days} rounds fired`
                : undefined
            }
            icon={<StatIcons.calendar className="w-5 h-5" />}
          />
        </div>
      </div>

      {/* Quick Actions */}
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Quick Actions</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Button variant="outline" className="justify-center" onClick={() => navigate('/rifles')}>
            View Rifles
          </Button>
          <Button variant="outline" className="justify-center" onClick={() => navigate('/ammo')}>
            View Ammunition
          </Button>
          <Button variant="outline" className="justify-center" onClick={() => navigate('/locations')}>
            View Locations
          </Button>
          <Button variant="outline" className="justify-center" onClick={() => navigate('/sessions')}>
            View All Sessions
          </Button>
        </div>
      </div>

      {/* Recent Sessions */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Recent Sessions</h2>
          <Button variant="outline" size="sm" onClick={() => navigate('/sessions')}>
            View All
          </Button>
        </div>
        {recentSessions.length === 0 ? (
          <div className="text-center py-8">
            <p className="text-gray-500 mb-4">No sessions yet. Record your first range session!</p>
            <Button onClick={() => navigate('/sessions/new')}>New Session</Button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Date
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Rifle
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Location
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    DOPE
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Chrono
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Groups
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {recentSessions.map((session) => (
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
