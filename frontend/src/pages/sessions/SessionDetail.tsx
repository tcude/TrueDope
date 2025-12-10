import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { sessionsService } from '../../services';
import type { SessionDetailDto } from '../../types';
import { Button, ConfirmDialog, Skeleton, Tabs, StatCard, StatIcons } from '../../components/ui';
import { DopeTab, ChronoTab, GroupsTab } from '../../components/sessions';
import { useToast } from '../../hooks';

export default function SessionDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [session, setSession] = useState<SessionDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    if (id) {
      loadSession(parseInt(id));
    }
  }, [id]);

  const loadSession = async (sessionId: number) => {
    try {
      setLoading(true);
      const data = await sessionsService.getById(sessionId);
      setSession(data);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load session' });
      navigate('/sessions');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!session) return;
    try {
      setDeleting(true);
      await sessionsService.delete(session.id);
      addToast({ type: 'success', message: 'Session deleted' });
      navigate('/sessions');
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : 'Failed to delete session' });
    } finally {
      setDeleting(false);
      setShowDeleteConfirm(false);
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <Skeleton className="h-8 w-64 mb-4" />
        <Skeleton className="h-4 w-32 mb-8" />
        <div className="grid grid-cols-4 gap-4 mb-6">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      </div>
    );
  }

  if (!session) {
    return null;
  }

  // Calculate summary stats
  const chronoRounds = session.chronoSession?.velocityReadings.length || 0;
  const avgVelocity = session.chronoSession?.averageVelocity;
  const bestGroup = session.groupEntries.length > 0
    ? Math.min(...session.groupEntries.filter(g => g.groupSizeMoa).map(g => g.groupSizeMoa!))
    : null;

  const tabs = [
    {
      id: 'dope',
      label: `DOPE (${session.dopeEntries.length})`,
      content: (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <DopeTab
            sessionId={session.id}
            entries={session.dopeEntries}
            onUpdate={() => loadSession(session.id)}
            readOnly
          />
        </div>
      ),
    },
    {
      id: 'chrono',
      label: `Chrono (${chronoRounds})`,
      content: (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <ChronoTab
            sessionId={session.id}
            chronoSession={session.chronoSession}
            onUpdate={() => loadSession(session.id)}
            readOnly
          />
        </div>
      ),
    },
    {
      id: 'groups',
      label: `Groups (${session.groupEntries.length})`,
      content: (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <GroupsTab
            sessionId={session.id}
            entries={session.groupEntries}
            onUpdate={() => loadSession(session.id)}
            readOnly
          />
        </div>
      ),
    },
  ];

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex items-start justify-between mb-8">
        <div>
          <Link to="/sessions" className="text-sm text-gray-500 hover:text-gray-700 mb-2 inline-block">
            &larr; Back to Sessions
          </Link>
          <h1 className="text-2xl font-bold text-gray-900">
            {new Date(session.sessionDate).toLocaleDateString('en-US', {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric'
            })}
          </h1>
          <p className="text-gray-500">
            {session.rifle.name} {session.locationName && `• ${session.locationName}`}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => navigate(`/sessions/${session.id}/edit`)}>
            Edit
          </Button>
          <Button variant="destructive" onClick={() => setShowDeleteConfirm(true)}>
            Delete
          </Button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <StatCard
          label="DOPE Entries"
          value={session.dopeEntries.length}
          icon={<StatIcons.distance />}
        />
        <StatCard
          label="Velocity Readings"
          value={chronoRounds}
          subValue={avgVelocity ? `Avg: ${Math.round(avgVelocity)} fps` : undefined}
          icon={<StatIcons.velocity />}
        />
        <StatCard
          label="Groups"
          value={session.groupEntries.length}
          subValue={bestGroup ? `Best: ${bestGroup.toFixed(2)} MOA` : undefined}
          icon={<StatIcons.target />}
        />
        <StatCard
          label="Temperature"
          value={session.temperature ? `${session.temperature}°F` : '-'}
          icon={<StatIcons.temperature />}
        />
      </div>

      {/* Conditions */}
      {(session.temperature || session.humidity || session.pressure || session.windSpeed || session.densityAltitude) && (
        <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Conditions</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-6">
            {session.temperature && (
              <div>
                <p className="text-sm text-gray-500">Temperature</p>
                <p className="font-medium">{session.temperature}°F</p>
              </div>
            )}
            {session.humidity && (
              <div>
                <p className="text-sm text-gray-500">Humidity</p>
                <p className="font-medium">{session.humidity}%</p>
              </div>
            )}
            {session.pressure && (
              <div>
                <p className="text-sm text-gray-500">Pressure</p>
                <p className="font-medium">{session.pressure}" Hg</p>
              </div>
            )}
            {session.windSpeed && (
              <div>
                <p className="text-sm text-gray-500">Wind</p>
                <p className="font-medium">
                  {session.windSpeed} mph
                  {session.windDirectionCardinal && ` ${session.windDirectionCardinal}`}
                </p>
              </div>
            )}
            {session.windDirection !== null && session.windDirection !== undefined && (
              <div>
                <p className="text-sm text-gray-500">Wind Direction</p>
                <p className="font-medium">{session.windDirection}°</p>
              </div>
            )}
            {session.densityAltitude && (
              <div>
                <p className="text-sm text-gray-500">Density Altitude</p>
                <p className="font-medium">{session.densityAltitude.toLocaleString()} ft</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Data Tabs */}
      <Tabs tabs={tabs} defaultTab="dope" />

      {/* Notes */}
      {session.notes && (
        <div className="bg-white rounded-lg border border-gray-200 p-6 mt-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Notes</h2>
          <p className="text-gray-600 whitespace-pre-wrap">{session.notes}</p>
        </div>
      )}

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={showDeleteConfirm}
        onClose={() => setShowDeleteConfirm(false)}
        onConfirm={handleDelete}
        title="Delete Session"
        message="Are you sure you want to delete this session and all its data? This action cannot be undone."
        confirmText="Delete"
        variant="danger"
        isLoading={deleting}
      />
    </div>
  );
}
