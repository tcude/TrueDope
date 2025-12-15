import { useState, useCallback, useEffect, useRef } from 'react';
import { Marker, useMapEvents, useMap } from 'react-leaflet';
import L from 'leaflet';
import { MapWrapper } from './MapWrapper';
import { Button } from '../ui';
import { useGeolocation } from '../../hooks/useGeolocation';
import { geocodingService } from '../../services';
import type { GeocodingSearchResult } from '../../types';

// Fix for default marker icon in React-Leaflet
// https://github.com/Leaflet/Leaflet/issues/4968
import iconUrl from 'leaflet/dist/images/marker-icon.png';
import iconRetinaUrl from 'leaflet/dist/images/marker-icon-2x.png';
import shadowUrl from 'leaflet/dist/images/marker-shadow.png';

const defaultIcon = L.icon({
  iconUrl,
  iconRetinaUrl,
  shadowUrl,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
});

L.Marker.prototype.options.icon = defaultIcon;

interface Coordinates {
  latitude: number;
  longitude: number;
}

interface LocationPickerProps {
  value?: Coordinates;
  onChange: (coords: Coordinates) => void;
  onElevationChange?: (elevation: number) => void;
  initialCenter?: { lat: number; lng: number };
  initialZoom?: number;
  showSearch?: boolean;
  showCurrentLocation?: boolean;
  autoFetchElevation?: boolean;
  height?: string;
}

// Component to handle map click events
function MapClickHandler({ onLocationSelect }: { onLocationSelect: (lat: number, lng: number) => void }) {
  useMapEvents({
    click: (e) => {
      onLocationSelect(e.latlng.lat, e.latlng.lng);
    },
  });
  return null;
}

// Component to handle flying to new location
function FlyToLocation({ position, shouldFly }: { position: { lat: number; lng: number } | null; shouldFly: boolean }) {
  const map = useMap();

  useEffect(() => {
    if (position && shouldFly) {
      map.flyTo([position.lat, position.lng], 14, { duration: 1.5 });
    }
  }, [map, position, shouldFly]);

  return null;
}

