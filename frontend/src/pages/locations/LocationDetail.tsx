import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { locationsService } from '../../services';
import type { LocationDetailDto } from '../../types';
import { Button, ConfirmDialog, Skeleton } from '../../components/ui';
import { useToast } from '../../hooks';
import { LocationPreview } from '../../components/map';

export default function LocationDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [location, setLocation] = useState<LocationDetailDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    if (id) {
      loadLocation(parseInt(id));
    }
  }, [id]);

  const loadLocation = async (locationId: number) => {
    try {
      setLoading(true);
      const data = await locationsService.getById(locationId);
      setLocation(data);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load location' });
      navigate('/locations');
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async () => {
    if (!location) return;
    try {
      setDeleting(true);
      await locationsService.delete(location.id);
      addToast({ type: 'success', message: 'Location deleted' });
      navigate('/locations');
    } catch (error) {
      addToast({ type: 'error', message: error instanceof Error ? error.message : 'Failed to delete location' });
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
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <Skeleton className="h-6 w-24 mb-4" />
          <div className="grid grid-cols-2 gap-4">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-full" />
          </div>
        </div>
      </div>
    );
  }

  if (!location) {
    return null;
  }

  return (
    <div className="container mx-auto px-4 py-8">
      {/* Header */}
      <div className="flex items-start justify-between mb-8">
        <div>
          <Link to="/locations" className="text-sm text-gray-500 hover:text-gray-700 mb-2 inline-block">
            &larr; Back to Locations
          </Link>
          <h1 className="text-2xl font-bold text-gray-900">{location.name}</h1>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => navigate(`/locations/${location.id}/edit`)}>
            Edit
          </Button>
          <Button variant="destructive" onClick={() => setShowDeleteConfirm(true)}>
            Delete
          </Button>
        </div>
      </div>

      {/* Map Preview */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Location</h2>
        <LocationPreview
          latitude={location.latitude}
          longitude={location.longitude}
          name={location.name}
          height="300px"
        />
        <div className="mt-4 flex flex-wrap items-center gap-4">
          <div>
            <p className="text-sm text-gray-500">Coordinates</p>
            <p className="font-medium">
              {location.latitude.toFixed(6)}°, {location.longitude.toFixed(6)}°
            </p>
          </div>
          {location.altitude && (
            <div>
              <p className="text-sm text-gray-500">Altitude</p>
              <p className="font-medium">{location.altitude.toLocaleString()} ft</p>
            </div>
          )}
          <a
            href={`https://www.google.com/maps/search/?api=1&query=${location.latitude},${location.longitude}`}
            target="_blank"
            rel="noopener noreferrer"
            className="text-sm text-blue-600 hover:text-blue-800 hover:underline"
          >
            Get Directions
          </a>
        </div>
      </div>

      {/* Description */}
      {location.description && (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Description</h2>
          <p className="text-gray-600 whitespace-pre-wrap">{location.description}</p>
        </div>
      )}

      {/* Delete Confirmation */}
      <ConfirmDialog
        isOpen={showDeleteConfirm}
        onClose={() => setShowDeleteConfirm(false)}
        onConfirm={handleDelete}
        title="Delete Location"
        message="Are you sure you want to delete this location? This action cannot be undone."
        confirmText="Delete"
        variant="danger"
        isLoading={deleting}
      />
    </div>
  );
}
