import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { locationsService } from '../../services';
import type { LocationListDto } from '../../types';
import { Button, EmptyState, EmptyStateIcons, LoadingPage } from '../../components/ui';
import { useToast } from '../../hooks';
import { LocationMap } from '../../components/map';

type ViewMode = 'list' | 'map';

export default function LocationsList() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const [locations, setLocations] = useState<LocationListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [viewMode, setViewMode] = useState<ViewMode>('list');

  useEffect(() => {
    loadLocations();
  }, []);

  const loadLocations = async () => {
    try {
      setLoading(true);
      const data = await locationsService.getAll();
      setLocations(data);
    } catch {
      addToast({ type: 'error', message: 'Failed to load locations' });
    } finally {
      setLoading(false);
    }
  };

  const filteredLocations = search
    ? locations.filter((l) => l.name.toLowerCase().includes(search.toLowerCase()))
    : locations;

  const handleLocationSelect = (location: { id: number }) => {
    navigate(`/locations/${location.id}`);
  };

  if (loading) {
    return (
      <div className="container mx-auto px-4 py-8">
        <LoadingPage message="Loading locations..." />
      </div>
    );
  }

  if (locations.length === 0 && !search) {
    return (
      <div className="container mx-auto px-4 py-8">
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Locations</h1>
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

      {/* Search and View Toggle */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search locations..."
          className="w-full sm:max-w-md h-10 px-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <div className="flex bg-gray-100 dark:bg-gray-800 rounded-md p-1">
          <button
            type="button"
            onClick={() => setViewMode('list')}
            className={`px-3 py-1.5 text-sm font-medium rounded transition-colors ${
              viewMode === 'list'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
            }`}
          >
            <svg className="w-4 h-4 inline-block mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 10h16M4 14h16M4 18h16" />
            </svg>
            List
          </button>
          <button
            type="button"
            onClick={() => setViewMode('map')}
            className={`px-3 py-1.5 text-sm font-medium rounded transition-colors ${
              viewMode === 'map'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100'
            }`}
          >
            <svg className="w-4 h-4 inline-block mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
            </svg>
            Map
          </button>
        </div>
      </div>

      {filteredLocations.length === 0 ? (
        <p className="text-center text-gray-500 dark:text-gray-400 py-8">No locations match your search.</p>
      ) : viewMode === 'map' ? (
        <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
          <LocationMap
            locations={filteredLocations}
            onSelect={handleLocationSelect}
            height="500px"
            showClustering={true}
          />
          <div className="p-4 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Click a marker to view location details. Showing {filteredLocations.length} location{filteredLocations.length !== 1 ? 's' : ''}.
            </p>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredLocations.map((location) => (
            <Link
              key={location.id}
              to={`/locations/${location.id}`}
              className="block bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6 hover:border-gray-300 dark:hover:border-gray-600 hover:shadow-sm transition-all"
            >
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2">
                {location.name}
              </h3>
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
                {location.latitude.toFixed(4)}°, {location.longitude.toFixed(4)}°
              </p>
              {location.altitude && (
                <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">
                  Altitude: {location.altitude} ft
                </p>
              )}
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {location.sessionCount} session{location.sessionCount !== 1 ? 's' : ''}
              </p>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