export function LocationPicker({
  value,
  onChange,
  onElevationChange,
  initialCenter,
  initialZoom = 4,
  showSearch = true,
  showCurrentLocation = true,
  autoFetchElevation = true,
  height = '400px',
}: LocationPickerProps) {
  const { position: gpsPosition, loading: gpsLoading, error: gpsError, getCurrentPosition } = useGeolocation();
  const [shouldFlyToGps, setShouldFlyToGps] = useState(false);
  const [shouldFlyToSearch, setShouldFlyToSearch] = useState(false);
  const [searchPosition, setSearchPosition] = useState<{ lat: number; lng: number } | null>(null);

  // Search state
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<GeocodingSearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [showResults, setShowResults] = useState(false);
  const searchTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const searchContainerRef = useRef<HTMLDivElement>(null);

  // Elevation fetch state
  const elevationTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [isFetchingElevation, setIsFetchingElevation] = useState(false);

  // Convert value to marker position
  const markerPosition = value
    ? { lat: value.latitude, lng: value.longitude }
    : null;

  // Determine map center
  const mapCenter = initialCenter ||
    (value ? { lat: value.latitude, lng: value.longitude } : undefined);

  // Debounced elevation fetch
  const fetchElevation = useCallback(async (lat: number, lng: number) => {
    if (!autoFetchElevation || !onElevationChange) return;

    if (elevationTimeoutRef.current) {
      clearTimeout(elevationTimeoutRef.current);
    }

    elevationTimeoutRef.current = setTimeout(async () => {
      setIsFetchingElevation(true);
      try {
        const result = await geocodingService.getElevation(lat, lng);
        if (result) {
          onElevationChange(result.elevation);
        }
      } catch (error) {
        console.error('Failed to fetch elevation:', error);
      } finally {
        setIsFetchingElevation(false);
      }
    }, 500); // 500ms debounce
  }, [autoFetchElevation, onElevationChange]);

  const handleLocationSelect = useCallback((lat: number, lng: number) => {
    onChange({ latitude: lat, longitude: lng });
    setShouldFlyToGps(false);
    setShouldFlyToSearch(false);
    fetchElevation(lat, lng);
  }, [onChange, fetchElevation]);

  const handleMarkerDrag = useCallback((e: L.DragEndEvent) => {
    const marker = e.target;
    const position = marker.getLatLng();
    onChange({ latitude: position.lat, longitude: position.lng });
    setShouldFlyToGps(false);
    setShouldFlyToSearch(false);
    fetchElevation(position.lat, position.lng);
  }, [onChange, fetchElevation]);

  const handleUseCurrentLocation = useCallback(() => {
    getCurrentPosition();
    setShouldFlyToGps(true);
    setShouldFlyToSearch(false);
  }, [getCurrentPosition]);

  // Update coordinates when GPS position changes
  useEffect(() => {
    if (gpsPosition && shouldFlyToGps) {
      onChange({ latitude: gpsPosition.latitude, longitude: gpsPosition.longitude });
      fetchElevation(gpsPosition.latitude, gpsPosition.longitude);
    }
  }, [gpsPosition, shouldFlyToGps, onChange, fetchElevation]);

  // Debounced search
  useEffect(() => {
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    if (searchQuery.length < 3) {
      setSearchResults([]);
      setShowResults(false);
      return;
    }

    searchTimeoutRef.current = setTimeout(async () => {
      setIsSearching(true);
      try {
        const results = await geocodingService.search(searchQuery, 5);
        setSearchResults(results);
        setShowResults(results.length > 0);
      } catch (error) {
        console.error('Search failed:', error);
        setSearchResults([]);
      } finally {
        setIsSearching(false);
      }
    }, 300); // 300ms debounce

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [searchQuery]);

  // Handle click outside search results
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (searchContainerRef.current && !searchContainerRef.current.contains(event.target as Node)) {
        setShowResults(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSearchResultSelect = useCallback((result: GeocodingSearchResult) => {
    onChange({ latitude: result.latitude, longitude: result.longitude });
    setSearchPosition({ lat: result.latitude, lng: result.longitude });
    setShouldFlyToSearch(true);
    setShouldFlyToGps(false);
    setShowResults(false);
    setSearchQuery('');
    fetchElevation(result.latitude, result.longitude);
  }, [onChange, fetchElevation]);

  const gpsMarkerPosition = shouldFlyToGps && gpsPosition
    ? { lat: gpsPosition.latitude, lng: gpsPosition.longitude }
    : null;

  return (
    <div className="space-y-3">
      <div className="flex flex-col sm:flex-row gap-2">
        {/* Search box */}
        {showSearch && (
          <div className="relative flex-1" ref={searchContainerRef}>
            <div className="relative">
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search for a location..."
                className="w-full h-10 pl-10 pr-3 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <svg
                className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              {isSearching && (
                <svg className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
              )}
            </div>

            {/* Search results dropdown */}
            {showResults && searchResults.length > 0 && (
              <div className="absolute z-[1000] mt-1 w-full bg-white dark:bg-gray-800 rounded-md border border-gray-200 dark:border-gray-600 shadow-lg max-h-60 overflow-y-auto">
                {searchResults.map((result, index) => (
                  <button
                    key={index}
                    type="button"
                    onClick={() => handleSearchResultSelect(result)}
                    className="w-full px-3 py-2 text-left text-sm hover:bg-gray-100 dark:hover:bg-gray-700 focus:bg-gray-100 dark:focus:bg-gray-700 focus:outline-none border-b border-gray-100 dark:border-gray-700 last:border-0"
                  >
                    <div className="font-medium text-gray-900 dark:text-gray-100 truncate">
                      {result.displayName.split(',')[0]}
                    </div>
                    <div className="text-gray-500 dark:text-gray-400 text-xs truncate">
                      {result.displayName.split(',').slice(1).join(',').trim()}
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}

        {/* GPS button */}
        {showCurrentLocation && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleUseCurrentLocation}
            disabled={gpsLoading}
            className="whitespace-nowrap"
          >
            {gpsLoading ? (
              <>
                <svg className="animate-spin -ml-1 mr-2 h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Getting...
              </>
            ) : (
              <>
                <svg className="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                GPS
              </>
            )}
          </Button>
        )}
      </div>

      {gpsError && (
        <span className="text-sm text-red-600">{gpsError}</span>
      )}

      <MapWrapper center={mapCenter} zoom={initialZoom} height={height}>
        <MapClickHandler onLocationSelect={handleLocationSelect} />
        <FlyToLocation position={gpsMarkerPosition || (shouldFlyToSearch ? searchPosition : null)} shouldFly={shouldFlyToGps || shouldFlyToSearch} />
        {markerPosition && (
          <Marker
            position={[markerPosition.lat, markerPosition.lng]}
            draggable={true}
            eventHandlers={{
              dragend: handleMarkerDrag,
            }}
          />
        )}
      </MapWrapper>

      {/* Coordinates display */}
      {value && (
        <div className="flex items-center gap-4 text-sm text-gray-600 dark:text-gray-400">
          <span>
            <span className="font-medium">Lat:</span> {value.latitude.toFixed(6)}
          </span>
          <span>
            <span className="font-medium">Lng:</span> {value.longitude.toFixed(6)}
          </span>
          {isFetchingElevation && (
            <span className="text-blue-600 dark:text-blue-400">Fetching elevation...</span>
          )}
        </div>
      )}

      <p className="text-xs text-gray-500 dark:text-gray-400">
        Search for a location, click on the map to place a marker, or drag the marker to adjust the position.
      </p>
    </div>
  );
}
