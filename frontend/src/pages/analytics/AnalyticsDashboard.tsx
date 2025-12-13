import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { analyticsService, sessionsService } from '../../services';
import type { AnalyticsSummaryDto, SessionListDto } from '../../types';
import { StatCard, StatIcons, LoadingPage, Button, DopeBadge, VelocityBadge, GroupBadge, PageHeader } from '../../components/ui';
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
      className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-4 ${onClick ? 'cursor-pointer hover:border-blue-300 dark:hover:border-blue-600 hover:shadow-sm transition-all' : ''}`}
      onClick={onClick}
    >
      <div className="flex items-start gap-3">
        <div className="p-2 bg-blue-50 dark:bg-blue-900/30 rounded-lg text-blue-600 dark:text-blue-400">{icon}</div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-gray-500 dark:text-gray-400">{title}</p>
          <p className="text-lg font-semibold text-gray-900 dark:text-gray-100 truncate">{value}</p>
          {subtitle && <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">{subtitle}</p>}
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
        <LoadingPage message="Loading analytics..." />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <PageHeader
        title="Analytics Dashboard"
        description="Overview of your shooting performance and statistics"
      />

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
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Personal Bests</h2>
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

      {/* Analytics Links */}
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">Analytics</h2>
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
          <Button variant="outline" className="justify-center" onClick={() => navigate('/analytics/dope-chart')}>
            DOPE Chart
          </Button>
          <Button variant="outline" className="justify-center" onClick={() => navigate('/analytics/velocity-trends')}>
            Velocity Trends
          </Button>
          <Button variant="outline" className="justify-center" onClick={() => navigate('/analytics/ammo-comparison')}>
            Ammo Comparison
          </Button>
          <Button variant="outline" className="justify-center" onClick={() => navigate('/analytics/lot-comparison')}>
            Lot Comparison
          </Button>
          <Button variant="outline" className="justify-center" onClick={() => navigate('/analytics/cost-analysis')}>
            Cost Analysis
          </Button>
          <Button variant="outline" className="justify-center" onClick={() => navigate('/sessions')}>
            All Sessions
          </Button>
        </div>
      </div>

      {/* Recent Sessions */}
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Recent Sessions</h2>
          <Button variant="outline" size="sm" onClick={() => navigate('/sessions')}>
            View All
          </Button>
        </div>
        {recentSessions.length === 0 ? (
          <div className="text-center py-8">
            <p className="text-gray-500 dark:text-gray-400 mb-4">No sessions yet. Record your first range session!</p>
            <Button onClick={() => navigate('/sessions/new')}>New Session</Button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead className="bg-gray-50 dark:bg-gray-900">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Date
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Rifle
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Location
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    DOPE
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Chrono
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Groups
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                {recentSessions.map((session) => (
                  <tr
                    key={session.id}
                    className="hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer transition-colors"
                    onClick={() => navigate(`/sessions/${session.id}`)}
                  >
                    <td className="px-4 py-3 text-sm text-gray-900 dark:text-gray-100">
                      {new Date(session.sessionDate).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900 dark:text-gray-100">{session.rifleName}</td>
                    <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{session.locationName || '-'}</td>
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
