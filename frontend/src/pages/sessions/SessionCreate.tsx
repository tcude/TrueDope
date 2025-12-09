import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { sessionsService, riflesService, locationsService } from '../../services';
import type { RifleListDto, LocationListDto, CreateSessionDto } from '../../types';
import { Button, Select, Skeleton } from '../../components/ui';
import { useToast } from '../../hooks';

export default function SessionCreate() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [rifles, setRifles] = useState<RifleListDto[]>([]);
  const [locations, setLocations] = useState<LocationListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [formData, setFormData] = useState<CreateSessionDto>({
    rifleSetupId: 0,
    sessionDate: new Date().toISOString().split('T')[0],
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [riflesRes, locationsRes] = await Promise.all([
        riflesService.getAll({ pageSize: 100 }),
        locationsService.getAll(),
      ]);
      setRifles(riflesRes.items);
      setLocations(locationsRes);

      // Default to first rifle if available
      if (riflesRes.items.length > 0) {
        setFormData((prev) => ({ ...prev, rifleSetupId: riflesRes.items[0].id }));
      }
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load data' });
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.rifleSetupId) {
      addToast({ type: 'error', message: 'Please select a rifle' });
      return;
    }

    try {
      setSubmitting(true);
      const sessionId = await sessionsService.create(formData);
      addToast({ type: 'success', message: 'Session created' });
      navigate(`/sessions/${sessionId}/edit`);
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : 'Failed to create session' });
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-2xl">
        <Skeleton className="h-8 w-48 mb-8" />
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <Skeleton className="h-10 w-full mb-4" />
          <Skeleton className="h-10 w-full mb-4" />
          <Skeleton className="h-10 w-full" />
        </div>
      </div>
    );
  }

  if (rifles.length === 0) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-2xl">
        <Link to="/sessions" className="text-sm text-gray-500 hover:text-gray-700 mb-2 inline-block">
          &larr; Back to Sessions
        </Link>
        <h1 className="text-2xl font-bold text-gray-900 mb-8">New Session</h1>
        <div className="bg-white rounded-lg border border-gray-200 p-6 text-center">
          <p className="text-gray-500 mb-4">You need to add a rifle before creating a session.</p>
          <Button onClick={() => navigate('/rifles/new')}>Add Rifle</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <Link to="/sessions" className="text-sm text-gray-500 hover:text-gray-700 mb-2 inline-block">
        &larr; Back to Sessions
      </Link>
      <h1 className="text-2xl font-bold text-gray-900 mb-8">New Session</h1>

      <form onSubmit={handleSubmit} className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Date *
            </label>
            <input
              type="date"
              value={formData.sessionDate}
              onChange={(e) => setFormData((prev) => ({ ...prev, sessionDate: e.target.value }))}
              className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Rifle *
            </label>
            <Select
              value={formData.rifleSetupId.toString()}
              onChange={(value) => setFormData((prev) => ({ ...prev, rifleSetupId: parseInt(value) }))}
              options={rifles.map((r) => ({ value: r.id.toString(), label: r.name }))}
              placeholder="Select rifle..."
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Location
            </label>
            <Select
              value={formData.savedLocationId?.toString() || ''}
              onChange={(value) => setFormData((prev) => ({
                ...prev,
                savedLocationId: value ? parseInt(value) : undefined
              }))}
              options={[
                { value: '', label: 'No location' },
                ...locations.map((l) => ({ value: l.id.toString(), label: l.name })),
              ]}
              placeholder="Select location..."
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Notes
            </label>
            <textarea
              value={formData.notes || ''}
              onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))}
              rows={3}
              className="w-full px-3 py-2 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Session notes..."
            />
          </div>
        </div>

        <div className="flex justify-end gap-2 mt-6 pt-6 border-t border-gray-200">
          <Button type="button" variant="outline" onClick={() => navigate('/sessions')}>
            Cancel
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Creating...' : 'Create & Add Data'}
          </Button>
        </div>
      </form>
    </div>
  );
}
