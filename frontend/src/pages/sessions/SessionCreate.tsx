import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { sessionsService, riflesService, locationsService, weatherService } from '../../services';
import type { RifleListDto, LocationListDto, CreateSessionDto } from '../../types';
import { Button, Select, Skeleton, Collapsible, Badge } from '../../components/ui';
import { useToast, useGeolocation } from '../../hooks';

export default function SessionCreate() {
  const navigate = useNavigate();
  const { addToast } = useToast();
  const { position, error: geoError, loading: geoLoading, getCurrentPosition } = useGeolocation();

  const [rifles, setRifles] = useState<RifleListDto[]>([]);
  const [locations, setLocations] = useState<LocationListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [fetchingWeather, setFetchingWeather] = useState(false);
  const [weatherFetched, setWeatherFetched] = useState(false);

  const [formData, setFormData] = useState<CreateSessionDto>({
    rifleSetupId: 0,
    sessionDate: new Date().toISOString().split('T')[0],
  });

  useEffect(() => {
    loadData();
  }, []);

  // Auto-fetch weather when we have coordinates
  useEffect(() => {
    if (position && !weatherFetched) {
      fetchWeather(position.latitude, position.longitude);
    }
  }, [position]);

  // Update form with GPS coordinates when position changes
  useEffect(() => {
    if (position) {
      setFormData(prev => ({
        ...prev,
        latitude: position.latitude,
        longitude: position.longitude,
        locationName: prev.locationName || 'Current Location',
      }));
    }
  }, [position]);

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

  const handleLocationChange = async (locationId: string) => {
    if (!locationId) {
      // Clear location data
      setFormData(prev => ({
        ...prev,
        savedLocationId: undefined,
        latitude: undefined,
        longitude: undefined,
        locationName: undefined,
      }));
      setWeatherFetched(false);
      return;
    }

    const selectedLocation = locations.find(l => l.id === parseInt(locationId));
    if (selectedLocation) {
      setFormData(prev => ({
        ...prev,
        savedLocationId: selectedLocation.id,
        latitude: selectedLocation.latitude,
        longitude: selectedLocation.longitude,
        locationName: selectedLocation.name,
      }));

      // Auto-fetch weather for saved location
      if (selectedLocation.latitude && selectedLocation.longitude) {
        await fetchWeather(selectedLocation.latitude, selectedLocation.longitude, selectedLocation.altitude ?? undefined);
      }
    }
  };

  const fetchWeather = async (lat: number, lon: number, elevation?: number) => {
    try {
      setFetchingWeather(true);
      const weather = await weatherService.getWeather({ lat, lon, elevation });

      if (weather) {
        setFormData(prev => ({
          ...prev,
          temperature: Math.round(weather.temperature),
          humidity: Math.round(weather.humidity),
          pressure: Math.round(weather.pressure * 100) / 100,
          windSpeed: Math.round(weather.windSpeed),
          windDirection: Math.round(weather.windDirection),
          densityAltitude: weather.densityAltitude ? Math.round(weather.densityAltitude) : undefined,
        }));
        setWeatherFetched(true);
        addToast({ type: 'success', message: 'Weather data loaded' });
      }
    } catch (error) {
      addToast({ type: 'error', message: 'Failed to fetch weather data' });
    } finally {
      setFetchingWeather(false);
    }
  };

  const handleUseGPS = () => {
    // Clear saved location when using GPS
    setFormData(prev => ({
      ...prev,
      savedLocationId: undefined,
    }));
    setWeatherFetched(false);
    getCurrentPosition();
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

  // Count filled conditions for badge
  const conditionsCount = [
    formData.temperature,
    formData.humidity,
    formData.pressure,
    formData.windSpeed,
    formData.densityAltitude,
  ].filter(v => v !== undefined && v !== null).length;

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
      <h1 className="text-2xl font-bold text-gray-900 mb-2">New Session</h1>
      <p className="text-gray-500 text-sm mb-8">
        Create a session first, then add DOPE, chrono, and group data on the edit page.
      </p>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Basic Info Section */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Basic Info</h2>
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
          </div>
        </div>

        {/* Location Section */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Location</h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Saved Location
              </label>
              <Select
                value={formData.savedLocationId?.toString() || ''}
                onChange={handleLocationChange}
                options={[
                  { value: '', label: 'No saved location' },
                  ...locations.map((l) => ({ value: l.id.toString(), label: l.name })),
                ]}
                placeholder="Select location..."
              />
            </div>

            <div className="flex items-center gap-2">
              <span className="text-sm text-gray-500">or</span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleUseGPS}
                disabled={geoLoading}
              >
                {geoLoading ? (
                  <>
                    <svg className="animate-spin -ml-1 mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                    </svg>
                    Getting Location...
                  </>
                ) : (
                  <>
                    <svg className="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                    </svg>
                    Use Current GPS
                  </>
                )}
              </Button>
            </div>

            {geoError && (
              <p className="text-sm text-red-600">{geoError}</p>
            )}

            {formData.latitude && formData.longitude && (
              <div className="text-sm text-gray-600 bg-gray-50 rounded-md p-3">
                <div className="flex items-center gap-2">
                  <svg className="w-4 h-4 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                  </svg>
                  <span>
                    {formData.locationName || 'Location set'}: {formData.latitude.toFixed(4)}, {formData.longitude.toFixed(4)}
                  </span>
                </div>
                {fetchingWeather && (
                  <div className="flex items-center gap-2 mt-2 text-blue-600">
                    <svg className="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                    </svg>
                    <span>Fetching weather...</span>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {/* Conditions Section (Collapsible) */}
        <Collapsible
          title="Conditions"
          defaultOpen={conditionsCount > 0}
          badge={conditionsCount > 0 ? (
            <Badge variant="secondary">{conditionsCount} set</Badge>
          ) : undefined}
        >
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Temperature (°F)
              </label>
              <input
                type="number"
                value={formData.temperature ?? ''}
                onChange={(e) => setFormData(prev => ({
                  ...prev,
                  temperature: e.target.value ? parseInt(e.target.value) : undefined
                }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="72"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Humidity (%)
              </label>
              <input
                type="number"
                value={formData.humidity ?? ''}
                onChange={(e) => setFormData(prev => ({
                  ...prev,
                  humidity: e.target.value ? parseInt(e.target.value) : undefined
                }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                min="0"
                max="100"
                placeholder="50"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Pressure (inHg)
              </label>
              <input
                type="number"
                step="0.01"
                value={formData.pressure ?? ''}
                onChange={(e) => setFormData(prev => ({
                  ...prev,
                  pressure: e.target.value ? parseFloat(e.target.value) : undefined
                }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="29.92"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Wind Speed (mph)
              </label>
              <input
                type="number"
                value={formData.windSpeed ?? ''}
                onChange={(e) => setFormData(prev => ({
                  ...prev,
                  windSpeed: e.target.value ? parseInt(e.target.value) : undefined
                }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                min="0"
                placeholder="5"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Wind Direction (°)
              </label>
              <input
                type="number"
                value={formData.windDirection ?? ''}
                onChange={(e) => setFormData(prev => ({
                  ...prev,
                  windDirection: e.target.value ? parseInt(e.target.value) : undefined
                }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                min="0"
                max="360"
                placeholder="180"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Density Altitude (ft)
              </label>
              <input
                type="number"
                value={formData.densityAltitude ?? ''}
                onChange={(e) => setFormData(prev => ({
                  ...prev,
                  densityAltitude: e.target.value ? parseInt(e.target.value) : undefined
                }))}
                className="w-full h-10 px-3 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="1500"
              />
            </div>
          </div>

          {weatherFetched && (
            <p className="text-sm text-green-600 mt-3 flex items-center gap-1">
              <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
              Weather data auto-filled from current conditions
            </p>
          )}
        </Collapsible>

        {/* Notes Section */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Notes</h2>
          <textarea
            value={formData.notes || ''}
            onChange={(e) => setFormData((prev) => ({ ...prev, notes: e.target.value }))}
            rows={3}
            className="w-full px-3 py-2 rounded-md border border-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Session notes..."
          />
        </div>

        {/* Submit Buttons */}
        <div className="flex justify-end gap-2">
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
