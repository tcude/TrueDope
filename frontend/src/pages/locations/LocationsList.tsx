import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { locationsService } from '../../services';
import type { LocationListDto } from '../../types';
import { Button, EmptyState, EmptyStateIcons, Skeleton } from '../../components/ui';
import { useToast } from '../../hooks';

export default function LocationsList() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [locations, setLocations] = useState<LocationListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  useEffect(() => {
    loadLocations();
  }, []);

  const loadLocations = async () => {
    try {
      setLoading(true);
      const data = await locationsService.getAll();
      setLocations(data);
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to load locations' });
    } finally {
      setLoading(false);
    }
  };

  const filteredLocations = search
    ? locations.filter((l) => l.name.toLowerCase().includes(search.toLowerCase()))
    : locations;

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Locations</h1>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[...Array(6)].map((_, i) => (
            <Skeleton key={i} className="h-40 w-full" />
          ))}
        </div>
      </div>
    );
  }

  if (locations.length === 0 && !search) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Locations</h1>
        </div>
        <EmptyState
          icon={EmptyStateIcons.location}
          title="No locations yet"
          description="Save your favorite shooting locations for quick access."
          action={{
            label: 'Add Location',
            onClick: () => navigate('/locations/new'),
          }}
        />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Locations</h1>
        <Button onClick={() => navigate('/locations/new')}>
          + New Location
        </Button>
      </div>

      {/* Search */}
      <div className="mb-6">
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search locations..."
          className="w-full max-w-md h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {filteredLocations.length === 0 ? (
        <p className="text-center text-gray-500 py-8">No locations match your search.</p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredLocations.map((location) => (
            <Link
              key={location.id}
              to={`/locations/${location.id}`}
              className="block bg-white rounded-lg border border-gray-200 p-6 hover:border-gray-300 hover:shadow-sm transition-all"
            >
              <h3 className="text-lg font-semibold text-gray-900 mb-2">
                {location.name}
              </h3>
              <p className="text-sm text-gray-500 mb-4">
                {location.latitude.toFixed(4)}°, {location.longitude.toFixed(4)}°
              </p>
              {location.altitude && (
                <p className="text-sm text-gray-500 mb-2">
                  Altitude: {location.altitude} ft
                </p>
              )}
              <p className="text-sm text-gray-500">
                {location.sessionCount} session{location.sessionCount !== 1 ? 's' : ''}
              </p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
