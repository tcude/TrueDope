import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { sessionsService, riflesService, locationsService } from '../../services';
import type {
  SessionDetailDto,
  RifleListDto,
  LocationListDto,
  UpdateSessionDto,
} from '../../types';
import {
  Button,
  Select,
  LoadingPage,
  Tabs,
} from '../../components/ui';
import { DopeTab, ChronoTab, GroupsTab, ImagesTab } from '../../components/sessions';
import { useToast } from '../../hooks';

export default function SessionEdit() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();

  const [session, setSession] = useState<SessionDetailDto | null>(null);
  const [rifles, setRifles] = useState<RifleListDto[]>([]);
  const [locations, setLocations] = useState<LocationListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [activeTab, setActiveTab] = useState('details');

  // Form data
  const [formData, setFormData] = useState<UpdateSessionDto>({});

  useEffect(() => {
    if (id) {
      loadData(parseInt(id));
    }
  }, [id]);

  const loadData = async (sessionId: number) => {
    try {
      setLoading(true);
      const [sessionData, riflesRes, locationsRes] = await Promise.all([
        sessionsService.getById(sessionId),
        riflesService.getAll({ pageSize: 100 }),
        locationsService.getAll(),
      ]);
      setSession(sessionData);
      setRifles(riflesRes.items);
      setLocations(locationsRes);
      // Convert UTC date to local date string for the date picker
      // This ensures the date picker shows the correct local date
      const localDate = new Date(sessionData.sessionDate);
      const localDateStr = `${localDate.getFullYear()}-${String(localDate.getMonth() + 1).padStart(2, '0')}-${String(localDate.getDate()).padStart(2, '0')}`;

      setFormData({
        sessionDate: localDateStr,
        rifleSetupId: sessionData.rifle.id,
        savedLocationId: sessionData.savedLocation?.id,
        notes: sessionData.notes || undefined,
        temperature: sessionData.temperature || undefined,
        humidity: sessionData.humidity || undefined,
        pressure: sessionData.pressure || undefined,
        windSpeed: sessionData.windSpeed || undefined,
        windDirection: sessionData.windDirection || undefined,
        densityAltitude: sessionData.densityAltitude || undefined,
      });
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load session' });
      navigate('/sessions');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!session) return;
    try {
      setSaving(true);
      await sessionsService.update(session.id, formData);
      addToast({ type: 'success', message: 'Session updated' });
      loadData(session.id);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to save session' });
    } finally {
      setSaving(false);
    }
  };

  const handleRefresh = () => {
    if (session) {
      loadData(session.id);
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <LoadingPage message="Loading session..." />
      </div>
    );
  }

  if (!session) {
    return null;
  }

  const tabs = [
    {
      id: 'details',
      label: 'Details',
      content: (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Date</label>
              <input
                type="date"
                value={formData.sessionDate || ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, sessionDate: e.target.value }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Rifle</label>
              <Select
                value={formData.rifleSetupId?.toString() || ''}
                onChange={(value) => setFormData((prev) => ({ ...prev, rifleSetupId: parseInt(value) }))}
                options={rifles.map((r) => ({ value: r.id.toString(), label: r.name }))}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Location</label>
              <Select
                value={formData.savedLocationId?.toString() || ''}
                onChange={(value) => setFormData((prev) => ({ ...prev, savedLocationId: value ? parseInt(value) : undefined }))}
                options={[
                  { value: '', label: 'No location' },
                  ...locations.map((l) => ({ value: l.id.toString(), label: l.name })),
                ]}
              />
            </div>
          </div>

          <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mt-6 mb-4">Conditions</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Temp (°F)</label>
              <input
                type="number"
                value={formData.temperature || ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, temperature: e.target.value ? parseFloat(e.target.value) : undefined }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Humidity (%)</label>
              <input
                type="number"
                value={formData.humidity ?? ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, humidity: e.target.value ? parseInt(e.target.value) : undefined }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Pressure (inHg)</label>
              <input
                type="number"
                step="0.01"
                value={formData.pressure || ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, pressure: e.target.value ? parseFloat(e.target.value) : undefined }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Wind (mph)</label>
              <input
                type="number"
                value={formData.windSpeed || ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, windSpeed: e.target.value ? parseFloat(e.target.value) : undefined }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Wind Dir (°)</label>
              <input
                type="number"
                min={0}
                max={359}
                value={formData.windDirection ?? ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, windDirection: e.target.value ? parseInt(e.target.value) : undefined }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Density Alt (ft)</label>
              <input
                type="number"
                value={formData.densityAltitude || ''}
                onChange={(e) => setFormData((prev) => ({ ...prev, densityAltitude: e.target.value ? parseFloat(e.target.value) : undefined }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>

          <div className="mt-6">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notes</label>
            <textarea
              value={formData.notes || ''}
              onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))}
              rows={3}
              className="w-full px-3 py-2 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div className="flex justify-end mt-6">
            <Button onClick={handleSave} disabled={saving}>
              {saving ? 'Saving...' : 'Save Details'}
            </Button>
          </div>
        </div>
      ),
    },
    {
      id: 'dope',
      label: `DOPE (${session.dopeEntries.length})`,
      content: (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <DopeTab
            sessionId={session.id}
            entries={session.dopeEntries}
            onUpdate={handleRefresh}
          />
        </div>
      ),
    },
    {
      id: 'chrono',
      label: `Chrono (${session.chronoSession ? session.chronoSession.velocityReadings.length : 0})`,
      content: (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <ChronoTab
            sessionId={session.id}
            chronoSession={session.chronoSession}
            onUpdate={handleRefresh}
          />
        </div>
      ),
    },
    {
      id: 'groups',
      label: `Groups (${session.groupEntries.length})`,
      content: (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <GroupsTab
            sessionId={session.id}
            entries={session.groupEntries}
            onUpdate={handleRefresh}
          />
        </div>
      ),
    },
    {
      id: 'images',
      label: 'Images',
      content: (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <ImagesTab
            parentType="session"
            parentId={session.id}
          />
        </div>
      ),
    },
  ];

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex items-start justify-between mb-8">
        <div>
          <Link to={`/sessions/${session.id}`} className="text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 mb-2 inline-block">
            &larr; Back to Session
          </Link>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Edit Session</h1>
          <p className="text-gray-500 dark:text-gray-400">
            {new Date(session.sessionDate).toLocaleDateString('en-US', {
              weekday: 'long',
              year: 'numeric',
              month: 'long',
              day: 'numeric'
            })}
            {' • '}
            {session.rifle.name}
          </p>
        </div>
      </div>

      <Tabs tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />
    </div>
  );
}
