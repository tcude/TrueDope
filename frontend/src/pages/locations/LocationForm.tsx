import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { locationsService } from '../../services';
import type { CreateLocationDto, UpdateLocationDto } from '../../types';
import { Button, Skeleton } from '../../components/ui';
import { useToast } from '../../hooks';

interface LocationFormProps {
  mode: 'create' | 'edit';
}

export default function LocationForm({ mode }: LocationFormProps) {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [loading, setLoading] = useState(mode === 'edit');
  const [submitting, setSubmitting] = useState(false);

  const [formData, setFormData] = useState<CreateLocationDto>({
    name: '',
    latitude: 0,
    longitude: 0,
  });

  useEffect(() => {
    if (mode === 'edit' && id) {
      loadLocation(parseInt(id));
    }
  }, [mode, id]);

  const loadLocation = async (locationId: number) => {
    try {
      setLoading(true);
      const location = await locationsService.getById(locationId);
      setFormData({
        name: location.name,
        latitude: location.latitude,
        longitude: location.longitude,
        altitude: location.altitude ?? undefined,
        description: location.description ?? undefined,
      });
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load location' });
      navigate('/locations');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name.trim()) {
      addToast({ type: 'error', message: 'Name is required' });
      return;
    }

    try {
      setSubmitting(true);
      if (mode === 'create') {
        const locationId = await locationsService.create(formData);
        addToast({ type: 'success', message: 'Location created' });
        navigate(`/locations/${locationId}`);
      } else if (id) {
        await locationsService.update(parseInt(id), formData as UpdateLocationDto);
        addToast({ type: 'success', message: 'Location updated' });
        navigate(`/locations/${id}`);
      }
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : `Failed to ${mode} location` });
    } finally {
      setSubmitting(false);
    }
  };

  const updateField = <K extends keyof CreateLocationDto>(key: K, value: CreateLocationDto[K]) => {
    setFormData((prev) => ({ ...prev, [key]: value }));
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8 max-w-2xl">
        <Skeleton className="h-8 w-48 mb-8" />
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <div className="space-y-4">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl">
      <Link to={mode === 'edit' && id ? `/locations/${id}` : '/locations'} className="text-sm text-gray-500 hover:text-gray-700 mb-2 inline-block">
        &larr; Back
      </Link>
      <h1 className="text-2xl font-bold text-gray-900 mb-8">
        {mode === 'create' ? 'Add Location' : 'Edit Location'}
      </h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Info */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Location Information</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Name *
              </label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => updateField('name', e.target.value)}
                placeholder="e.g., Thunder Valley Precision"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Latitude *
              </label>
              <input
                type="number"
                step="0.0001"
                value={formData.latitude || ''}
                onChange={(e) => updateField('latitude', e.target.value ? parseFloat(e.target.value) : 0)}
                placeholder="e.g., 30.1902"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Longitude *
              </label>
              <input
                type="number"
                step="0.0001"
                value={formData.longitude || ''}
                onChange={(e) => updateField('longitude', e.target.value ? parseFloat(e.target.value) : 0)}
                placeholder="e.g., -98.0867"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Altitude (feet)
              </label>
              <input
                type="number"
                value={formData.altitude || ''}
                onChange={(e) => updateField('altitude', e.target.value ? parseInt(e.target.value) : undefined)}
                placeholder="e.g., 1200"
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">
            Coordinates are used for weather data and ballistic calculations.
          </p>
        </div>

        {/* Description */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Description</h2>
          <textarea
            value={formData.description || ''}
            onChange={(e) => updateField('description', e.target.value)}
            rows={4}
            placeholder="Any additional notes about this location..."
            className="w-full px-3 py-2 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate(mode === 'edit' && id ? `/locations/${id}` : '/locations')}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Saving...' : mode === 'create' ? 'Create Location' : 'Save Changes'}
          </Button>
        </div>
      </form>
    </div>
  );
}
